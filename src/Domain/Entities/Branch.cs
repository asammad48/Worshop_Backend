namespace Domain.Entities;

public sealed class Branch : BaseEntity { public string Name {get;set;}=string.Empty; public string? Address {get;set;} public bool IsActive {get;set;}=true; }
