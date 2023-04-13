// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using IdentityExpress.Identity;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Middleware;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Services;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Controllers
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityExpressUser> userManager;
        private readonly IIdentityServerInteractionService interaction;
        private readonly IEventService events;
        private readonly IAccountService accountService;
        private readonly IUrlHelperFactory urlHelperFactory;
        private readonly IIdentityProviderStore identityProviderStore;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityExpressUser> userManager,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            IAccountService accountService,
            IUrlHelperFactory urlHelperFactory,
            IIdentityProviderStore identityProviderStore)
        {
            this.interaction = interaction;
            this.events = events;
            this.urlHelperFactory = urlHelperFactory;
            this.accountService = accountService;
            this.userManager = userManager;
            this.identityProviderStore = identityProviderStore;
        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            bool externalLogin = Request.Cookies["Identity.External"] != null;
            
            // build a model so we know what to show on the login page
            // if were told we a linking an external login then then we build a model 
            var vm = !externalLogin ? await accountService.BuildLoginViewModelAsync(returnUrl)
                    : accountService.BuildLinkLoginViewModel(returnUrl);

            if (!externalLogin && vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return ExternalLogin(vm.ExternalLoginScheme, returnUrl);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            if (button != "login")
            {
                // the user clicked the "cancel" button
                var context = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                    
                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Redirect(model.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            if (ModelState.IsValid)
            {
                var user = await userManager.FindByNameAsync(model.Username);

                if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
                {
                    await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

                    // only set explicit expiration here if user chooses "remember me". 
                    // otherwise we rely upon expiration configured in cookie middleware.
                    AuthenticationProperties props = null;
                    if (model.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(30))
                        };
                    }

                    var isuser = new IdentityServerUser(user.Id)
                    {
                        DisplayName = user.UserName,
                        AdditionalClaims = new List<Claim>
                        {
                            new Claim("AspNet.Identity.SecurityStamp", user.SecurityStamp)
                        }
                    };

                    // issue authentication cookie with subject ID and username
                    await events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));
                    await HttpContext.SignInAsync(isuser, props);

                    // link external login if cookie exists
                    await LinkIfExternalLogin(user);

                    // make sure the returnUrl is still valid, and if so redirect back to authorize endpoint or a local page
                    if (interaction.IsValidReturnUrl(model.ReturnUrl) || Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                await events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials"));

                ModelState.AddModelError("", "Invalid username or password");
            }

            // something went wrong, show form with error
            var vm = await accountService.BuildLoginViewModelAsync(model);
            vm.LinkSetup = Request.Cookies["Identity.External"] != null;
            return View(vm);
        }

        /// <summary>
        /// initiate roundtrip to external authentication provider
        /// </summary>
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl)
        {
            var urlHelper = urlHelperFactory.GetUrlHelper(ControllerContext);
            var props = new AuthenticationProperties
            {
                RedirectUri = urlHelper.Action("ExternalLoginCallback"),
                Items =
                {
                    { "returnUrl", returnUrl }
                }
            };

            // challenge specific authentication middleware
            props.Items.Add("scheme", provider);
            return Challenge(props, provider);
        }

        /// <summary>
        /// Post processing of external authentication
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            // get external identity from external scheme cookie
            var result = await HttpContext.AuthenticateAsync("Identity.External");
            if (result?.Succeeded != true) throw new Exception("External authentication error");

            var externalUser = result.Principal;
            var claims = externalUser.Claims.ToList();

            // try to determine the unique id of the external user
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject) ?? claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new Exception("Unknown userid");

            claims.Remove(userIdClaim);
            var userId = userIdClaim.Value;
            var provider = result.Properties.Items["scheme"];
            
            var returnUrl = result.Properties.Items["returnUrl"];
            if (!interaction.IsValidReturnUrl(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "~/";
            }

            // check if the external user is already provisioned
            var user = await userManager.FindByLoginAsync(provider, userId);

            if (user == null)
            {
                return RedirectToAction("Login", new { returnUrl });
            }

            var additionalClaims = new List<Claim>();

            // if the external system sent a session id claim, copy it over
            var sid = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
            if (sid != null) additionalClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));

            // if the external provider issued an id_token, we'll keep it for signout
            AuthenticationProperties props = null;
            var idToken = result.Properties.GetTokenValue("id_token");
            if (idToken != null)
            {
                props = new AuthenticationProperties();
                props.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
            }

            var isuser = new IdentityServerUser(user.Id)
            {
                DisplayName = user.UserName,
                AdditionalClaims = additionalClaims
            };

            // issue local authentication cookie for user
            await events.RaiseAsync(new UserLoginSuccessEvent(provider, userId, user.Id, user.UserName));
            await HttpContext.SignInAsync(isuser, props);

            // delete temporary cookie used during external authentication
            await HttpContext.SignOutAsync("Identity.External");

            return Redirect(returnUrl);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await accountService.BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await accountService.BuildLoggedOutViewModelAsync(model.LogoutId);

            var user = HttpContext.User;
            if (user?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                // raise the logout event
                await events.RaiseAsync(new UserLogoutSuccessEvent(user.GetSubjectId(), user.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult Register()
        {
            var vm = accountService.BuildRegisterViewModel();
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] RegisterInputModel model)
        {
            var vm = accountService.BuildRegisterViewModel(model, false);
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var user = await userManager.FindByNameAsync(model.Username);

            if (user == null)
            {
                ModelState.AddModelError("", "Username does not exist");
                vm = accountService.BuildRegisterViewModel(model, false);
                return View(vm);
            }

            var result = await userManager.AddPasswordAsync(user, model.Password);

            if (result.Succeeded)
            {
                result = await userManager.UpdateAsync(user);
            }

            if (!result.Succeeded)
            {
                foreach (var item in result.Errors)
                {
                    ModelState.AddModelError("", item.Description);
                }

            }

            vm = accountService.BuildRegisterViewModel(model, result.Succeeded);

            return View(vm);
        }

        private async Task<IdentityResult> LinkIfExternalLogin(IdentityExpressUser localUser)
        {
            // get external identity from external scheme cookie
            var result = await HttpContext.AuthenticateAsync("Identity.External");
            if (result?.Succeeded != true) return null;

            var externalUser = result.Principal;
            var claims = externalUser.Claims.ToList();

            // try to determine the unique id of the external user
            var userIdClaim = claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Subject) ?? claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null) throw new Exception("Unknown userid");

            claims.Remove(userIdClaim);
            var userId = userIdClaim.Value;
            var providerScheme = result.Properties.Items["scheme"];
            
            var provider = await identityProviderStore.GetBySchemeAsync(providerScheme);
            var outcome =  await userManager.AddLoginAsync(localUser, new UserLoginInfo(provider.Scheme, userId, provider.DisplayName));
            await HttpContext.SignOutAsync("Identity.External");
            return outcome;
        }
        
    }
}