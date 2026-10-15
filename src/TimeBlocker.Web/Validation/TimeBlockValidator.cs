using System.Globalization;
using TimeBlocker.Web.Dtos;
using TimeBlocker.Web.Models;

namespace TimeBlocker.Web.Validation;

public sealed record ParsedTimeBlock(
    string Title,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Notes);

public static class TimeBlockValidator
{
    public const int MinuteStep = 5;
    public const int MinDurationMinutes = 5;

    public static Dictionary<string, string[]> Validate(TimeBlockUpsertDto? dto, out ParsedTimeBlock parsed)
    {
        var errors = new Dictionary<string, List<string>>();

        void AddError(string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                list = new List<string>();
                errors[key] = list;
            }

            list.Add(message);
        }

        var title = dto?.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
        {
            AddError("title", "Titlul este obligatoriu.");
        }
        else if (title.Length > TimeBlock.TitleMaxLength)
        {
            AddError("title", $"Titlul poate avea maxim {TimeBlock.TitleMaxLength} de caractere.");
        }

        if (!TryParseDate(dto?.Date, out var date))
        {
            AddError("date", "Data este invalidă (format așteptat: yyyy-MM-dd).");
        }

        var startOk = TryParseTime(dto?.StartTime, out var start);
        if (!startOk)
        {
            AddError("startTime", "Ora de început este invalidă (format așteptat: HH:mm).");
        }
        else if (start.Minute % MinuteStep != 0)
        {
            AddError("startTime", $"Minutele trebuie să fie multiplu de {MinuteStep}.");
            startOk = false;
        }

        var endOk = TryParseTime(dto?.EndTime, out var end);
        if (!endOk)
        {
            AddError("endTime", "Ora de sfârșit este invalidă (format așteptat: HH:mm).");
        }
        else if (end.Minute % MinuteStep != 0)
        {
            AddError("endTime", $"Minutele trebuie să fie multiplu de {MinuteStep}.");
            endOk = false;
        }

        if (startOk && endOk)
        {
            if (end <= start)
            {
                AddError("endTime", "Ora de sfârșit trebuie să fie după ora de început.");
            }
            else if ((end - start).TotalMinutes < MinDurationMinutes)
            {
                AddError("endTime", $"Durata minimă este de {MinDurationMinutes} minute.");
            }
        }

        var notes = dto?.Notes?.Trim();
        if (string.IsNullOrEmpty(notes))
        {
            notes = null;
        }
        else if (notes.Length > TimeBlock.NotesMaxLength)
        {
            AddError("notes", $"Notițele pot avea maxim {TimeBlock.NotesMaxLength} de caractere.");
        }

        parsed = new ParsedTimeBlock(title, date, start, end, notes);

        return errors.ToDictionary(e => e.Key, e => e.Value.ToArray());
    }

    public static bool TryParseDate(string? value, out DateOnly date) =>
        DateOnly.TryParseExact(
            value,
            TimeBlockResponseDto.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value,
            TimeBlockResponseDto.TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);
}
