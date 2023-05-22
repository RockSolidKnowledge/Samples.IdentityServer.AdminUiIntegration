using System.Threading.Tasks;
using Duende.IdentityServer.Models;
using IdentityExpress.Identity;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Controllers;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Services;
using Xunit;

namespace AdminUIIntegration.Tests
{
    public class AccountControllerUnitTests
    {
        private readonly Mock<UserManager<IdentityExpressUser>> mockUserManager;
        private readonly Mock<IIdentityServerInteractionService> mockInteraction;
        private readonly Mock<IClientStore> mockClientStore;
        private readonly Mock<IHttpContextAccessor> mockAccessor;
        private readonly Mock<IAuthenticationSchemeProvider> mockSchemeProvider;
        private readonly Mock<IEventService> mockEvents;
        private readonly Mock<IAccountService> mockAccountService;
        private readonly Mock<IUrlHelperFactory> mockUrlHelper;
        private readonly Mock<IIdentityProviderStore> mockIdentityProviderStore;

        private readonly Mock<HttpContext> mockHttpContext;
        private readonly Mock<IRequestCookieCollection> mockRequestCookies;

        public AccountControllerUnitTests()
        {
            mockInteraction = new Mock<IIdentityServerInteractionService>();
            mockClientStore = new Mock<IClientStore>();
            mockAccessor = new Mock<IHttpContextAccessor>();
            var userStoreMock = new Mock<IUserStore<IdentityExpressUser>>();
            mockUserManager = new Mock<UserManager<IdentityExpressUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);
            mockSchemeProvider = new Mock<IAuthenticationSchemeProvider>();
            mockEvents = new Mock<IEventService>();
            mockAccountService = new Mock<IAccountService>();
            mockUrlHelper = new Mock<IUrlHelperFactory>();
            mockIdentityProviderStore = new Mock<IIdentityProviderStore>();
            mockHttpContext = new Mock<HttpContext>();
            mockRequestCookies = new Mock<IRequestCookieCollection>();
        }

        private AccountController CreateSut(bool withExternalCookie = false)
        { 
            if (withExternalCookie)
            {
                mockRequestCookies.Setup(x => x["Identity.External"]).Returns("FakeCookie");
            }

            mockHttpContext.Setup(x => x.Request.Cookies).Returns(mockRequestCookies.Object);
            
            var sut = new AccountController(
                mockInteraction.Object,
                mockUserManager.Object,
                mockEvents.Object,
                mockAccountService.Object,
                mockUrlHelper.Object,
                mockIdentityProviderStore.Object);

            sut.ControllerContext = new ControllerContext
            {
                HttpContext = mockHttpContext.Object,
            };
            
            return sut;
        }

        [Fact]
        public async Task Login_WhenNotExternal_ShouldCallBuildLoginViewModelAsync()
        {
            var testReturnUrl = "";
            var testLoginViewModel = new LoginViewModel
            {
                EnableLocalLogin = true,
                ReturnUrl = testReturnUrl,
                Username = "LoginHunt",
            };
            
            mockAccountService.Setup(x => x.BuildLoginViewModelAsync(It.IsAny<string>()))
                .ReturnsAsync(testLoginViewModel);
            
            var sut = CreateSut();

            var actual = await sut.Login(testReturnUrl);

            mockAccountService.Verify(x => x.BuildLoginViewModelAsync(testReturnUrl));

            var viewResult = Assert.IsType<ViewResult>(actual);
            Assert.Equal(viewResult.Model, testLoginViewModel);
        }

        [Fact]
        public async Task Login_WhenExternal_ShouldCallBuildLinkLoginViewModel()
        {
            var testReturnUrl = "";
            var testLoginViewModel = new LoginViewModel
            {
                EnableLocalLogin = true,
                LinkSetup = true,
                ReturnUrl = testReturnUrl,
                Username = "LoginHunt",
            };
            
            mockAccountService.Setup(x => x.BuildLinkLoginViewModel(It.IsAny<string>()))
                .Returns(testLoginViewModel);
            
            var sut = CreateSut(withExternalCookie: true);

            var actual = await sut.Login(testReturnUrl);

            mockAccountService.Verify(x => x.BuildLinkLoginViewModel(testReturnUrl));

            var viewResult = Assert.IsType<ViewResult>(actual);
            Assert.Equal(viewResult.Model, testLoginViewModel);
        }

