using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AssociationAdonfAPI.Context;
using AssociationAdonfAPI.Models;
using AssociationAdonfAPI.Services;
using AssociationAdonfAPI.Utilities;

// Make the Program class accessible for integration tests
public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        var services = builder.Services;
        ConfigureServices(services);
        var app = builder.Build();
        ConfigureMiddlewarePipeline(app);

        //// Only run migrations if not in testing environment
        //if (!app.Environment.IsEnvironment("Testing"))
        //{
        //using (var scope = app.Services.CreateScope())
        //{
        //    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
        //    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserApp>>(); 

        //    dataContext.Database.Migrate();
        //    //dataContext.Database.EnsureCreated();

        //    var user = new UserApp
        //    {
        //        Name = "Antoine Simper",
        //        UserName = "antoine.simper@gmail.com",
        //        Email = "antoine.simper@gmail.com"
        //    };

        //   var res =  await userManager.CreateAsync(user, "mahdiiii");
        //}
        //}
        // Apply database migrations
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            context.Database.Migrate();

            // Seed default users
            SeedUsers(scope.ServiceProvider);
        }

        app.Run();
    }

    #region Services
    static void ConfigureServices(IServiceCollection services)
    {
        // Custom Services
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<SendMailService>();

        // Logger
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
            loggingBuilder.AddDebug();
        });

        // Routes
        services.AddRouting(opt => opt.LowercaseUrls = true);

        // Database Context (Entity Framework Core ORM)
        services.AddDbContext<DataContext>(options =>
        {
            options.UseNpgsql(Env.CONNECTION_STRING);
        });
        {
            ConfigureCors(services);
            ConfigureControllers(services);
            ConfigureSwagger(services);
            ConfigureIdentity(services);
            ConfigureAuthentication(services);
        }
    }

    #region Configuration Methods
    #region CORS

    static void ConfigureCors(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                .SetIsOriginAllowed(IsOriginAllowed)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
        });
    }

    static bool IsOriginAllowed(string origin)
    {
        List<string> localUrls =
                new()
                {
                        "http://localhost",
                        "https://localhost",
                        "https://localhost:4321",
                        "http://localhost:4321",
                        "http://localhost:7113",
                        "https://localhost:7113",
                        "https://localhost:7168",
                        "http://hexaplanning.fr",
                        "https://hexaplanning.fr",
                        "http://api.hexaplanning.fr",
                        "https://api.hexaplanning.fr",
                        Env.API_BACK_URL,
                        Env.API_FRONT_URL,
                };
        return localUrls.Contains(origin);
    }
    #endregion
    static void ConfigureControllers(IServiceCollection services)
    {
        services.AddControllers()
             .AddJsonOptions(options =>
             {
                 options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
             }); 
    }

    static void ConfigureIdentity(IServiceCollection services)
    {
        services
            .AddIdentity<UserApp, Role>()
            .AddEntityFrameworkStores<DataContext>()
            .AddRoleManager<RoleManager<Role>>()
            .AddUserManager<UserManager<UserApp>>()
            .AddSignInManager<SignInManager<UserApp>>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(2);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.AllowedUserNameCharacters =
                " abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            // Login settings
            options.SignIn.RequireConfirmedAccount = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        });
    }

    static void ConfigureSwagger(IServiceCollection services)
    {
        // Configuration Swagger
        services.AddSwaggerGen(c =>
        {
            // Set the comments path for the Swagger JSON and UI.
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            //c.IncludeXmlComments(xmlPath);

            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                }
            );

            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement()
                {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference
                                    {
                                        Type = ReferenceType.SecurityScheme,
                                        Id = "Bearer"
                                    },
                                    Scheme = "oauth2",
                                    Name = "Bearer",
                                    In = ParameterLocation.Header,
                                },
                                new List<string>()
                            }
                }
            );
        });

        services.AddHttpClient();
    }

    static void ConfigureAuthentication(IServiceCollection services)
    {
        // Authentication
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Env.API_BACK_URL,
                    ValidAudience = Env.API_BACK_URL,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Env.JWT_KEY)
                    ),
                };
            });
        // Authorization
        services.AddAuthorization();
    }
    #endregion
    #endregion

    #region Middleware
        static void ConfigureMiddlewarePipeline(WebApplication app)
        {
            // Configure localization for supported cultures
            var supportedCultures = new string[] { "fr-FR" };
            app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures)
                    .SetDefaultCulture("fr-FR")
            );

            // Store assets in the wwwroot folder
            app.UseStaticFiles();

            // Enable developer exception page if in development environment
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger and Swagger UI
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "data_lib v1");
                c.RoutePrefix = "swagger";
            });

            // Enable routing
            app.UseRouting();

            // Enable Cross-Origin Resource Sharing (CORS)
            app.UseCors();

            // Enable authentication
            app.UseAuthentication();

            // Enable authorization
            app.UseAuthorization();

            // Map controllers
            app.MapControllers();
    }
    #endregion

    static void SeedUsers(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserApp>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            // Seed a default super admin user
            var superAdminEmail = new UserApp
            {
                UserName = "antoine.simper+admin@gmail.com",
                Email = "antoine.simper+admin@gmail.com",
                EmailConfirmed = true,
                Name= "Administrator",
            };
            var superAdminPassword = "Password123!";
            if (userManager.FindByEmailAsync(superAdminEmail.Email).Result == null)
            {
                var createPowerUser = userManager
                    .CreateAsync(superAdminEmail, superAdminPassword)
                    .Result;
                if (createPowerUser.Succeeded)
                {
                    if (!roleManager.RoleExistsAsync("Admin").Result)
                    {
                        var role = new Role { Name = "Admin" };
                        roleManager.CreateAsync(role).Wait();
                    }
                    userManager.AddToRoleAsync(superAdminEmail, "Admin").Wait();
                }
            }
        }
    }
}
