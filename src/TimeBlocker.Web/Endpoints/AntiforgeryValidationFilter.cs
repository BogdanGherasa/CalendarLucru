using Microsoft.AspNetCore.Antiforgery;

namespace TimeBlocker.Web.Endpoints;

public sealed class AntiforgeryValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (HttpMethods.IsPost(httpContext.Request.Method)
            || HttpMethods.IsPut(httpContext.Request.Method)
            || HttpMethods.IsDelete(httpContext.Request.Method))
        {
            var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();

            try
            {
                await antiforgery.ValidateRequestAsync(httpContext);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.Problem(
                    title: "Token antiforgery invalid sau lipsă.",
                    statusCode: StatusCodes.Status400BadRequest);
            }
        }

        return await next(context);
    }
}
