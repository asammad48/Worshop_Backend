using Application.DTOs.Wages;

namespace Application.Services.Interfaces;

public interface IUserWageService
{
    Task<UserWageResponse> GetLatestWageAsync(Guid userId, CancellationToken ct = default);
    Task<UserWageResponse> UpsertWageAsync(Guid userId, UserWageUpsertRequest req, CancellationToken ct = default);
}
