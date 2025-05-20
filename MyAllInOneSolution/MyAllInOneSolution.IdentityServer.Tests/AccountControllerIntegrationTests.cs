using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyAllInOneSolution.IdentityServer;
using Xunit;

namespace AdminUIIntegration.Tests
{
    public class AccountControllerIntegrationTests
    {
        private HttpClient client;
        private DbContextOptions<IdentityExpressDbContext> options;
        private ILookupNormalizer normalizer;

        public AccountControllerIntegrationTests()
        {
            var pathUri = new Uri(@"../../../../MyAllInOneSolution.IdentityServer", UriKind.Relative);
            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), pathUri.ToString()));

            var config = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.testing.json").Build();

            normalizer = new UpperInvariantLookupNormalizer();
            options = new DbContextOptionsBuilder<IdentityExpressDbContext>().UseSqlite(config.GetValue<string>("IdentityConnectionString")).Options;

            using (var context = new IdentityExpressDbContext(options))
                context.Database.EnsureCreated();
            
            var testServer = new TestServer(new WebHostBuilder().UseStartup<Startup>().UseContentRoot(path).UseEnvironment("testing"));
            client = testServer.CreateClient();
        }

        [Fact]
        public async Task Register_WhenModelIsValidWithNullPassword_ExpectSuccessAndUserPasswordSet()
        {
            //arrange
            var username = Guid.NewGuid().ToString();
            var user = new IdentityExpressUser()
            {
                UserName = username,
                NormalizedUserName = normalizer.NormalizeName(username)
            };

            using (var context = new IdentityExpressDbContext(options))
            {
                context.Users.Add(user);
                context.SaveChanges();
            }

            var registerInputModel = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("ConfirmPassword", "Password123!"),
                new KeyValuePair<string, string>("Password", "Password123!"),
                new KeyValuePair<string, string>("Username", username)
            };

            //act
            var result = await client.PostAsync("/account/register", new FormUrlEncodedContent(registerInputModel));

            //assert
            Assert.True(result.IsSuccessStatusCode);

            IdentityExpressUser foundUser;
            using (var context = new IdentityExpressDbContext(options))
            {
                foundUser = await context.Users.FirstOrDefaultAsync(x => x.UserName == username);
            }

            Assert.NotNull(foundUser);
            Assert.NotNull(foundUser.PasswordHash);
        }

        // TODO: Post with user who already has password

        [Fact]
        public async Task Register_WhenUserAlreadyHasPassword_ExpectFailureAndPasswordNotUpdated()
        {
            //arrange
            var username = Guid.NewGuid().ToString();
            var user = new IdentityExpressUser()
            {
                UserName = username,
                NormalizedUserName = normalizer.NormalizeName(username)       
            };

            var passwordHasher = new PasswordHasher<IdentityExpressUser>();
            var hash = passwordHasher.HashPassword(user, "hello");
            user.PasswordHash = hash;

            using (var context = new IdentityExpressDbContext(options))
            {
                context.Users.Add(user);
                context.SaveChanges();
            }

            var registerInputModel = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("ConfirmPassword", "Password123!"),
                new KeyValuePair<string, string>("Password", "Password123!"),
                new KeyValuePair<string, string>("Username", username)
            };

            //act
            var result = await client.PostAsync("/account/register", new FormUrlEncodedContent(registerInputModel));

            //assert
            Assert.True(result.IsSuccessStatusCode);

            IdentityExpressUser foundUser;
            using (var context = new IdentityExpressDbContext(options))
            {
                foundUser = await context.Users.FirstOrDefaultAsync(x => x.UserName == username);
            }

            Assert.NotNull(foundUser);
            Assert.Equal(hash, foundUser.PasswordHash);
        }
    }
}
