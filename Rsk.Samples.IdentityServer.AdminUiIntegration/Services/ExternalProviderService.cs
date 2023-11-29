using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services;

public class ExternalProviderService: IExternalProviderService
{
    private readonly IAuthenticationSchemeProvider schemeProvider;
    private readonly IIdentityProviderStore identityProviderStore;
    private readonly IConfiguration configuration;

    public ExternalProviderService(
        IAuthenticationSchemeProvider schemeProvider,
        IIdentityProviderStore identityProviderStore,
        IConfiguration configuration)
    {
        this.schemeProvider = schemeProvider ?? throw new ArgumentNullException(nameof(schemeProvider));
        this.identityProviderStore = identityProviderStore ?? throw new ArgumentNullException(nameof(identityProviderStore));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public async Task<IEnumerable<ExternalProvider>> GetAll()
    {
        var mode = configuration.GetValue<string>("DynamicAuth:Mode");
        var providers = new List<ExternalProvider>();

        switch (mode)
        {
            case "Rsk":
                providers.AddRange(await GetRskSchemes());
                break;
            case "Duende":
                providers.AddRange(await GetDuendeSchemes());
                break;
        }
        
        return providers;
    }

    public async Task<ExternalProvider> GetScheme(string scheme)
    {
        return (await GetAll()).FirstOrDefault(x => x.AuthenticationScheme == scheme);
    }

    private async Task<IEnumerable<ExternalProvider>> GetRskSchemes()
    {
        var schemes = await schemeProvider.GetAllSchemesAsync();
        
        return schemes
            .Where(x => x.DisplayName != null)
            .Select(x => new ExternalProvider
            {
                DisplayName = x.DisplayName ?? x.Name,
                AuthenticationScheme = x.Name
            }).ToList();
    }

    private async Task<IEnumerable<ExternalProvider>> GetDuendeSchemes()
    {
        return (await identityProviderStore.GetAllSchemeNamesAsync())
            .Where(x => x.Enabled)
            .Select(x => new ExternalProvider
            {
                AuthenticationScheme = x.Scheme,
                DisplayName = x.DisplayName
            });
    }
}