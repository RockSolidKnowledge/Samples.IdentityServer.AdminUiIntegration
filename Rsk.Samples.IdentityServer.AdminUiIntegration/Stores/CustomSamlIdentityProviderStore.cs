using Duende.IdentityServer.EntityFramework.Interfaces;
using Duende.IdentityServer.EntityFramework.Stores;
using Duende.IdentityServer.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rsk.AspNetCore.Authentication.Saml2p;
using Rsk.Saml.DuendeIdentityServer.DynamicProviders;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Stores;

public class CustomSamlIdentityProviderStore : IdentityProviderStore
{
    public CustomSamlIdentityProviderStore(
        IConfigurationDbContext context,
        ILogger<IdentityProviderStore> logger,
        ICancellationTokenProvider cancellationTokenProvider)
        : base(context, logger, cancellationTokenProvider)
    {
    }

    protected override Duende.IdentityServer.Models.IdentityProvider MapIdp(
        Duende.IdentityServer.EntityFramework.Entities.IdentityProvider idp)
    {
        if (idp != null && idp.Type == "saml2p")
        {
            var samlStored = JsonConvert.DeserializeObject<Saml2pAuthenticationOptions>(idp.Properties,
                SamlDynamicIdentityProvider.SerializerSettings);
                
            return  new SamlDynamicIdentityProvider()
            {
                Scheme = idp.Scheme,
                DisplayName = idp.DisplayName,
                Enabled = idp.Enabled,
                Type = idp.Type,
                SamlAuthenticationOptions = samlStored
            };
        }

        return base.MapIdp(idp);
    }
}