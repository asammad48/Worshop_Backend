namespace Domain.Entities;
public sealed class Part: BaseEntity{ public string Sku{get;set;}=string.Empty; public string Name{get;set;}=string.Empty; public string? Brand{get;set;} public string? Unit{get;set;} public decimal ReorderLevel { get; set; } }
