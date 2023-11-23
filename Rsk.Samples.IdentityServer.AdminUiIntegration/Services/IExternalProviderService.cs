using System.Collections.Generic;
using System.Threading.Tasks;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services;

public interface IExternalProviderService
{
    Task<IEnumerable<ExternalProvider>> GetAll();
    Task<ExternalProvider> GetScheme(string scheme);
}