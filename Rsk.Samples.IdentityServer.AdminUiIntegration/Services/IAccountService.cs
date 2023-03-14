using System.Threading.Tasks;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Services;

public interface IAccountService
{
    Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl);
    Task<LoginViewModel> BuildLinkLoginViewModel(string returnUrl);
    Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model);
    Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId);
    Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId);
    RegisterViewModel BuildRegisterViewModel();
    RegisterViewModel BuildRegisterViewModel(RegisterInputModel model, bool success);
}