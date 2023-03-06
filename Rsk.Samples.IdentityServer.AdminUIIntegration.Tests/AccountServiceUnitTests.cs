﻿using System.Collections.Generic;
using System.Security.Claims;
using IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Moq;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Services;
using Xunit;

namespace AdminUIIntegration.Tests
{
    public class AccountServiceUnitTests
    {
        private readonly Mock<IIdentityServerInteractionService> mockInteraction;
        private readonly Mock<IClientStore> mockClientStore;
        private readonly Mock<IHttpContextAccessor> mockAccessor;
        private readonly Mock<IAuthenticationSchemeProvider> mockSchemeProvider;
        private readonly Mock<IIdentityProviderStore> mockIdentityProviderStore;
        

        public AccountServiceUnitTests()
        {
            mockInteraction = new Mock<IIdentityServerInteractionService>();
            mockClientStore = new Mock<IClientStore>();
            mockAccessor = new Mock<IHttpContextAccessor>();
            mockSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
            mockIdentityProviderStore = new Mock<IIdentityProviderStore>();
        }

        private AccountService CreateSut()
        {
            return new AccountService(mockInteraction.Object, mockAccessor.Object, mockSchemeProvider.Object, mockClientStore.Object, mockIdentityProviderStore.Object);
        }

        [Fact]
        public void BuildLoggedOutViewModelAsync_WhenSuccessfulLogout_ShowCorrectLogoutView()
        {
            //arrange
            const string logoutId = "logoutId";
            const string iFrameUrl = "iframeUrl";
            const string redirectUrl = "redirectUrl";

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "name"),
                new Claim(JwtClaimTypes.IdentityProvider, "client")
            }));

            var client = new Client
            {
                ClientId = "clientId",
                ClientName = "clientName",
                PostLogoutRedirectUris = new List<string> { redirectUrl}
            };

            var logoutMessge = new LogoutMessage
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
            var result = CreateSut().BuildLoggedOutViewModelAsync(logoutId).Result;

            //assert
            Assert.True(result.AutomaticRedirectAfterSignOut);
            Assert.Equal(redirectUrl, result.PostLogoutRedirectUri);
            Assert.Equal(client.ClientName, result.ClientName);
            Assert.Equal(iFrameUrl, result.SignOutIframeUrl);
            Assert.Equal(logoutId, result.LogoutId);

            mockInteraction.Verify();
            mockAccessor.Verify();

        }
    }
}
