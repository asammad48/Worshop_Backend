namespace Domain.Entities;
public sealed class EmployeeProfile: BaseEntity{ public Guid UserId{get;set;} public string FullName{get;set;}=string.Empty; public string? CNIC{get;set;} public decimal? BaseSalary{get;set;} public bool IsActive{get;set;}=true; }
