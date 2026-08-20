using System.Threading;
using System.Threading.Tasks;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services;

public interface IAccountService
{
    Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, CancellationToken cancellationToken);
    LoginViewModel BuildLinkLoginViewModel(string returnUrl);
    Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model, CancellationToken cancellationToken);
    Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId, CancellationToken cancellationToken);
    Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId, CancellationToken cancellationToken);
    RegisterViewModel BuildRegisterViewModel();
    RegisterViewModel BuildRegisterViewModel(RegisterInputModel model, bool success);
}