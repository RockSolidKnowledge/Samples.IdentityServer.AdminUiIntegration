using System;
using System.Threading.Tasks;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Models;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Services;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration.Controllers
{
    public class WebhookController : Controller
    {
        private readonly WebhookService webhookService;
        private readonly ILogger<WebhookController> logger;
        
        public WebhookController(ILogger<WebhookController> logger, UserManager<IdentityExpressUser> userManager)
        {
            webhookService = new WebhookService(userManager);
            
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Authorize("webhook")]
        [HttpPost]
        public async Task<IActionResult> ResetMfa([FromBody] WebhookModel dto)
        {
            if (string.IsNullOrEmpty(dto.Email))
            {
                logger.LogError("Cannot reset Mfa if email does not have value");
                return BadRequest("Email cannot be null");
            }

            var result = await webhookService.SendResetMfaEmail(dto.Username, CreateMfaResetLink);

            if (!result.Succeeded)
            {
                logger.LogError($"ResetMfa webhook failed: {result.ErrorMessage}");
                return BadRequest(result.ErrorMessage);
            }
            
            return Ok();
        }
        
        private Uri CreateMfaResetLink(string subject)
        {
            return new Uri(Url.Action("", "", new { subject }, Request.Scheme));
        }
    }
}