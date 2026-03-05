using Application.DTOs.Printing;
using Application.Services.Interfaces;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Shared.Errors;
using NetBarcode;

namespace Infrastructure.Services;

public sealed class PrintService : IPrintService
{
    private readonly AppDbContext _db;
    private readonly IReceiptService _receiptService;
    private readonly IConfiguration _config;

    public PrintService(AppDbContext db, IReceiptService receiptService, IConfiguration config)
    {
        _db = db;
        _receiptService = receiptService;
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> RenderJobCardPdfAsync(Guid jobCardId, Guid branchId, CancellationToken ct = default)
    {
        var data = await GetJobCardPrintDataAsync(jobCardId, branchId, ct);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Job Card: {data.Header.JobCardNo}").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        col.Item().Text($"{data.Header.BranchName}");
                    });

                    row.ConstantItem(100).Column(col => {
                         var barcode = new Barcode(data.Header.JobCardId.ToString(), NetBarcode.Type.Code128, true);
                         col.Item().Image(barcode.GetByteArray(), ImageScaling.FitArea);
                    });
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Header Info
                    col.Item().BorderBottom(1).PaddingBottom(5).Row(row => {
                        row.RelativeItem().Column(c => {
                            c.Item().Text(t => { t.Span("Plate: ").Bold(); t.Span(data.Header.Plate); });
                            c.Item().Text(t => { t.Span("Customer: ").Bold(); t.Span(data.Header.CustomerName); });
                            c.Item().Text(t => { t.Span("Phone: ").Bold(); t.Span(data.Header.CustomerPhone ?? "N/A"); });
                        });
                        row.RelativeItem().Column(c => {
                            c.Item().Text(t => { t.Span("Status: ").Bold(); t.Span(data.Header.Status); });
                            c.Item().Text(t => { t.Span("Entry: ").Bold(); t.Span(data.Header.EntryAt.ToString("g")); });
                            c.Item().Text(t => { t.Span("Days in shop: ").Bold(); t.Span(data.Header.DaysInShop.ToString()); });
                        });
                    });

