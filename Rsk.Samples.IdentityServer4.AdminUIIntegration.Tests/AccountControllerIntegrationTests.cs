using IdentityExpress.Identity;
using IdentityServer4.Quickstart.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Rsk.Samples.IdentityServer4.AdminUiIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Rsk.Samples.IdentityServer4.AdminUIIntegration.Tests
{
    public class AccountControllerIntegrationTests
    {
        private HttpClient client;
        private DbContextOptions<IdentityExpressDbContext> options;
        private ILookupNormalizer normalizer;

        public AccountControllerIntegrationTests()
        {
            var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\Rsk.Samples.IdentityServer4.AdminUiIntegration"));

            var config = new ConfigurationBuilder()
                .SetBasePath(path)
                .AddJsonFile("appsettings.testing.json").Build();

            normalizer = new UpperInvariantLookupNormalizer();
            options = new DbContextOptionsBuilder<IdentityExpressDbContext>().UseSqlite(config.GetValue<string>("DbConnectionString")).Options;

            using (var context = new IdentityExpressDbContext(options))
                context.Database.EnsureCreated();
            
            var testServer = new TestServer(new WebHostBuilder().UseStartup<Startup>().UseContentRoot(path).UseEnvironment("testing"));
            client = testServer.CreateClient();
        }

        [Fact]
        public async Task Resgiter_WhenModelIsValidWithNullPassword_ExpectSuccessAndUserPasswordSet()
        {
            //arrange
            var username = Guid.NewGuid().ToString();
            var user = new IdentityExpressUser()
            {
                UserName = username,
                NormalizedUserName = normalizer.Normalize(username)
            };

            using (var context = new IdentityExpressDbContext(options))
            {
                context.Users.Add(user);
                context.SaveChanges();
            }

            var model = new RegisterInputModel
            {
                ConfirmPassword = "Password123!",
                Password = "Password123!",
                Username = username
            };

            
            var list = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("ConfirmPassword", "Password123!"),
                new KeyValuePair<string, string>("Password", "Password123!"),
                new KeyValuePair<string, string>("Username", username)
            };


            //act
            var result = await client.PostAsync("/account/register", new FormUrlEncodedContent(list));

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
        public async Task Resgiter_WhenUserAlreadyHasPassword_ExpectFailureAndPasswordNotUpdated()
        {
            //arrange
            var username = Guid.NewGuid().ToString();
            var user = new IdentityExpressUser()
            {
                UserName = username,
                NormalizedUserName = normalizer.Normalize(username)       
            };

            var passwordHasher = new PasswordHasher<IdentityExpressUser>();
            var hash = passwordHasher.HashPassword(user, "hello");
            user.PasswordHash = hash;

            using (var context = new IdentityExpressDbContext(options))
            {
                context.Users.Add(user);
                context.SaveChanges();
            }

            var model = new RegisterInputModel
            {
                ConfirmPassword = "Password123!",
                Password = "Password123!",
                Username = username
            };
            var json = JsonConvert.SerializeObject(model);

            //act
            var result = await client.PostAsync("/account/register", new StringContent(json, Encoding.UTF8, "application/json"));

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
