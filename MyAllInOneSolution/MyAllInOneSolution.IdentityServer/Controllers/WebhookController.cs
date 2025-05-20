using System;
using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyAllInOneSolution.IdentityServer.Models;
using MyAllInOneSolution.IdentityServer.Services;

namespace MyAllInOneSolution.IdentityServer.Controllers
{
    [Route("[controller]")]
    public class WebhookController : Controller
    {
        private readonly WebhookService webhookService;
        private readonly ILogger<WebhookController> logger;
        private readonly ISessionManagementService sessionManagementService;
        
        public WebhookController(ILogger<WebhookController> logger, UserManager<IdentityExpressUser> userManager, ISessionManagementService sessionManagementService)
        {
            webhookService = new WebhookService(userManager);
            
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.sessionManagementService = sessionManagementService ?? throw new ArgumentNullException(nameof(sessionManagementService));
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

        [HttpDelete("deletesession/{id}")]
        public async Task<IActionResult> DeleteServerSideSession([FromRoute] string id)
        {
            ArgumentNullException.ThrowIfNull(id);

            await sessionManagementService.RemoveSessionsAsync(new RemoveSessionsContext
            {
                SessionId = id
            });

            return Ok();
        }
        
        private Uri CreateMfaResetLink(string subject)
        {
            return new Uri(Url.Action("", "", new { subject }, Request.Scheme));
        }
    }
}