﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services
{
    public class AccountService: IAccountService
    {
        private readonly IClientStore clientStore;
        private readonly IIdentityServerInteractionService interaction;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IAuthenticationSchemeProvider schemeProvider;
        private readonly IExternalProviderService externalProviderService;
        private readonly IAuthenticationHandlerProvider authenticationHandlerProvider;

        public AccountService(
            IIdentityServerInteractionService interaction,
            IHttpContextAccessor httpContextAccessor,
            IAuthenticationSchemeProvider schemeProvider,
            IClientStore clientStore,
            IExternalProviderService externalProviderService,
            IAuthenticationHandlerProvider authenticationHandlerProvider)
        {
            this.interaction = interaction;
            this.httpContextAccessor = httpContextAccessor;
            this.schemeProvider = schemeProvider;
            this.clientStore = clientStore;
            this.externalProviderService = externalProviderService;
            this.authenticationHandlerProvider = authenticationHandlerProvider;
        }
        
        public async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Username = context.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var providers = await externalProviderService.GetAll();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                EnableLocalLogin = allowLocal,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        public LoginViewModel BuildLinkLoginViewModel(string returnUrl)
        {
            return new LoginViewModel
            {
                EnableLocalLogin = true,
                LinkSetup = true,
                ReturnUrl = returnUrl,
            };
        }

        public async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.Username = model.Username;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        public async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = true };

            if (httpContextAccessor.HttpContext.User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        public async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = true,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            var user = httpContextAccessor?.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var idp = user.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    var authenticationHandler = await authenticationHandlerProvider.GetHandlerAsync(this.httpContextAccessor.HttpContext, idp);

                    if (authenticationHandler != null)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await interaction.CreateLogoutContextAsync();
                        }
                        
                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        public RegisterViewModel BuildRegisterViewModel()
        {
            var vm = new RegisterViewModel();
            return vm;
        }

        public RegisterViewModel BuildRegisterViewModel(RegisterInputModel model, bool success)
        {
            RegisterViewModel newModel = new RegisterViewModel
            {
                IsSuccess = success,
                Username = model.Username,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword
            };
            return newModel;
        }
    }
}
