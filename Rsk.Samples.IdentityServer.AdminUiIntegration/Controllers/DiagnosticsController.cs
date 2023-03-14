using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Middleware;
using Rsk.Samples.IdentityServer.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer.AdminUiIntegration.Controllers
{
    [Authorize]
    [SecurityHeaders]
    public class DiagnosticsController : Controller
    {
        public async Task<IActionResult> Index()
        {
            var model = new DiagnosticsViewModel(await HttpContext.AuthenticateAsync());
            return View(model);
        }
    }
}