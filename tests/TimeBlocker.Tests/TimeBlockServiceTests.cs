using TimeBlocker.Web.Services;
using TimeBlocker.Web.Validation;

namespace TimeBlocker.Tests;

public sealed class TimeBlockServiceTests : IDisposable
{
    private static readonly DateOnly Day = new(2026, 10, 15);

    private readonly TestDbFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private TimeBlockService CreateService() => new(_factory.CreateContext());

    private static ParsedTimeBlock Block(string start, string end, string title = "Bloc", DateOnly? date = null, string? notes = null) =>
        new(title, date ?? Day, TimeOnly.Parse(start), TimeOnly.Parse(end), notes);

    [Fact]
    public async Task Create_ThenGetByDate_ReturnsBlocksSortedByStartTime()
    {
        var service = CreateService();
        Assert.True((await service.CreateAsync(TestDbFactory.UserAId, Block("14:00", "15:00", "C"))).IsSuccess);
        Assert.True((await service.CreateAsync(TestDbFactory.UserAId, Block("08:00", "09:00", "A"))).IsSuccess);
        Assert.True((await service.CreateAsync(TestDbFactory.UserAId, Block("10:30", "11:00", "B"))).IsSuccess);
        Assert.True((await service.CreateAsync(TestDbFactory.UserAId, Block("09:00", "10:00", "Altă zi", Day.AddDays(1)))).IsSuccess);

        var blocks = await CreateService().GetByDateAsync(TestDbFactory.UserAId, Day);

        Assert.Equal(["A", "B", "C"], blocks.Select(b => b.Title));
    }

    [Fact]
    public async Task Create_SetsTimestamps()
    {
        var result = await CreateService().CreateAsync(TestDbFactory.UserAId, Block("08:00", "09:00"));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(default, result.Value!.CreatedAtUtc);
        Assert.Equal(result.Value.CreatedAtUtc, result.Value.UpdatedAtUtc);
    }

    [Theory]
    [InlineData("10:30", "11:30")]
    [InlineData("09:30", "10:30")]
    [InlineData("10:15", "10:45")]
    [InlineData("09:00", "12:00")]
    [InlineData("10:00", "11:00")]
    public async Task Create_OverlappingInterval_ReturnsConflict(string start, string end)
    {
        var service = CreateService();
        await service.CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00", "Existent"));

        var result = await CreateService().CreateAsync(TestDbFactory.UserAId, Block(start, end));

        Assert.Equal(ServiceResultStatus.Conflict, result.Status);
        Assert.Contains("Existent", result.Message);
        Assert.Contains("10:00", result.Message);
        Assert.Contains("11:00", result.Message);
    }

    [Theory]
    [InlineData("11:00", "12:00")]
    [InlineData("09:00", "10:00")]
    public async Task Create_TouchingEdges_IsAllowed(string start, string end)
    {
        var service = CreateService();
        await service.CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00"));

        var result = await CreateService().CreateAsync(TestDbFactory.UserAId, Block(start, end));

        Assert.Equal(ServiceResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task Update_DoesNotConflictWithItself()
    {
        var created = await CreateService().CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00"));

        var result = await CreateService().UpdateAsync(
            TestDbFactory.UserAId,
            created.Value!.Id,
            Block("10:30", "11:30", "Mutat", notes: "notă"));

        Assert.Equal(ServiceResultStatus.Success, result.Status);
        var reloaded = await CreateService().GetByIdAsync(TestDbFactory.UserAId, created.Value.Id);
        Assert.Equal("Mutat", reloaded.Value!.Title);
        Assert.Equal(new TimeOnly(10, 30), reloaded.Value.StartTime);
        Assert.Equal("notă", reloaded.Value.Notes);
    }

    [Fact]
    public async Task Update_OverlappingAnotherBlock_ReturnsConflict()
    {
        var service = CreateService();
        await service.CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00"));
        var second = await service.CreateAsync(TestDbFactory.UserAId, Block("12:00", "13:00"));

        var result = await CreateService().UpdateAsync(TestDbFactory.UserAId, second.Value!.Id, Block("10:30", "12:30"));

        Assert.Equal(ServiceResultStatus.Conflict, result.Status);
    }

    [Fact]
    public async Task UserB_CannotReadUpdateOrDelete_UserABlock()
    {
        var created = await CreateService().CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00", "Privat"));
        var id = created.Value!.Id;

        var get = await CreateService().GetByIdAsync(TestDbFactory.UserBId, id);
        var update = await CreateService().UpdateAsync(TestDbFactory.UserBId, id, Block("12:00", "13:00", "Furat"));
        var delete = await CreateService().DeleteAsync(TestDbFactory.UserBId, id);
        var list = await CreateService().GetByDateAsync(TestDbFactory.UserBId, Day);

        Assert.Equal(ServiceResultStatus.NotFound, get.Status);
        Assert.Equal(ServiceResultStatus.NotFound, update.Status);
        Assert.Equal(ServiceResultStatus.NotFound, delete.Status);
        Assert.Empty(list);

        var stillThere = await CreateService().GetByIdAsync(TestDbFactory.UserAId, id);
        Assert.Equal("Privat", stillThere.Value!.Title);
        Assert.Equal(new TimeOnly(10, 0), stillThere.Value.StartTime);
    }

    [Fact]
    public async Task OverlapBetweenDifferentUsers_IsAllowed()
    {
        await CreateService().CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00"));

        var result = await CreateService().CreateAsync(TestDbFactory.UserBId, Block("10:00", "11:00"));

        Assert.Equal(ServiceResultStatus.Success, result.Status);
    }

    [Fact]
    public async Task Delete_Existing_RemovesBlock()
    {
        var created = await CreateService().CreateAsync(TestDbFactory.UserAId, Block("10:00", "11:00"));

        var result = await CreateService().DeleteAsync(TestDbFactory.UserAId, created.Value!.Id);

        Assert.Equal(ServiceResultStatus.Success, result.Status);
        Assert.Empty(await CreateService().GetByDateAsync(TestDbFactory.UserAId, Day));
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        var result = await CreateService().DeleteAsync(TestDbFactory.UserAId, 9999);

        Assert.Equal(ServiceResultStatus.NotFound, result.Status);
    }
}
