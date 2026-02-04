using Domain.Enums;

namespace Application.DTOs.JobCards;

public sealed record JobCardStatusChangeRequest(JobCardStatus Status, string? Note = null);
