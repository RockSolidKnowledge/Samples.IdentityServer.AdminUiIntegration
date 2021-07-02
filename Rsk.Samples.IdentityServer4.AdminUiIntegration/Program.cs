using System;
using System.Collections.Generic;
using System.Linq;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Hosting;
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

            if (args.Contains("seed"))
            {
                var context = host.Services.GetRequiredService<IConfigurationDbContext>();
                var exitCode = Config.Seed(context);
                Environment.ExitCode = exitCode;
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
        public static int Seed(IConfigurationDbContext context)
        {
            var missingClients = Clients.Where(x => !context.Clients.Any(y => y.ClientId == x.ClientId));
            context.Clients.AddRange(missingClients.Select(x => x.ToEntity()));
            var missingIdentityResources = IdentityResources.Where(x => !context.IdentityResources.Any(y => y.Name == x.Name));
            context.IdentityResources.AddRange(missingIdentityResources.Select(x => x.ToEntity()));
            var missingApiResources = ApiResources.Where(x => !context.IdentityResources.Any(y => y.Name == x.Name));
            context.ApiResources.AddRange(missingApiResources.Select(x => x.ToEntity()));
            var missingApiScopes = ApiScopes.Where(x => !context.IdentityResources.Any(y => y.Name == x.Name));
            context.ApiScopes.AddRange(missingApiScopes.Select(x => x.ToEntity()));
            
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
                    ClientSecrets = {new Secret("secret".Sha256())},
                    Description = "Demo client credentials app. Client secret = 'secret'.",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,
                    AllowedScopes = {"api1"}
                },

                // interactive ASP.NET Core MVC client
                new Client
                {
                    ClientId = "mvc",
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