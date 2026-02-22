using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Procrastinator.Context;
using Procrastinator.Models;
using Procrastinator.Services;
using Procrastinator.Utilities;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
ConfigureServices(services);
var app = builder.Build();
ConfigureMiddlewarePipeline(app);
using (var scope = app.Services.CreateScope())
{
    var dataContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dataContext.Database.Migrate();
}
app.Run();

#region Services
void ConfigureServices(IServiceCollection services)
{
    // Custom Services
    services.AddScoped<AuthService>();
    services.AddScoped<UserService>();
    services.AddScoped<QuestService>();
    services.AddScoped<HexAssignmentService>();

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
        Console.WriteLine("Connection String: " + Env.CONNECTION_STRING);
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

void ConfigureCors(IServiceCollection services)
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

bool IsOriginAllowed(string origin)
{
    List<string> localUrls =
            new()
            {
                    "http://localhost",
                    "https://localhost",
                    "https://localhost:4200",
                    "http://localhost:4200",
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
void ConfigureControllers(IServiceCollection services)
{
    services.AddControllers()
         .AddJsonOptions(options =>
         {
             options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
         }); 
}

void ConfigureIdentity(IServiceCollection services)
{
    services
        .AddIdentity<UserApp, Procrastinator.Context.Role>()
        .AddEntityFrameworkStores<DataContext>()
        .AddRoleManager<RoleManager<Procrastinator.Context.Role>>()
        .AddUserManager<UserManager<UserApp>>()
        .AddSignInManager<SignInManager<UserApp>>()
        .AddDefaultTokenProviders();

    services.Configure<IdentityOptions>(options =>
    {
        // Password settings
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 2;
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

void ConfigureSwagger(IServiceCollection services)
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

void ConfigureAuthentication(IServiceCollection services)
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
    void ConfigureMiddlewarePipeline(WebApplication app)
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

        // Enable authentication
        app.UseAuthentication();

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

        // Enable authorization
        app.UseAuthorization();

    // Map controllers
    app.MapControllers();
    }
#endregion
