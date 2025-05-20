// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc;
using MyAllInOneSolution.IdentityServer.Middleware;
using MyAllInOneSolution.IdentityServer.Models;
using MyAllInOneSolution.IdentityServer.Services;

namespace MyAllInOneSolution.IdentityServer.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService interaction;
        private readonly IEventStore eventStore;

        public HomeController(IIdentityServerInteractionService interaction, IEventStore eventStore)
        {
            this.interaction = interaction;
            this.eventStore = eventStore;
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
            
            // retrieve error details from IdentityServer
            var message = await interaction.GetErrorContextAsync(errorId);
            
            //try and get more details regarding the error from the IdentityServer event cache
            var cachedEventInformation = eventStore.GetEventByTraceID(HttpContext.TraceIdentifier);
            
            if (message != null)
            {
                vm.Error = message;
                vm.EventStoreMessage = cachedEventInformation;
            }

            return View("Error", vm);
        }
    }
}