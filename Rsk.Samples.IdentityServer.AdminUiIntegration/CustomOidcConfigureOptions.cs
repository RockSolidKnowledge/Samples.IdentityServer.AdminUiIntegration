using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration;

public class CustomOidcConfigureOptions : IConfigureNamedOptions<OpenIdConnectOptions>
{
    public void Configure(string name, OpenIdConnectOptions options)
    {
        Configure(options);
    }

    public void Configure(OpenIdConnectOptions options)
    {
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.None;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.None;
    }
}