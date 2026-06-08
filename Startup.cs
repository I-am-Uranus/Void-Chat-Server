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
            EnsureUserDisplayNameColumn(app);

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

            services.AddScoped<UserRepository>();
            services.AddScoped<FriendshipRepository>();
            services.AddScoped<FriendRequestRepository>();
            services.AddScoped<BlockRepository>();
            services.AddScoped<ChatRepository>();



        }

        private static void EnsureUserDisplayNameColumn(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
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
    }
}
