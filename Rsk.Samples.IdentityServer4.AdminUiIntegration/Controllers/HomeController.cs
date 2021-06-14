// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Middleware;
using Rsk.Samples.IdentityServer4.AdminUiIntegration.Models;

namespace Rsk.Samples.IdentityServer4.AdminUiIntegration.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService interaction;

        public HomeController(IIdentityServerInteractionService interaction)
        {
            this.interaction = interaction;
        }

        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identityserver
            var message = await interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;
            }

            return View("Error", vm);
        }
    }
}