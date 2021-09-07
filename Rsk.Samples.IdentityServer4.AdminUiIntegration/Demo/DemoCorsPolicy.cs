using System.Threading.Tasks;
using Duende.IdentityServer.Services;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration.Demo
{
    /// <summary>
    /// Allows any CORS origin - DO NOT USE IN PRODUCTION
    /// </summary>
    public class DemoCorsPolicy : ICorsPolicyService
    {
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            return Task.FromResult(true);
        }
    }
}