using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace Rsk.Samples.IdentityServer4.AdminUIIntegration.Tests
{
    public class AccountServiceUnitTests
    {
        private readonly Mock<IIdentityServerInteractionService> mockInteraction;
        private readonly Mock<IClientStore> mockClientStore;
        private readonly Mock<IHttpContextAccessor> mockAccessor;
        private readonly Mock<IAuthenticationSchemeProvider> mockSchemeProvider;
        

        public AccountServiceUnitTests()
        {
            mockInteraction = new Mock<IIdentityServerInteractionService>();
            mockClientStore = new Mock<IClientStore>();
            mockAccessor = new Mock<IHttpContextAccessor>();
            mockSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
        }

        private AccountService sut()
        {
            return new AccountService(mockInteraction.Object, mockAccessor.Object, mockSchemeProvider.Object, mockClientStore.Object);
        }

        [Fact]
        public void BuildLoggedOutViewModelAsync_WhenSuccessfulLogout_ShowCorrectLogoutView()
        {
            //arrange
            string logoutId = "logoutId";
            string iFrameUrl = "iframeUrl";
            string redirectUrl = "redirectUrl";

            ClaimsPrincipal user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]{
                new Claim(ClaimTypes.Name, "name"),
                new Claim(JwtClaimTypes.IdentityProvider,"client")
            }));

            Client client = new Client()
            {
                ClientId = "clientId",
                ClientName = "clientName",
                PostLogoutRedirectUris = new List<string> { redirectUrl}
            };

            LogoutMessage logoutMessge = new LogoutMessage()
            {
                ClientId = client.ClientId,
                ClientName = client.ClientName,
                ClientIds = new List<string>
                {
                    client.ClientId
                },
                SessionId = "sessionId",
                SubjectId = "subjectId",
                PostLogoutRedirectUri = redirectUrl
            };

            LogoutRequest logoutRequest = new LogoutRequest(iFrameUrl, logoutMessge);
            mockInteraction.Setup(x => x.GetLogoutContextAsync(logoutId))
                .ReturnsAsync(logoutRequest).Verifiable();

            mockAccessor.SetupGet(x => x.HttpContext.User).Returns(user).Verifiable();

            //act
            var result = sut().BuildLoggedOutViewModelAsync(logoutId).Result;

            //assert
            Assert.False(result.AutomaticRedirectAfterSignOut);
            Assert.Equal(redirectUrl, result.PostLogoutRedirectUri);
            Assert.Equal(client.ClientName, result.ClientName);
            Assert.Equal(iFrameUrl, result.SignOutIframeUrl);
            Assert.Equal(logoutId, result.LogoutId);

            mockInteraction.Verify();
            mockAccessor.Verify();

        }
    }
}
