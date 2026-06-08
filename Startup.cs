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

            transaction.Commit();
        }
    }
}
