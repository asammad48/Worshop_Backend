using Domain.Enums;
namespace Domain.Entities;
public sealed class Expense: BaseEntity{ public Guid BranchId{get;set;} public ExpenseCategory Category{get;set;} public decimal Amount{get;set;} public string? Description{get;set;} public DateTimeOffset ExpenseAt{get;set;} public Guid CreatedByUserId{get;set;} }
