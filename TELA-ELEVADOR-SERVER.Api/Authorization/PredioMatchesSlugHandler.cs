using Microsoft.AspNetCore.Authorization;

namespace TELA_ELEVADOR_SERVER.Api.Authorization;

public sealed class PredioMatchesSlugHandler : AuthorizationHandler<PredioMatchesSlugRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PredioMatchesSlugRequirement requirement)
    {
        if (context.Resource is HttpContext httpContext)
        {
            var roleClaim = context.User.FindFirst("role")?.Value;
            if (string.Equals(roleClaim, "Developer", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            var slugClaim = context.User.FindFirst("slug")?.Value;
            if (!string.IsNullOrWhiteSpace(slugClaim)
                && httpContext.Request.RouteValues.TryGetValue("slug", out var routeSlug)
                && routeSlug is string routeSlugValue
                && string.Equals(slugClaim, routeSlugValue, StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
