using Api.Security;
using Application.DTOs.Approvals;
using Application.DTOs.Billing;
using Application.DTOs.JobCards;
using Application.DTOs.TimeLogs;
using Application.DTOs.WorkStations;
using Application.Pagination;
using Application.Services.Interfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Api;

namespace Api.Controllers;

[ApiController]
[Route("api/v1/jobcards")]
[Authorize(Policy = "BranchUser")]
public sealed class JobCardsController : ControllerBase
{
    private readonly IJobCardService _jobs;
    private readonly IWorkStationService _stations;
    private readonly IApprovalService _approvals;
    private readonly ITimeLogService _timelogs;
    private readonly IBillingService _billing;

    public JobCardsController(IJobCardService jobs, IWorkStationService stations, IApprovalService approvals, ITimeLogService timelogs, IBillingService billing)
    {
        _jobs = jobs;
        _stations = stations;
        _approvals = approvals;
        _timelogs = timelogs;
        _billing = billing;
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> Create([FromBody] JobCardCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.CreateAsync(userId, branchId, req, ct));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PageResponse<JobCardResponse>>>> GetPaged([FromQuery] PageRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<PageResponse<JobCardResponse>>.Ok(await _jobs.GetPagedAsync(branchId, req, ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.GetByIdAsync(branchId, id, ct));
    }

    [HttpPost("{id:guid}/check-in")]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> CheckIn(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.CheckInAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/check-out")]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> CheckOut(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.CheckOutAsync(userId, branchId, id, ct));
    }

    [HttpPost("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> ChangeStatus(Guid id, [FromBody] JobCardStatusChangeRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.ChangeStatusAsync(userId, branchId, id, req.Status, ct));
    }

    [HttpPost("{id:guid}/diagnosis")]
    public async Task<ActionResult<ApiResponse<JobCardResponse>>> UpdateDiagnosis(Guid id, [FromBody] JobCardDiagnosisUpdateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardResponse>.Ok(await _jobs.UpdateDiagnosisAsync(userId, branchId, id, req.Diagnosis, ct));
    }

    // Workstation movement + history
    [HttpPost("{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<JobCardStationHistoryResponse>>> Move(Guid id, [FromBody] MoveJobCardRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardStationHistoryResponse>.Ok(await _stations.MoveJobCardAsync(userId, branchId, id, req, ct));
    }

    [HttpGet("{id:guid}/station-history")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobCardStationHistoryResponse>>>> StationHistory(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<JobCardStationHistoryResponse>>.Ok(await _stations.GetJobCardHistoryAsync(branchId, id, ct));
    }

    // Approvals
    [HttpPost("{id:guid}/approve-supervisor")]
    public async Task<ActionResult<ApiResponse<JobCardApprovalResponse>>> ApproveSupervisor(Guid id, [FromBody] ApproveRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardApprovalResponse>.Ok(await _approvals.ApproveAsync(userId, branchId, id, ApprovalRole.Supervisor, req.Notes, ct));
    }

    [HttpPost("{id:guid}/approve-cashier")]
    public async Task<ActionResult<ApiResponse<JobCardApprovalResponse>>> ApproveCashier(Guid id, [FromBody] ApproveRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<JobCardApprovalResponse>.Ok(await _approvals.ApproveAsync(userId, branchId, id, ApprovalRole.Cashier, req.Notes, ct));
    }

    [HttpGet("{id:guid}/approvals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<JobCardApprovalResponse>>>> Approvals(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<JobCardApprovalResponse>>.Ok(await _approvals.ListAsync(branchId, id, ct));
    }

    // Time logs
    [HttpPost("{id:guid}/timelogs/start")]
    public async Task<ActionResult<ApiResponse<TimeLogResponse>>> StartTimeLog(Guid id, [FromBody] StartTimeLogRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TimeLogResponse>.Ok(await _timelogs.StartAsync(userId, branchId, id, req.TechnicianUserId, ct));
    }

    [HttpPost("{id:guid}/timelogs/stop")]
    public async Task<ActionResult<ApiResponse<TimeLogResponse>>> StopTimeLog(Guid id, [FromBody] StopTimeLogRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<TimeLogResponse>.Ok(await _timelogs.StopAsync(userId, branchId, id, req.TimeLogId, ct));
    }

    [HttpGet("{id:guid}/timelogs")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TimeLogResponse>>>> ListTimeLogs(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<IReadOnlyList<TimeLogResponse>>.Ok(await _timelogs.ListAsync(branchId, id, ct));
    }

    // Billing
    [HttpPost("{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> CreateOrGetInvoice(Guid id, [FromBody] InvoiceCreateRequest req, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        var userId = User.GetUserId();
        return ApiResponse<InvoiceResponse>.Ok(await _billing.CreateOrGetInvoiceAsync(userId, branchId, id, req, ct));
    }

    [HttpGet("{id:guid}/invoice")]
    public async Task<ActionResult<ApiResponse<InvoiceResponse>>> GetInvoice(Guid id, CancellationToken ct)
    {
        var branchId = User.GetBranchIdOrThrow();
        return ApiResponse<InvoiceResponse>.Ok(await _billing.GetInvoiceAsync(branchId, id, ct));
    }
}
