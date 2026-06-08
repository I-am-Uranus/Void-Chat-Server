using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Void.Database;
using Void.Hubs;
using Void.Repositories;
using Void.Services;


namespace Void
{
    public class Startup
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder.Services);

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                db.Database.EnsureCreated();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseHttpsRedirection();
            }
            app.UseWebSockets();

            app.UseRouting();
            app.UseCors("AllowReact");

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHub<GroupChatHub>("/groupChatHub");
            app.MapHub<PrivateChatHub>("/privateChatHub");
            app.MapHub<NotificationHub>("/notificationHub");

            app.MapControllers();
            InitializeDatabase(app);

            app.Run();
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddSignalR();

            services.AddScoped<UserService>();
            services.AddScoped<NotificationService>();
            services.AddDbContext<DatabaseContext>(options =>
                options.UseSqlite("Data Source=Void.db"));


            services.AddCors(options =>
            {
                options.AddPolicy("AllowReact",
                    builder =>
                    {
                        builder.WithOrigins("http://localhost:5173")
                               .AllowAnyHeader()
                               .AllowAnyMethod()
                               .AllowCredentials();

                    });
            });


            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                        options.SlidingExpiration = true;
                        options.Cookie.SameSite = SameSiteMode.Strict;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                        options.Cookie.HttpOnly = false;
                    });

            services.AddAuthorization();

            services.AddScoped<AuthenticationService>();
            services.AddScoped<UserService>();
            services.AddScoped<FriendshipService>();
            services.AddScoped<ChatService>();

            services.AddScoped<UserRepository>();
            services.AddScoped<FriendshipRepository>();
            services.AddScoped<FriendRequestRepository>();
            services.AddScoped<BlockRepository>();
            services.AddScoped<ChatRepository>();



        }
        private static void InitializeDatabase(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            context.Database.EnsureCreated();
            EnsureUserDisplayNameColumn(context);
            EnsureFriendshipTableSchema(context);
            EnsureFriendRequestTableSchema(context);
            EnsureNotificationTableSchema(context);
        }

        private static void EnsureUserDisplayNameColumn(DatabaseContext context)
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info('Users');";

            var hasDisplayName = false;
            using (var reader = checkCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (string.Equals(reader["name"]?.ToString(), "DisplayName", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDisplayName = true;
                        break;
                    }
                }
            }

            if (!hasDisplayName)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN DisplayName TEXT;");
            }
        }

        private static void EnsureFriendshipTableSchema(DatabaseContext context)
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info('Friendships');";

            var hasRows = false;
            var hasUserAId = false;
            var hasUserBId = false;
            var hasLegacyUserId = false;
            var hasLegacyFriendId = false;

            using (var reader = checkCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    hasRows = true;
                    var columnName = reader["name"]?.ToString();

                    if (string.Equals(columnName, "UserAId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasUserAId = true;
                    }
                    else if (string.Equals(columnName, "UserBId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasUserBId = true;
                    }
                    else if (string.Equals(columnName, "UserId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasLegacyUserId = true;
                    }
                    else if (string.Equals(columnName, "FriendId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasLegacyFriendId = true;
                    }
                }
            }

            if (hasUserAId && hasUserBId)
            {
                context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Friendships_UserAId_UserBId ON Friendships (UserAId, UserBId);");
                return;
            }

            if (!hasRows)
            {
                context.Database.ExecuteSqlRaw(
                    @"CREATE TABLE IF NOT EXISTS Friendships (
                        Id INTEGER NOT NULL CONSTRAINT PK_Friendships PRIMARY KEY AUTOINCREMENT,
                        UserAId INTEGER NOT NULL,
                        UserBId INTEGER NOT NULL,
                        CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        CONSTRAINT FK_Friendships_Users_UserAId FOREIGN KEY (UserAId) REFERENCES Users (Id) ON DELETE CASCADE,
                        CONSTRAINT FK_Friendships_Users_UserBId FOREIGN KEY (UserBId) REFERENCES Users (Id) ON DELETE CASCADE
                    );");

                context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Friendships_UserAId ON Friendships (UserAId);");
                context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Friendships_UserBId ON Friendships (UserBId);");
                context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Friendships_UserAId_UserBId ON Friendships (UserAId, UserBId);");
                return;
            }

            if (!(hasLegacyUserId && hasLegacyFriendId))
            {
                throw new InvalidOperationException("Friendships table schema is incompatible with the current application model.");
            }

            using var transaction = context.Database.BeginTransaction();

            context.Database.ExecuteSqlRaw(
                @"CREATE TABLE __Friendships_Migration (
                    Id INTEGER NOT NULL CONSTRAINT PK_Friendships PRIMARY KEY AUTOINCREMENT,
                    UserAId INTEGER NOT NULL,
                    UserBId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT FK_Friendships_Users_UserAId FOREIGN KEY (UserAId) REFERENCES Users (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_Friendships_Users_UserBId FOREIGN KEY (UserBId) REFERENCES Users (Id) ON DELETE CASCADE
                );");

            context.Database.ExecuteSqlRaw(
                @"INSERT INTO __Friendships_Migration (Id, UserAId, UserBId, CreatedAt)
                  SELECT Id, UserId, FriendId, COALESCE(CreatedAt, CURRENT_TIMESTAMP)
                  FROM Friendships
                  WHERE Status = 1 OR Status IS NULL;");

            context.Database.ExecuteSqlRaw("DROP TABLE Friendships;");
            context.Database.ExecuteSqlRaw("ALTER TABLE __Friendships_Migration RENAME TO Friendships;");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Friendships_UserAId ON Friendships (UserAId);");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Friendships_UserBId ON Friendships (UserBId);");
            context.Database.ExecuteSqlRaw("CREATE UNIQUE INDEX IF NOT EXISTS IX_Friendships_UserAId_UserBId ON Friendships (UserAId, UserBId);");

            transaction.Commit();
        }

        private static void EnsureFriendRequestTableSchema(DatabaseContext context)
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info('FriendRequests');";

            var hasRows = false;
            var hasRequesterId = false;
            var hasRecipientId = false;
            var hasStatus = false;
            var hasCreatedAt = false;
            var hasUpdatedAt = false;

            using (var reader = checkCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    hasRows = true;
                    var columnName = reader["name"]?.ToString();

                    if (string.Equals(columnName, "RequesterId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRequesterId = true;
                    }
                    else if (string.Equals(columnName, "RecipientId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRecipientId = true;
                    }
                    else if (string.Equals(columnName, "Status", StringComparison.OrdinalIgnoreCase))
                    {
                        hasStatus = true;
                    }
                    else if (string.Equals(columnName, "CreatedAt", StringComparison.OrdinalIgnoreCase))
                    {
                        hasCreatedAt = true;
                    }
                    else if (string.Equals(columnName, "UpdatedAt", StringComparison.OrdinalIgnoreCase))
                    {
                        hasUpdatedAt = true;
                    }
                }
            }

            if (!hasRows)
            {
                CreateFriendRequestsTable(context);
                return;
            }

            if (hasRequesterId && hasRecipientId && hasStatus && hasCreatedAt)
            {
                if (!hasUpdatedAt)
                {
                    context.Database.ExecuteSqlRaw("ALTER TABLE FriendRequests ADD COLUMN UpdatedAt TEXT;");
                }

                CreateFriendRequestsIndexes(context);
                return;
            }

            using var transaction = context.Database.BeginTransaction();

            context.Database.ExecuteSqlRaw("DROP TABLE FriendRequests;");
            CreateFriendRequestsTable(context);

            transaction.Commit();
        }

        private static void CreateFriendRequestsTable(DatabaseContext context)
        {
            context.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS FriendRequests (
                    Id INTEGER NOT NULL CONSTRAINT PK_FriendRequests PRIMARY KEY AUTOINCREMENT,
                    RequesterId INTEGER NOT NULL,
                    RecipientId INTEGER NOT NULL,
                    Status INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NULL,
                    CONSTRAINT FK_FriendRequests_Users_RequesterId FOREIGN KEY (RequesterId) REFERENCES Users (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_FriendRequests_Users_RecipientId FOREIGN KEY (RecipientId) REFERENCES Users (Id) ON DELETE CASCADE
                );");

            CreateFriendRequestsIndexes(context);
        }

        private static void CreateFriendRequestsIndexes(DatabaseContext context)
        {
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FriendRequests_RequesterId ON FriendRequests (RequesterId);");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FriendRequests_RecipientId ON FriendRequests (RecipientId);");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_FriendRequests_RequesterId_RecipientId_Status ON FriendRequests (RequesterId, RecipientId, Status);");
        }

        private static void EnsureNotificationTableSchema(DatabaseContext context)
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = "PRAGMA table_info('Notifications');";

            var hasRows = false;
            var hasRecipientUserId = false;
            var hasSenderUserId = false;
            var hasType = false;
            var hasTitle = false;
            var hasMessage = false;
            var hasIsRead = false;
            var hasCreatedAt = false;
            var hasRelatedEntityId = false;

            using (var reader = checkCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    hasRows = true;
                    var columnName = reader["name"]?.ToString();

                    if (string.Equals(columnName, "RecipientUserId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRecipientUserId = true;
                    }
                    else if (string.Equals(columnName, "SenderUserId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasSenderUserId = true;
                    }
                    else if (string.Equals(columnName, "Type", StringComparison.OrdinalIgnoreCase))
                    {
                        hasType = true;
                    }
                    else if (string.Equals(columnName, "Title", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTitle = true;
                    }
                    else if (string.Equals(columnName, "Message", StringComparison.OrdinalIgnoreCase))
                    {
                        hasMessage = true;
                    }
                    else if (string.Equals(columnName, "IsRead", StringComparison.OrdinalIgnoreCase))
                    {
                        hasIsRead = true;
                    }
                    else if (string.Equals(columnName, "CreatedAt", StringComparison.OrdinalIgnoreCase))
                    {
                        hasCreatedAt = true;
                    }
                    else if (string.Equals(columnName, "RelatedEntityId", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRelatedEntityId = true;
                    }
                }
            }

            if (!hasRows)
            {
                CreateNotificationsTable(context);
                return;
            }

            if (!hasRecipientUserId)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN RecipientUserId INTEGER NOT NULL DEFAULT 0;");
            }

            if (!hasSenderUserId)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN SenderUserId INTEGER NULL;");
            }

            if (!hasType)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN Type TEXT NOT NULL DEFAULT 'Chat';");
            }

            if (!hasTitle)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN Title TEXT NOT NULL DEFAULT '';");
            }

            if (!hasMessage)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN Message TEXT NOT NULL DEFAULT '';");
            }

            if (!hasIsRead)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN IsRead INTEGER NOT NULL DEFAULT 0;");
            }

            if (!hasCreatedAt)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT '1970-01-01 00:00:00';");
            }

            if (!hasRelatedEntityId)
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE Notifications ADD COLUMN RelatedEntityId INTEGER NULL;");
            }

            CreateNotificationsIndexes(context);
        }

        private static void CreateNotificationsTable(DatabaseContext context)
        {
            context.Database.ExecuteSqlRaw(
                @"CREATE TABLE IF NOT EXISTS Notifications (
                    Id INTEGER NOT NULL CONSTRAINT PK_Notifications PRIMARY KEY AUTOINCREMENT,
                    RecipientUserId INTEGER NOT NULL,
                    SenderUserId INTEGER NULL,
                    Type TEXT NOT NULL,
                    Title TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    IsRead INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    RelatedEntityId INTEGER NULL
                );");

            CreateNotificationsIndexes(context);
        }

        private static void CreateNotificationsIndexes(DatabaseContext context)
        {
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientUserId_CreatedAt ON Notifications (RecipientUserId, CreatedAt);");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientUserId_Type_CreatedAt ON Notifications (RecipientUserId, Type, CreatedAt);");
            context.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Notifications_RecipientUserId_IsRead ON Notifications (RecipientUserId, IsRead);");
        }
    }
}
