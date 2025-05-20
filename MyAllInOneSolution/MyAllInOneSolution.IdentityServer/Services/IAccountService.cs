using System.Threading.Tasks;
using MyAllInOneSolution.IdentityServer.Models;

namespace MyAllInOneSolution.IdentityServer.Services;

public interface IAccountService
{
    Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl);
    LoginViewModel BuildLinkLoginViewModel(string returnUrl);
    Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model);
    Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId);
    Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId);
    RegisterViewModel BuildRegisterViewModel();
    RegisterViewModel BuildRegisterViewModel(RegisterInputModel model, bool success);
}