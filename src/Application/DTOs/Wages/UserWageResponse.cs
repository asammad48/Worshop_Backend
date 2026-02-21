namespace Application.DTOs.Wages;

public sealed record UserWageResponse(
    Guid UserId,
    decimal HourlyRate,
    string Currency,
    DateTime? EffectiveFrom,
    DateTime? EffectiveTo,
    DateTimeOffset UpdatedAt
);
