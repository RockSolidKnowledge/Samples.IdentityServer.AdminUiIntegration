using System;
using System.Threading.Tasks;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Identity;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration.Services
{
    public class WebhookService
    {
        private readonly UserManager<IdentityExpressUser> userManager;

        public WebhookService(UserManager<IdentityExpressUser> userManager)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }
        
        public async Task<SendResetMfaOneTimeLinkResult> SendResetMfaEmail(string username, Func<string, Uri> registrationUrl)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            if (registrationUrl == null) throw new ArgumentNullException(nameof(registrationUrl));
            
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                return SendResetMfaOneTimeLinkResult.Failed($"No user exists with username: {username}");
            }
            
            //send the one time link email to the user here
            
            return SendResetMfaOneTimeLinkResult.Success();
        }
    }
}