                    // Tasks
                    if (data.Tasks.Any())
                    {
                        col.Item().PaddingTop(10).Text("Tasks").FontSize(14).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                                columns.ConstantColumn(100);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Title");
                                header.Cell().Element(CellStyle).Text("Status");
                                header.Cell().Element(CellStyle).Text("Assigned To");
                                header.Cell().Element(CellStyle).Text("Completed");
                            });

                            foreach (var task in data.Tasks)
                            {
                                table.Cell().Element(CellStyle).Text(task.Title);
                                table.Cell().Element(CellStyle).Text(task.Status);
                                table.Cell().Element(CellStyle).Text(task.AssignedToEmail ?? "-");
                                table.Cell().Element(CellStyle).Text(task.CompletedAt?.ToString("g") ?? "-");
                            }
                        });
                    }

                    // Parts Used
                    if (data.PartsUsed.Any())
                    {
                        col.Item().PaddingTop(10).Text("Parts Issued").FontSize(14).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Part");
                                header.Cell().Element(CellStyle).Text("Qty");
                                header.Cell().Element(CellStyle).Text("Price");
                                header.Cell().Element(CellStyle).Text("Total");
                            });

                            foreach (var part in data.PartsUsed)
                            {
                                table.Cell().Element(CellStyle).Text($"{part.PartName} ({part.PartSku})");
                                table.Cell().Element(CellStyle).Text(part.Quantity.ToString("N2"));
                                table.Cell().Element(CellStyle).Text(part.UnitPrice.ToString("N2"));
                                table.Cell().Element(CellStyle).Text(part.LineTotal.ToString("N2"));
                            }
                        });
                    }

                    // Part Requests
                    if (data.PartRequests.Any())
                    {
                        col.Item().PaddingTop(10).Text("Part Requests").FontSize(14).SemiBold();
                        col.Item().Table(table =>
                        {
                             table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.ConstantColumn(50);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Part");
                                header.Cell().Element(CellStyle).Text("Qty");
                                header.Cell().Element(CellStyle).Text("Status");
                                header.Cell().Element(CellStyle).Text("Note");
                            });

                            foreach (var req in data.PartRequests)
                            {
                                table.Cell().Element(CellStyle).Text(req.PartName);
                                table.Cell().Element(CellStyle).Text(req.QuantityRequested.ToString("N2"));
                                table.Cell().Element(CellStyle).Text(req.Status);
                                table.Cell().Element(CellStyle).Text(req.Note ?? "-");
                            }
                        });
                    }

                    // Roadblockers
                    if (data.Roadblockers.Any())
                    {
                        col.Item().PaddingTop(10).Text("Roadblockers").FontSize(14).SemiBold();
                        foreach(var rb in data.Roadblockers)
                        {
                            col.Item().PaddingLeft(5).Text($"- [{rb.Status}] {rb.Title}: {rb.Note} (Created by {rb.CreatedByEmail} at {rb.CreatedAt:g})");
                        }
                    }

                    // Financial
                    if (data.Financial.HasInvoice)
                    {
                        col.Item().PaddingTop(15).AlignRight().Column(fcol => {
                            fcol.Item().Text("Financial Summary").FontSize(14).SemiBold();
                            fcol.Item().Text($"Subtotal: {data.Financial.Subtotal:N2}");
                            fcol.Item().Text($"Discount: {data.Financial.Discount:N2}");
                            fcol.Item().Text($"Tax: {data.Financial.Tax:N2}");
                            fcol.Item().Text($"Total: {data.Financial.Total:N2}").FontSize(12).Bold();
                            fcol.Item().Text($"Paid: {data.Financial.Paid:N2}").FontColor(Colors.Green.Medium);
                            fcol.Item().Text($"Due: {data.Financial.Due:N2}").FontColor(Colors.Red.Medium).Bold();
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> RenderInvoicePdfAsync(Guid invoiceId, Guid branchId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices
            .Include(x => x.JobCard).ThenInclude(x => x!.Customer)
            .Include(x => x.JobCard).ThenInclude(x => x!.Vehicle)
            .Include(x => x.JobCard).ThenInclude(x => x!.Branch)
            .FirstOrDefaultAsync(x => x.Id == invoiceId && !x.IsDeleted, ct);

        if (invoice is null) throw new NotFoundException("Invoice not found");
        if (invoice.JobCard?.BranchId != branchId) throw new UnauthorizedException("Invoice does not belong to your branch");

        var lines = await _db.JobLineItems
            .Where(x => x.JobCardId == invoice.JobCardId && !x.IsDeleted)
            .ToListAsync(ct);

        var payments = await _db.Payments
            .Where(x => x.InvoiceId == invoiceId && !x.IsDeleted)
            .ToListAsync(ct);

        var paid = payments.Sum(x => x.Amount);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.Header().Row(row => {
                    row.RelativeItem().Column(col => {
                        col.Item().Text("INVOICE").FontSize(24).SemiBold();
                        col.Item().Text($"{invoice.JobCard.Branch?.Name}");
                    });
                    row.RelativeItem().AlignRight().Column(col => {
                        col.Item().Text($"Date: {invoice.CreatedAt:d}");
                        col.Item().Text($"Vehicle: {invoice.JobCard.Vehicle?.Plate}");
                        col.Item().Text($"Customer: {invoice.JobCard.Customer?.FullName}");
                    });
                });

                page.Content().PaddingVertical(20).Column(col => {
                    col.Item().Table(table => {
                        table.ColumnsDefinition(columns => {
                            columns.RelativeColumn();
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(80);
                        });

                        table.Header(header => {
                            header.Cell().Element(CellStyle).Text("Description");
                            header.Cell().Element(CellStyle).Text("Qty");
                            header.Cell().Element(CellStyle).Text("Unit Price");
                            header.Cell().Element(CellStyle).Text("Amount");
                        });

                        foreach(var line in lines) {
                            table.Cell().Element(CellStyle).Text(line.Title);
                            table.Cell().Element(CellStyle).Text(line.Qty.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(line.UnitPrice.ToString("N2"));
                            table.Cell().Element(CellStyle).Text(line.Total.ToString("N2"));
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Column(f => {
                        f.Item().Text($"Subtotal: {invoice.Subtotal:N2}");
                        f.Item().Text($"Discount: {invoice.Discount:N2}");
                        f.Item().Text($"Tax: {invoice.Tax:N2}");
                        f.Item().Text($"Total: {invoice.Total:N2}").Bold();
                        f.Item().Text($"Paid: {paid:N2}");
                        f.Item().Text($"Due: {invoice.Total - paid:N2}").Bold();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> RenderPublicReceiptPdfAsync(Guid jobCardId, string? token, CancellationToken ct = default)
    {
        var data = await _receiptService.GetPublicReceiptAsync(jobCardId, token, ct);

        bool requireToken = _config.GetValue<bool>("PublicReceipt:RequireToken", false);
        string payload = jobCardId.ToString();
        if (requireToken && !string.IsNullOrEmpty(token))
        {
            payload = $"{jobCardId}|{token}";
        }

        var baseUrl = _config["App:BaseUrl"] ?? "https://workshop.example.com";
        var publicUrl = $"{baseUrl}/public/receipt/jobcards/{jobCardId}" + (string.IsNullOrEmpty(token) ? "" : $"?t={token}");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(1, Unit.Centimetre);
                page.Content().Column(col => {
                    col.Item().AlignCenter().Text(data.BranchName).FontSize(18).SemiBold();
                    col.Item().AlignCenter().Text("Visit Summary").FontSize(14);

                    col.Item().PaddingTop(10).Row(row => {
                        row.RelativeItem().Column(c => {
                            c.Item().Text($"Plate: {data.Plate}");
                            c.Item().Text($"Customer: {data.CustomerName}");
                        });
                        row.RelativeItem().AlignRight().Column(c => {
                            c.Item().Text($"Status: {data.Status}");
                            c.Item().Text($"Date: {data.EntryAt:d}");
                        });
                    });

                    col.Item().PaddingTop(10).Table(table => {
                        table.ColumnsDefinition(columns => {
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                        });
                        table.Cell().Element(CellStyle).Text("Total Amount");
                        table.Cell().Element(CellStyle).AlignRight().Text(data.Invoice.Total.ToString("N2"));
                        table.Cell().Element(CellStyle).Text("Total Paid");
                        table.Cell().Element(CellStyle).AlignRight().Text(data.Invoice.Paid.ToString("N2"));
                        table.Cell().Element(CellStyle).Text("Balance Due").Bold();
                        table.Cell().Element(CellStyle).AlignRight().Text(data.Invoice.Due.ToString("N2")).Bold();
                    });

                    col.Item().PaddingTop(20).AlignCenter().Column(cc => {
                        var barcode = new Barcode(payload, NetBarcode.Type.Code128, true);
                        cc.Item().Width(150).Image(barcode.GetByteArray());
                        cc.Item().Text(publicUrl).FontSize(8).FontColor(Colors.Blue.Medium);
                        cc.Item().Text("Scan to view full details online").FontSize(8);
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(5);
    }

    private async Task<JobCardPrintResponse> GetJobCardPrintDataAsync(Guid jobCardId, Guid branchId, CancellationToken ct)
    {
        var job = await _db.JobCards
            .Include(x => x.Branch)
            .Include(x => x.Customer)
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == jobCardId && x.BranchId == branchId && !x.IsDeleted, ct);

        if (job is null) throw new NotFoundException("Job card not found");

        var header = new JobCardPrintHeaderDto(
            job.Id,
            job.Id.ToString().Substring(0, 8).ToUpper(),
            job.Vehicle?.Plate ?? "N/A",
            job.Customer?.FullName ?? "N/A",
            job.Customer?.Phone,
            job.Branch?.Name ?? "N/A",
            job.EntryAt ?? job.CreatedAt,
            job.ExitAt,
            (int)((job.ExitAt ?? DateTimeOffset.UtcNow) - (job.EntryAt ?? job.CreatedAt)).TotalDays,
            job.Status.ToString(),
            job.InitialReport
        );

        var tasks = await (from t in _db.JobTasks.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                           join u in _db.Users on t.StartedByUserId equals u.Id into users
                           from u in users.DefaultIfEmpty()
                           select new JobCardPrintTaskDto(t.Id, t.Title, t.Status.ToString(), u.Email, t.StartedAt, t.EndedAt, t.Notes))
                           .ToListAsync(ct);

        var partsUsed = await (from u in _db.JobCardPartUsages.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                               join p in _db.Parts on u.PartId equals p.Id
                               join l in _db.Locations on u.LocationId equals l.Id
                               select new JobCardPrintPartDto(p.Sku, p.Name, u.QuantityUsed, u.UnitPrice ?? 0, u.QuantityUsed * (u.UnitPrice ?? 0), l.Name, u.UsedAt))
                               .ToListAsync(ct);

        var partRequests = await (from r in _db.JobPartRequests.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                  join p in _db.Parts on r.PartId equals p.Id
                                  join u in _db.Users on r.CreatedBy equals u.Id into users
                                  from u in users.DefaultIfEmpty()
                                  join s in _db.Suppliers on r.SupplierId equals s.Id into suppliers
                                  from s in suppliers.DefaultIfEmpty()
                                  select new JobCardPrintPartRequestDto(p.Sku, p.Name, r.Qty, r.Status.ToString(), u.Email, r.RequestedAt, s.Name, null))
                                  .ToListAsync(ct);

        var roadblockers = await (from rb in _db.Roadblockers.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                  join u in _db.Users on rb.CreatedByUserId equals u.Id
                                  join ru in _db.Users on rb.ResolvedByUserId equals ru.Id into rusers
                                  from ru in rusers.DefaultIfEmpty()
                                  select new JobCardPrintRoadblockerDto(rb.Type.ToString(), rb.IsResolved ? "Resolved" : "Active", u.Email, rb.CreatedAt, ru.Email, rb.ResolvedAt, rb.Description))
                                  .ToListAsync(ct);

        var timeLogs = await (from tl in _db.JobCardTimeLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                              join u in _db.Users on tl.TechnicianUserId equals u.Id
                              join t in _db.JobTasks on tl.JobTaskId equals t.Id into tasks_
                              from t in tasks_.DefaultIfEmpty()
                              select new JobCardPrintTimeLogDto(u.Email, t.Title, tl.StartAt, tl.EndAt, tl.TotalMinutes))
                              .ToListAsync(ct);

        var communications = await (from c in _db.CommunicationLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                    join u in _db.Users on c.CreatedByUserId equals u.Id
                                    select new JobCardPrintCommunicationDto(c.Type.ToString(), c.Direction.ToString(), c.Summary, c.Details, c.OccurredAt, u.Email))
                                    .ToListAsync(ct);

        var invoice = await _db.Invoices.FirstOrDefaultAsync(x => x.JobCardId == jobCardId && !x.IsDeleted, ct);
        decimal paid = invoice == null ? 0 : await _db.Payments.Where(x => x.InvoiceId == invoice.Id && !x.IsDeleted).SumAsync(x => x.Amount, ct);

        var financial = new JobCardPrintFinancialDto(
            invoice != null,
            invoice?.Id.ToString().Substring(0, 8),
            invoice?.Subtotal ?? 0,
            invoice?.Discount ?? 0,
            invoice?.Tax ?? 0,
            invoice?.Total ?? 0,
            paid,
            (invoice?.Total ?? 0) - paid
        );

        return new JobCardPrintResponse(header, tasks, partsUsed, partRequests, roadblockers, timeLogs, communications, financial);
    }
}
