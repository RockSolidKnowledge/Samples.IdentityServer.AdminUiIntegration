﻿using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace IdentityExpress.Manager.UI.Middleware
{
    public class XForwardedPrefixMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Request.Headers.TryGetValue("X-Forwarded-Prefix", out var pathBase))
            {
                context.Request.PathBase = Enumerable.Last<string>(pathBase);

                if (context.Request.Path.StartsWithSegments(context.Request.PathBase, out var path))
                {
                    context.Request.Path = path;
                }
            }
            await next(context);
        }
    }
}