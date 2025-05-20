using System;
using System.Threading.Tasks;
using IdentityExpress.Identity;
using Microsoft.AspNetCore.Identity;
using MyAllInOneSolution.IdentityServer.Models;

namespace MyAllInOneSolution.IdentityServer.Services
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
            
            // -- Suggested Flow -- //
            
            //1. Send an email to your user wanting to reset their MFA
            //2. The user will click the link and then be prompted to log into IdentityServer
            //3. After successfully logging in, they will be prompted to create a new MFA provider
            //4. Previous MFA providers will then be removed
            
            return SendResetMfaOneTimeLinkResult.Success();
        }
    }
}