using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using TimeBlocker.Web.Dtos;
using TimeBlocker.Web.Models;
using TimeBlocker.Web.Services;
using TimeBlocker.Web.Validation;

namespace TimeBlocker.Web.Endpoints;

public static class TimeBlockEndpoints
{
    public static IEndpointRouteBuilder MapTimeBlockEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/timeblocks")
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryValidationFilter>()
            .DisableAntiforgery();

        group.MapGet("/", ListAsync);
        group.MapGet("/{id:int}", GetAsync).WithName("GetTimeBlock");
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return routes;
    }

    private static async Task<Results<Ok<TimeBlockResponseDto[]>, ValidationProblem, UnauthorizedHttpResult>> ListAsync(
        string? date,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITimeBlockService service,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        if (!TimeBlockValidator.TryParseDate(date, out var parsedDate))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["date"] = ["Parametrul „date” lipsește sau este invalid (format așteptat: yyyy-MM-dd)."]
            });
        }

        var blocks = await service.GetByDateAsync(userId, parsedDate, cancellationToken);
        return TypedResults.Ok(blocks.Select(TimeBlockResponseDto.FromEntity).ToArray());
    }

    private static async Task<Results<Ok<TimeBlockResponseDto>, NotFound, UnauthorizedHttpResult>> GetAsync(
        int id,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITimeBlockService service,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.GetByIdAsync(userId, id, cancellationToken);
        return result is { IsSuccess: true, Value: { } block }
            ? TypedResults.Ok(TimeBlockResponseDto.FromEntity(block))
            : TypedResults.NotFound();
    }

    private static async Task<Results<CreatedAtRoute<TimeBlockResponseDto>, ValidationProblem, ProblemHttpResult, UnauthorizedHttpResult>> CreateAsync(
        TimeBlockUpsertDto dto,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITimeBlockService service,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var errors = TimeBlockValidator.Validate(dto, out var parsed);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await service.CreateAsync(userId, parsed, cancellationToken);
        return result.Status switch
        {
            ServiceResultStatus.Success when result.Value is { } block =>
                TypedResults.CreatedAtRoute(TimeBlockResponseDto.FromEntity(block), "GetTimeBlock", new { id = block.Id }),
            ServiceResultStatus.Conflict => Conflict(result.Message),
            ServiceResultStatus.Invalid when result.Errors is { } serviceErrors => TypedResults.ValidationProblem(serviceErrors),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<Results<Ok<TimeBlockResponseDto>, ValidationProblem, NotFound, ProblemHttpResult, UnauthorizedHttpResult>> UpdateAsync(
        int id,
        TimeBlockUpsertDto dto,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITimeBlockService service,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var errors = TimeBlockValidator.Validate(dto, out var parsed);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var result = await service.UpdateAsync(userId, id, parsed, cancellationToken);
        return result.Status switch
        {
            ServiceResultStatus.Success when result.Value is { } block => TypedResults.Ok(TimeBlockResponseDto.FromEntity(block)),
            ServiceResultStatus.NotFound => TypedResults.NotFound(),
            ServiceResultStatus.Conflict => Conflict(result.Message),
            ServiceResultStatus.Invalid when result.Errors is { } serviceErrors => TypedResults.ValidationProblem(serviceErrors),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> DeleteAsync(
        int id,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        ITimeBlockService service,
        CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(principal);
        if (userId is null)
        {
            return TypedResults.Unauthorized();
        }

        var result = await service.DeleteAsync(userId, id, cancellationToken);
        return result.IsSuccess
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static ProblemHttpResult Conflict(string? message) =>
        TypedResults.Problem(
            title: "Suprapunere de intervale.",
            detail: message,
            statusCode: StatusCodes.Status409Conflict);
}
