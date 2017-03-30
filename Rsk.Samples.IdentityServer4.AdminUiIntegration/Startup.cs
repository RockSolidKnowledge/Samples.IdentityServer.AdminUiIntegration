using System;
using System.Reflection;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

            if (Configuration.GetValue("DbProvider", "Sqlite") == "SqlServer")
                builder = x => x.UseSqlServer(connectionString, options => options.MigrationsAssembly(migrationAssembly));
            else
                builder = x => x.UseSqlite(connectionString, options => options.MigrationsAssembly(migrationAssembly));

            services.AddIdentityExpressAdminUiConfiguraiton(builder)
                .AddIdentityServerUserClaimsPrincipalFactory();

            services.AddIdentityServer()
                .AddTemporarySigningCredential()
                .AddOperationalStore(builder)
                .AddConfigurationStore(builder)
                .AddAspNetIdentity<IdentityExpressUser>();

            services.AddMvc();
        }
        
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseDeveloperExceptionPage();
            
            app.UseIdentity();
            app.UseIdentityServer();

            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }
    }
}
