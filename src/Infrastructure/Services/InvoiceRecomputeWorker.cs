using Application.Services.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class InvoiceRecomputeWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InvoiceRecomputeWorker> _logger;

    public InvoiceRecomputeWorker(IServiceProvider serviceProvider, ILogger<InvoiceRecomputeWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Recompute Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing invoice recompute jobs.");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessJobsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var computationService = scope.ServiceProvider.GetRequiredService<IInvoiceComputationService>();

        var jobs = await db.InvoiceRecomputeJobs
            .Where(x => x.Status == "Pending" && x.Attempts < 5 && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var job in jobs)
        {
            job.Status = "Processing";
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            try
            {
                await computationService.RecomputeAsync(job.JobCardId, job.Reason, ct);
                job.Status = "Succeeded";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recompute invoice for JobCard {JobCardId}", job.JobCardId);
                job.Attempts++;
                job.LastError = ex.Message;

                if (job.Attempts < 5)
                {
                    job.Status = "Pending";
                }
                else
                {
                    job.Status = "Failed";
                }
            }

            job.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
