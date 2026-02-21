namespace Application.DTOs.Wages;

public sealed record UserWageUpsertRequest(
    decimal HourlyRate,
    string? Currency = "PKR",
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null
);
