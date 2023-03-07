using System;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Duende.Bff.EntityFramework;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using IdentityExpress.Identity;
using IdentityExpress.Manager.Api.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Logging;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Demo;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Middleware;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Services;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            IsDemo = Configuration.GetValue("IsDemo", false);
        }

        public IConfigurationRoot Configuration { get; }
        private bool IsDemo { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //add memory cache for custom identity server event sink
            //limit cache to preserving the last 10 error or failure identity server events
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 10;
            });

            // configure databases
            Action<DbContextOptionsBuilder> identityBuilder;
            Action<DbContextOptionsBuilder> identityServerBuilder;
            var identityConnectionString = Configuration.GetValue("IdentityConnectionString", Configuration.GetValue<string>("DbConnectionString"));
            var identityServerConnectionString = Configuration.GetValue("IdentityServerConnectionString", Configuration.GetValue<string>("DbConnectionString"));
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var operationalStoreSchemaName = Configuration.GetValue("OperationalStoreSchemaName", "dbo");
            var configurationStoreSchemaName = Configuration.GetValue("ConfigurationStoreSchemaName", "dbo");

            switch (Configuration.GetValue<string>("DbProvider"))
            {
                case "SqlServer":
                    identityBuilder = x => x.UseSqlServer(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseSqlServer(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                case "MySql":
                    identityBuilder = x => x.UseMySql(identityConnectionString,  new MySqlServerVersion(new Version(5, 6, 49)), options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseMySql(identityServerConnectionString, new MySqlServerVersion(new Version(5, 6, 49)),options => options.MigrationsAssembly(migrationAssembly));
                    break;
                case "PostgreSql":
                    identityBuilder = x => x.UseNpgsql(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseNpgsql(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                default:
                    identityBuilder = x => x.UseSqlite(identityConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    identityServerBuilder = x => x.UseSqlite(identityServerConnectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
            }
			
            // configure test-suitable X-Forwarded headers and CORS policy
            services.AddSingleton<XForwardedPrefixMiddleware>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.RequireHeaderSymmetry = false;
                options.ForwardLimit = 10;
                
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });
            
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            // configure ASP.NET Identity
            services
                .AddIdentityExpressAdminUiConfiguration(identityBuilder) // ASP.NET Core Identity Registrations for AdminUI
                .AddDefaultTokenProviders()
                .AddIdentityExpressUserClaimsPrincipalFactory(); // Claims Principal Factory for loading AdminUI users as .NET Identities

            services.AddScoped<IUserStore<IdentityExpressUser>>(
                x => new IdentityExpressUserStore(x.GetService<IdentityExpressDbContext>())
                {
                    AutoSaveChanges = true
                });
            
            // Add ServerSideSessions 
            services.AddBff()
                .AddEntityFrameworkServerSideSessions(options =>
                {
                    options.UseSqlServer(identityServerConnectionString);
                });
            
            // configure IdentityServer
            services.AddIdentityServer(options =>
                {
                    options.KeyManagement.Enabled = false; // disabled to only use test cert
                    options.LicenseKey = null; // for development only
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    options.DynamicProviders.SignInScheme = "Identity.External";
                    options.DynamicProviders.SignOutScheme = "Identity.External";
                })
                .AddOperationalStore(
                    options => {
                        options.ConfigureDbContext = identityServerBuilder;
                        options.DefaultSchema = operationalStoreSchemaName;
                    })
                .AddConfigurationStore(options => {
                    options.ConfigureDbContext = identityServerBuilder;
                    options.DefaultSchema = configurationStoreSchemaName;
                })
                .AddAspNetIdentity<IdentityExpressUser>() // configure IdentityServer to use ASP.NET Identity
                .AddSigningCredential(GetEmbeddedCertificate()) // embedded test cert for testing only
                .AddServerSideSessions();
           
            // Demo services - DO NOT USE IN PRODUCTION
            if (IsDemo)
            {
                services.AddTransient<ICorsPolicyService, DemoCorsPolicy>();
                services.AddTransient<IRedirectUriValidator, DemoRedirectUriValidator>();
            }
            
            // configure the ASP.NET Identity cookie to work on HTTP for testing only
            services.Configure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.IsEssential = true;
            });

            // optional Google authentication
            var googleClientId = Configuration.GetValue<string>("Google_ClientId");
            var googleClientSecret = Configuration.GetValue<string>("Google_ClientSecret");
            if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                services
                    .AddAuthentication()
                    .AddGoogle(options =>
                    {
                        options.SignInScheme = "Identity.External"; // ASP.NET Core Identity Extneral User Cookie
                        options.ClientId = googleClientId; // ClientId Configured within Google Admin
                        options.ClientSecret = googleClientSecret; // ClientSecret Generated within Google Admin
                    });
            }

            services.AddMvc();
            services.AddMvcCore().AddAuthorization();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = "https://localhost:5001";
                    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
                    options.Audience = "admin_api";

                    // if token does not contain a dot, it is a reference token
                    options.ForwardDefaultSelector = Selector.ForwardReferenceToken("introspection");
                })
                .AddOAuth2Introspection("introspection", options =>
                {
                    options.Authority = "https://localhost:5001";
                    options.ClientId = "admin_api";
                    options.ClientSecret = "adminUiWebhooksSecret";
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("webhook", builder =>
                {
                    builder.AddAuthenticationSchemes("Bearer");
                    builder.RequireScope("admin_ui_webhooks");
                });
            });

			services.AddSingleton<IEventSink, CustomEventSink>();
            services.AddSingleton<IEventStore, ErrorEventStore>();

            services.AddSingleton<IAccountService, AccountService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            
            app.UseMiddleware<XForwardedPrefixMiddleware>();
            app.UseForwardedHeaders();

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }

        private static X509Certificate2 GetEmbeddedCertificate()
        {
            try
            {
                using (var certStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(@"Rsk.Samples.IdentityServer.AdminUiIntegration.CN=RSKSampleIdentityServer.pfx"))
                {
                    var rawBytes = new byte[certStream.Length];
                    for (var index = 0; index < certStream.Length; index++)
                    {
                        rawBytes[index] = (byte)certStream.ReadByte();
                    }
                    
                    return new X509Certificate2(rawBytes, "Password123!", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);
                }
            }
            catch (Exception)
            {
                return new X509Certificate2(AppDomain.CurrentDomain.BaseDirectory + "CN=RSKSampleIdentityServer.pfx", "Password123!");
            }
        }
    }
}
