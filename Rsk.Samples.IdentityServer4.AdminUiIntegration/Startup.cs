using System;
using System.Reflection;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            Action<DbContextOptionsBuilder> builder;
            var connectionString = Configuration.GetValue<string>("DbConnectionString");
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            switch (Configuration.GetValue<string>("DbProvider"))
            {
                case "SqlServer":
                    builder = x => x.UseSqlServer(connectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                case "MySql":
                    builder = x => x.UseMySql(connectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
                default:
                    builder = x => x.UseSqlite(connectionString, options => options.MigrationsAssembly(migrationAssembly));
                    break;
            }

            services.AddCors();

            services.AddIdentityExpressAdminUiConfiguration(builder)
                .AddIdentityServerUserClaimsPrincipalFactory();

            services.AddScoped<IUserStore<IdentityExpressUser>>(x =>
            {
                return new IdentityExpressUserStore(x.GetService<IdentityExpressDbContext>())
                {
                    AutoSaveChanges = true
                };
            });

            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddOperationalStore(builder)
                .AddConfigurationStore(builder)
                .AddAspNetIdentity<IdentityExpressUser>();

            services.AddMvc();
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Warning);
            //app.UseDeveloperExceptionPage();
            
            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

            app.UseIdentity();
            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
