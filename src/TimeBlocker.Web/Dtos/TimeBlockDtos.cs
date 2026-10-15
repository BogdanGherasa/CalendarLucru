using System.Globalization;
using TimeBlocker.Web.Models;

namespace TimeBlocker.Web.Dtos;

public sealed record TimeBlockUpsertDto(
    string Title,
    string Date,
    string StartTime,
    string EndTime,
    string? Notes);

public sealed record TimeBlockResponseDto(
    int Id,
    string Title,
    string Date,
    string StartTime,
    string EndTime,
    string? Notes,
    DateTime UpdatedAtUtc)
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";

    public static TimeBlockResponseDto FromEntity(TimeBlock block) => new(
        block.Id,
        block.Title,
        block.Date.ToString(DateFormat, CultureInfo.InvariantCulture),
        block.StartTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
        block.EndTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
        block.Notes,
        DateTime.SpecifyKind(block.UpdatedAtUtc, DateTimeKind.Utc));
}
