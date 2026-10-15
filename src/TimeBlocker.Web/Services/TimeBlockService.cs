using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TimeBlocker.Web.Data;
using TimeBlocker.Web.Dtos;
using TimeBlocker.Web.Models;
using TimeBlocker.Web.Validation;

namespace TimeBlocker.Web.Services;

public sealed class TimeBlockService : ITimeBlockService
{
    private readonly AppDbContext _db;

    public TimeBlockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TimeBlock>> GetByDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _db.TimeBlocks
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Date == date)
            .OrderBy(b => b.StartTime)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<TimeBlock>> GetByIdAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var block = await _db.TimeBlocks
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);

        return block is null
            ? ServiceResult<TimeBlock>.NotFound()
            : ServiceResult<TimeBlock>.Success(block);
    }

    public async Task<ServiceResult<TimeBlock>> CreateAsync(string userId, ParsedTimeBlock input, CancellationToken cancellationToken = default)
    {
        var conflict = await FindOverlapAsync(userId, input, excludeId: null, cancellationToken);
        if (conflict is not null)
        {
            return ServiceResult<TimeBlock>.Conflict(BuildConflictMessage(conflict));
        }

        var block = new TimeBlock
        {
            UserId = userId,
            Title = input.Title,
            Date = input.Date,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            Notes = input.Notes
        };

        _db.TimeBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TimeBlock>.Success(block);
    }

    public async Task<ServiceResult<TimeBlock>> UpdateAsync(string userId, int id, ParsedTimeBlock input, CancellationToken cancellationToken = default)
    {
        var block = await _db.TimeBlocks
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);
        if (block is null)
        {
            return ServiceResult<TimeBlock>.NotFound();
        }

        var conflict = await FindOverlapAsync(userId, input, excludeId: id, cancellationToken);
        if (conflict is not null)
        {
            return ServiceResult<TimeBlock>.Conflict(BuildConflictMessage(conflict));
        }

        block.Title = input.Title;
        block.Date = input.Date;
        block.StartTime = input.StartTime;
        block.EndTime = input.EndTime;
        block.Notes = input.Notes;

        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<TimeBlock>.Success(block);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var block = await _db.TimeBlocks
            .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId, cancellationToken);
        if (block is null)
        {
            return ServiceResult<bool>.NotFound();
        }

        _db.TimeBlocks.Remove(block);
        await _db.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private Task<TimeBlock?> FindOverlapAsync(string userId, ParsedTimeBlock input, int? excludeId, CancellationToken cancellationToken)
    {
        var query = _db.TimeBlocks
            .AsNoTracking()
            .Where(b => b.UserId == userId
                        && b.Date == input.Date
                        && b.StartTime < input.EndTime
                        && b.EndTime > input.StartTime);

        if (excludeId is int excluded)
        {
            query = query.Where(b => b.Id != excluded);
        }

        return query
            .OrderBy(b => b.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string BuildConflictMessage(TimeBlock existing)
    {
        var start = existing.StartTime.ToString(TimeBlockResponseDto.TimeFormat, CultureInfo.InvariantCulture);
        var end = existing.EndTime.ToString(TimeBlockResponseDto.TimeFormat, CultureInfo.InvariantCulture);
        return $"Intervalul se suprapune cu „{existing.Title}” ({start}–{end}).";
    }
}
