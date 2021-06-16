// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Microsoft.Extensions.Logging;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration.Services
{
    public class ConsentService
    {
        private readonly IIdentityServerInteractionService interaction;
        private readonly IEventService events;
        private readonly ILogger logger;

        public ConsentService(
            IIdentityServerInteractionService interaction,
            IEventService events,
            ILogger logger)
        {
            this.interaction = interaction;
            this.events = events;
            this.logger = logger;
        }

        public async Task<ProcessConsentResult> ProcessConsent(ConsentInputModel model, string userId)
        {
            var result = new ProcessConsentResult();

            // validate return url is still valid
            var request = await interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (request == null) return result;

            ConsentResponse grantedConsent = null;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (model.Button == "no")
            {
                grantedConsent = new ConsentResponse {Error = AuthorizationError.AccessDenied};

                // emit event
                await events.RaiseAsync(new ConsentDeniedEvent(userId, request.Client.ClientId,
                    request.ValidatedResources.RawScopeValues));
            }
            // user clicked 'yes' - validate the data
            else if (model.Button == "yes")
            {
                // if the user consented to some scope, build the response model
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    var scopes = model.ScopesConsented;
                    scopes = scopes.Where(x =>
                        x != IdentityServerConstants.StandardScopes.OfflineAccess);

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = model.Description
                    };

                    // emit event
                    await events.RaiseAsync(new ConsentGrantedEvent(userId, request.Client.ClientId,
                        request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented,
                        grantedConsent.RememberConsent));
                }
                else
                {
                    result.ValidationError = "You must pick at least one permission";
                }
            }
            else
            {
                result.ValidationError = "Invalid selection";
            }

            if (grantedConsent != null)
            {
                // communicate outcome of consent back to identityserver
                await interaction.GrantConsentAsync(request, grantedConsent);

                // indicate that's it ok to redirect back to authorization endpoint
                result.RedirectUri = model.ReturnUrl;
                result.Client = request.Client;
            }
            else
            {
                // we need to redisplay the consent UI
                result.ViewModel = await BuildViewModelAsync(model.ReturnUrl, model);
            }

            return result;
        }

        public async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null)
        {
            var request = await interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null)
            {
                return CreateConsentViewModel(model, returnUrl, request);
            }

            logger.LogError("No consent request matching request: {0}", returnUrl);
            return null;
        }

        private ConsentViewModel CreateConsentViewModel(ConsentInputModel model, string returnUrl, AuthorizationRequest request)
        {
            var vm = new ConsentViewModel
            {
                RememberConsent = model?.RememberConsent ?? true,
                ScopesConsented = model?.ScopesConsented ?? Enumerable.Empty<string>(),
                Description = model?.Description,

                ReturnUrl = returnUrl,

                ClientName = request.Client.ClientName ?? request.Client.ClientId,
                ClientUrl = request.Client.ClientUri,
                ClientLogoUrl = request.Client.LogoUri,
                AllowRememberConsent = request.Client.AllowRememberConsent
            };

            vm.IdentityScopes = request.ValidatedResources.Resources.IdentityResources.Select(x =>
                CreateScopeViewModel(x, vm.ScopesConsented.Contains(x.Name) || model == null)).ToArray();

            var apiScopes = new List<ScopeViewModel>();
            foreach (var parsedScope in request.ValidatedResources.ParsedScopes)
            {
                var apiScope = request.ValidatedResources.Resources.FindApiScope(parsedScope.ParsedName);
                if (apiScope != null)
                {
                    var scopeVm = CreateScopeViewModel(parsedScope, apiScope,
                        vm.ScopesConsented.Contains(parsedScope.RawValue) || model == null);
                    apiScopes.Add(scopeVm);
                }
            }

            if (request.ValidatedResources.Resources.OfflineAccess)
            {
                apiScopes.Add(GetOfflineAccessScope(
                    vm.ScopesConsented.Contains(IdentityServerConstants.StandardScopes.OfflineAccess) ||
                    model == null));
            }

            vm.ApiScopes = apiScopes;

            return vm;
        }

        private ScopeViewModel CreateScopeViewModel(IdentityResource identity, bool check)
        {
            {
                return new ScopeViewModel
                {
                    Value = identity.Name,
                    DisplayName = identity.DisplayName ?? identity.Name,
                    Description = identity.Description,
                    Emphasize = identity.Emphasize,
                    Required = identity.Required,
                    Checked = check || identity.Required
                };
            }
        }


        public ScopeViewModel CreateScopeViewModel(ParsedScopeValue parsedScopeValue, ApiScope apiScope, bool check)
        {
            var displayName = apiScope.DisplayName ?? apiScope.Name;
            if (!String.IsNullOrWhiteSpace(parsedScopeValue.ParsedParameter))
            {
                displayName += ":" + parsedScopeValue.ParsedParameter;
            }

            return new ScopeViewModel
            {
                Value = parsedScopeValue.RawValue,
                DisplayName = displayName,
                Description = apiScope.Description,
                Emphasize = apiScope.Emphasize,
                Required = apiScope.Required,
                Checked = check || apiScope.Required
            };
        }

        private ScopeViewModel GetOfflineAccessScope(bool check)
        {
            return new ScopeViewModel
            {
                Value = IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = "Offline Access",
                Description = "Access to your applications and resources, even when you are offline",
                Emphasize = true,
                Checked = check
            };
        }
    }
}