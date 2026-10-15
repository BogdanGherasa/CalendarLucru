using TimeBlocker.Web.Models;
using TimeBlocker.Web.Validation;

namespace TimeBlocker.Web.Services;

public interface ITimeBlockService
{
    Task<IReadOnlyList<TimeBlock>> GetByDateAsync(string userId, DateOnly date, CancellationToken cancellationToken = default);

    Task<ServiceResult<TimeBlock>> GetByIdAsync(string userId, int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<TimeBlock>> CreateAsync(string userId, ParsedTimeBlock input, CancellationToken cancellationToken = default);

    Task<ServiceResult<TimeBlock>> UpdateAsync(string userId, int id, ParsedTimeBlock input, CancellationToken cancellationToken = default);

    Task<ServiceResult<bool>> DeleteAsync(string userId, int id, CancellationToken cancellationToken = default);
}