        [Fact]
        public async Task Login_WhenModelPassedIn_WithButtonNotLogin_AndNullAuthContext_ShouldRedirectToBase()
        {
            var testLoginModel = new LoginInputModel
            {
                Username = "tuser",
                Password = "somepassword",
                RememberLogin = true,
                ReturnUrl = "https://some.return.com",
            };

            mockInteraction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>()))
                .ReturnsAsync(() => null);
            
            var sut = CreateSut();
            var actual = await sut.Login(testLoginModel, "cancel");

            var viewResult = Assert.IsType<RedirectResult>(actual);
            Assert.Equal("~/", viewResult.Url);
        }

        [Fact]
        public async Task Login_WhenModelPassedIn_WithButtonNotLogin_AndAuthContextReturned_ShouldRedirectToReturnUrl()
        {
            var testLoginModel = new LoginInputModel
            {
                Username = "tuser",
                Password = "somepassword",
                RememberLogin = true,
                ReturnUrl = "https://some.return.com",
            };

            var authRequest = new AuthorizationRequest();

            mockInteraction.Setup(x => x.GetAuthorizationContextAsync(It.IsAny<string>()))
                .ReturnsAsync(() => authRequest);

            var sut = CreateSut();
            var actual = await sut.Login(testLoginModel, "cancel");
            
            mockInteraction.Verify(x => x.DenyAuthorizationAsync(authRequest, AuthorizationError.AccessDenied, null));

            var viewResult = Assert.IsType<RedirectResult>(actual);
            Assert.Equal(testLoginModel.ReturnUrl, viewResult.Url);
        }
        
        //Returns view when modelState is invalid.
        [Fact]
        public async Task Register_WhenModelStateIsInvalid_()
        {
            //arrange
            var controller = CreateSut();
            var model = new RegisterInputModel();
            controller.ModelState.AddModelError("", "Unit Test Error");
            //act
            var result = await controller.Register(model);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Equal(1, viewResult.ViewData.ModelState.ErrorCount);
        }

        [Fact]
        public async Task Register_WhenUserCannotBeFound_ExpectModelStateHassInvalidUsernameErrorMessage()
        {
            //arrange
            var controller = CreateSut();

            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(null as IdentityExpressUser);

            var model = new RegisterInputModel {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);

            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            Assert.Single(viewResult.ViewData.ModelState);
            Assert.Equal("Username does not exist", viewResult.ViewData.ModelState.Root.Errors[0].ErrorMessage);
        }

        [Fact]
        public async Task Register_WhenAddPasswordFails_ExpectUpdateToNotBeCalledAndModelStateToHaveErrors()
        {
            //arrange
            var controller = CreateSut();

            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityExpressUser());
            mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<IdentityExpressUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError {Description = "Error" }));
           
            var model = new RegisterInputModel
            {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);
            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(viewResult.ViewData.ModelState.IsValid);
            mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>()), Times.Never);
        }

        [Fact]
        public async Task Register_WhenAddPasswordSuccees_ExpectUpdateToHaveBeenCalled()
        {
            //arrange
            var controller = CreateSut();
            mockUserManager.Setup(x => x.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(new IdentityExpressUser());
            mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>())).ReturnsAsync(IdentityResult.Success);
            mockUserManager.Setup(x => x.AddPasswordAsync(It.IsAny<IdentityExpressUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var model = new RegisterInputModel
            {
                Username = "Hello"
            };

            //act
            var result = await controller.Register(model);
            //assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.True(viewResult.ViewData.ModelState.IsValid);
            mockUserManager.Verify(x => x.UpdateAsync(It.IsAny<IdentityExpressUser>()), Times.Once);
        }
    }
}
