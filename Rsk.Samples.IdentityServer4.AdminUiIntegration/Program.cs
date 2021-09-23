using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args).Build();

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            var configuration = builder.Build();
            
            if (args.Contains("seed"))
            {
                var isDemo = configuration.GetValue("IsDemo", false);
                if (!isDemo)
                {
                    Console.WriteLine("IsDemo Set to False - Not Running Seed");
                    // Per https://tldp.org/LDP/abs/html/exitcodes.html
                    Environment.ExitCode = 126;
                    return;
                }

                //ONLY FOR DEMO PURPOSES
                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                    var exitCode = Config.Seed(dbContext);
                    Environment.ExitCode = exitCode;
                }
                return;
            }
            
            host.Run();
        }

        public static IHostBuilder BuildWebHost(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging((context, logging) =>
                        {
                            logging.AddDebug();
                            logging.AddConsole();

                            // Suppress some of the SQL Statements output from EF
                            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                        });
                });
    }
    
    /// <summary>
    /// Quickstart config
    /// </summary>
    public static class Config
    {
        public static int Seed(ConfigurationDbContext context)
        {
            var missingClients = Clients.Where(x => !context.Clients.Any(y => y.ClientId == x.ClientId));
            context.Clients.AddRange(missingClients.Select(x => x.ToEntity()));
            var missingApiResources = ApiResources.Where(x => !context.ApiResources.Any(y => y.Name == x.Name));
            context.ApiResources.AddRange(missingApiResources.Select(x => x.ToEntity()));
            var missingApiScopes = ApiScopes.Where(x => !context.ApiScopes.Any(y => y.Name == x.Name));
            context.ApiScopes.AddRange(missingApiScopes.Select(x => x.ToEntity()));
            
            var missingIdentityResources = IdentityResources.Where(x => !context.IdentityResources.Any(y => y.Name == x.Name));
            foreach (var idr in missingIdentityResources)
            {
                var idrToEntity = idr.ToEntity();
                if (idrToEntity.Name == IdentityServerConstants.StandardScopes.OpenId || idrToEntity.Name == IdentityServerConstants.StandardScopes.Profile) idrToEntity.NonEditable = true;
                context.IdentityResources.Add(idrToEntity);
            }

            try
            {
                context.SaveChanges();
            }
            catch (Exception e)
            {
                return 1;
            }
            return 0;
        }

        public static IEnumerable<IdentityResource> IdentityResources =>
            new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone()
            };

        private static IEnumerable<ApiResource> ApiResources => new[] {new ApiResource("api1", "Sample API") {Scopes = new[] {"api1"}}};
        private static IEnumerable<ApiScope> ApiScopes => new[] {new ApiScope("api1", "Sample API - full access")};

        private static IEnumerable<Client> Clients =>
            new[]
            {
                // machine to machine client
                new Client
                {
                    ClientId = "client",
                    ClientName = "Machine to Machine Client",
                    ClientSecrets = {new Secret("secret".Sha256())},
                    Description = "Demo client credentials app. Client secret = 'secret'.",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = {"api1"}
                },

                // interactive ASP.NET Core MVC client
                new Client
                {
                    ClientId = "mvc",
                    ClientName = "MVC Client",
                    ClientSecrets = {new Secret("secret".Sha256())},
                    Description = "Demo auth code + PKCE app. Client secret = 'secret'.",
                    AllowedGrantTypes = GrantTypes.Code,
                    RedirectUris = {"https://localhost:5002/signin-oidc"},
                    PostLogoutRedirectUris = {"https://localhost:5002/signout-callback-oidc"},
                    AllowedScopes = new List<string>
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "api1"
                    }
                }
            };
    }
}