namespace Domain.Entities;

public sealed class Customer : BaseEntity { public string FullName{get;set;}=string.Empty; public string? Phone{get;set;} public string? Email{get;set;} public string? NationalId{get;set;} }
