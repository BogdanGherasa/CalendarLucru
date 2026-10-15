using TimeBlocker.Web.Dtos;
using TimeBlocker.Web.Validation;

namespace TimeBlocker.Tests;

public class TimeBlockValidatorTests
{
    private static TimeBlockUpsertDto Valid(
        string title = "Ședință",
        string date = "2026-10-15",
        string start = "10:00",
        string end = "11:00",
        string? notes = "Agenda https://example.com") =>
        new(title, date, start, end, notes);

    [Fact]
    public void ValidDto_ReturnsNoErrors_AndParsesValues()
    {
        var errors = TimeBlockValidator.Validate(Valid(title: "  Ședință  ", notes: "  note  "), out var parsed);

        Assert.Empty(errors);
        Assert.Equal("Ședință", parsed.Title);
        Assert.Equal(new DateOnly(2026, 10, 15), parsed.Date);
        Assert.Equal(new TimeOnly(10, 0), parsed.StartTime);
        Assert.Equal(new TimeOnly(11, 0), parsed.EndTime);
        Assert.Equal("note", parsed.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_IsInvalid(string title)
    {
        var errors = TimeBlockValidator.Validate(Valid(title: title), out _);

        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void TitleWith121Characters_IsInvalid()
    {
        var errors = TimeBlockValidator.Validate(Valid(title: new string('a', 121)), out _);

        Assert.True(errors.ContainsKey("title"));
    }

    [Fact]
    public void TitleWith120Characters_IsValid()
    {
        var errors = TimeBlockValidator.Validate(Valid(title: new string('a', 120)), out _);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("2026-13-01")]
    [InlineData("15.10.2026")]
    [InlineData("2026-02-30")]
    [InlineData("")]
    public void InvalidDate_IsInvalid(string date)
    {
        var errors = TimeBlockValidator.Validate(Valid(date: date), out _);

        Assert.True(errors.ContainsKey("date"));
    }

    [Theory]
    [InlineData("25:00", "26:00", "startTime")]
    [InlineData("9:00", "10:00", "startTime")]
    [InlineData("10:00", "abc", "endTime")]
    public void InvalidTime_IsInvalid(string start, string end, string expectedKey)
    {
        var errors = TimeBlockValidator.Validate(Valid(start: start, end: end), out _);

        Assert.True(errors.ContainsKey(expectedKey));
    }

    [Fact]
    public void EndEqualToStart_IsInvalid()
    {
        var errors = TimeBlockValidator.Validate(Valid(start: "10:00", end: "10:00"), out _);

        Assert.True(errors.ContainsKey("endTime"));
    }

    [Fact]
    public void EndBeforeStart_IsInvalid()
    {
        var errors = TimeBlockValidator.Validate(Valid(start: "11:00", end: "10:00"), out _);

        Assert.True(errors.ContainsKey("endTime"));
    }

    [Theory]
    [InlineData("10:03", "11:00", "startTime")]
    [InlineData("10:00", "11:07", "endTime")]
    public void MinutesNotMultipleOfFive_AreInvalid(string start, string end, string expectedKey)
    {
        var errors = TimeBlockValidator.Validate(Valid(start: start, end: end), out _);

        Assert.True(errors.ContainsKey(expectedKey));
    }

    [Fact]
    public void NotesWith5001Characters_IsInvalid()
    {
        var errors = TimeBlockValidator.Validate(Valid(notes: new string('n', 5001)), out _);

        Assert.True(errors.ContainsKey("notes"));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData(null)]
    public void WhitespaceNotes_BecomeNull(string? notes)
    {
        var errors = TimeBlockValidator.Validate(Valid(notes: notes), out var parsed);

        Assert.Empty(errors);
        Assert.Null(parsed.Notes);
    }
}
