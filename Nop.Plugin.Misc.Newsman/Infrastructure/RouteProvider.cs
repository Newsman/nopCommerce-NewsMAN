using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.Newsman.Infrastructure
{
    /// <summary>
    /// Represents a plugin route provider
    /// </summary>
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(NewsmanDefaults.WebhookRoute,
                "Plugins/Newsman/Sync",
                new { controller = "Newsman", action = "Sync" });

            endpointRouteBuilder.MapControllerRoute(NewsmanDefaults.WebhookRoute,
                "Plugins/Newsman/Api",
                new { controller = "Newsman", action = "Api" });

            endpointRouteBuilder.MapControllerRoute(NewsmanDefaults.WebhookRoute,
                "Plugins/Newsman/Cart",
                new { controller = "Newsman", action = "Cart" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;

    }
}