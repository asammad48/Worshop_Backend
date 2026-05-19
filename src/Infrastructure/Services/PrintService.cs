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
    private readonly IPublicReportService _publicReportService;
    private readonly IConfiguration _config;

    public PrintService(AppDbContext db, IReceiptService receiptService, IPublicReportService publicReportService, IConfiguration config)
    {
        _db = db;
        _receiptService = receiptService;
        _publicReportService = publicReportService;
        _config = config;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> RenderPublicFullReportPdfAsync(Guid jobCardId, string? token, CancellationToken ct = default)
    {
        var data = await _publicReportService.GetPublicFullJobCardReportAsync(jobCardId, token, ct);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ── HEADER ─────────────────────────────────────────────────
                page.Header().Column(col =>
                {
                    // Dark title bar
                    col.Item()
                        .Background(Colors.Blue.Darken4)
                        .Padding(12)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("WORKSHOP JOB CARD REPORT")
                                    .FontSize(15).Bold().FontColor(Colors.White);
                                c.Item().Text($"Ref: {data.Header.JobCardNo}  |  Status: {data.Header.Status}")
                                    .FontSize(8).FontColor(Colors.Blue.Lighten3);
                            });
                            row.ConstantItem(160).Column(c =>
                            {
                                c.Item().AlignRight().Text($"Branch: {data.Header.BranchName}")
                                    .FontSize(8).FontColor(Colors.Blue.Lighten3);
                                c.Item().AlignRight().Text($"Generated: {DateTimeOffset.UtcNow:dd MMM yyyy}")
                                    .FontSize(8).FontColor(Colors.Blue.Lighten3);
                            });
                        });

                    // Info strip beneath title bar
                    col.Item()
                        .Background(Colors.Blue.Lighten5)
                        .BorderBottom(2).BorderColor(Colors.Blue.Darken2)
                        .PaddingHorizontal(12).PaddingVertical(7)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Plate:  ").SemiBold(); t.Span(data.Header.Plate); });
                                c.Item().Text(t => { t.Span("Customer:  ").SemiBold(); t.Span(data.Header.CustomerName); });
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Entry:  ").SemiBold(); t.Span(data.Header.EntryAt.ToString("dd MMM yyyy")); });
                                c.Item().Text(t => { t.Span("Days in Shop:  ").SemiBold(); t.Span(data.Header.DaysInShop.ToString()); });
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t => { t.Span("Parts Requested:  ").SemiBold(); t.Span(data.TotalPartRequests.ToString()); });
                                c.Item().Text(t => { t.Span("Parts Used:  ").SemiBold(); t.Span(data.TotalPartsUsed.ToString()); });
                            });
                        });
                });

                // ── CONTENT ────────────────────────────────────────────────
                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(14);

                    // ── DIAGNOSIS OVERVIEW ──────────────────────────────────
                    col.Item().Column(inner =>
                    {
                        inner.Item()
                            .BorderLeft(4).BorderColor(Colors.Blue.Darken3)
                            .PaddingLeft(8).PaddingVertical(2)
                            .Text("DIAGNOSIS OVERVIEW")
                            .FontSize(10).Bold().FontColor(Colors.Blue.Darken3);

                        inner.Item()
                            .Border(1).BorderColor(Colors.Grey.Lighten2)
                            .Background(Colors.Grey.Lighten5)
                            .Padding(10)
                            .Column(info =>
                            {
                                info.Spacing(4);
                                info.Item().Text(t =>
                                {
                                    t.Span("Initial Diagnosis:  ").SemiBold();
                                    t.Span(data.Diagnosis ?? "-");
                                });
                                info.Item().Text(t =>
                                {
                                    t.Span("Latest Summary:  ").SemiBold();
                                    t.Span(data.LatestDiagnosisSummary ?? "-");
                                });
                                info.Item().Row(r =>
                                {
                                    r.RelativeItem().Text(t => { t.Span("Requested ETA:  ").SemiBold(); t.Span(data.RequestedEta?.ToString("dd MMM yyyy HH:mm") ?? "-"); });
                                    r.RelativeItem().Text(t => { t.Span("Latest ETA:  ").SemiBold(); t.Span(data.LatestEstimatedEta?.ToString("dd MMM yyyy HH:mm") ?? "-"); });
                                    r.RelativeItem().Text(t => { t.Span("Current Garage:  ").SemiBold(); t.Span(data.CurrentGarage ?? "-"); });
                                });
                            });
                    });

                    // ── JOB TASK CHECKLIST ──────────────────────────────────
                    col.Item().Column(inner =>
                    {
                        inner.Item()
                            .BorderLeft(4).BorderColor(Colors.Blue.Darken3)
                            .PaddingLeft(8).PaddingVertical(2)
                            .Text("JOB TASK CHECKLIST")
                            .FontSize(10).Bold().FontColor(Colors.Blue.Darken3);

                        inner.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(22);
                                columns.RelativeColumn(5);
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(125);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(FrTaskHeader).AlignCenter().Text("#");
                                header.Cell().Element(FrTaskHeader).Text("Task");
                                header.Cell().Element(FrTaskHeader).AlignCenter().Text("Status");
                                header.Cell().Element(FrTaskHeader).Text("Completed At");
                            });

                            var ri = 0;
                            foreach (var task in data.Tasks)
                            {
                                var isDone = string.Equals(task.DisplayStatus, "Completed", StringComparison.OrdinalIgnoreCase);
                                var isInProgress = string.Equals(task.DisplayStatus, "InProgress", StringComparison.OrdinalIgnoreCase);
                                var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                    .Text(isDone ? "☑" : "☐")
                                    .FontColor(isDone ? Colors.Green.Darken2 : Colors.Grey.Medium);
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                    .Text(task.Title);
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                    .Text(task.DisplayStatus)
                                    .FontColor(isDone ? Colors.Green.Darken2 : isInProgress ? Colors.Blue.Darken1 : Colors.Grey.Darken1)
                                    .SemiBold();
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                    .Text(task.CompletedAt?.ToString("dd MMM yyyy HH:mm") ?? "-")
                                    .FontColor(Colors.Grey.Darken2);
                            }
                        });
                    });

                    // ── TIME LOG SUMMARY ────────────────────────────────────
                    if (data.TimeLogs.Any())
                    {
                        col.Item().Column(inner =>
                        {
                            inner.Item()
                                .BorderLeft(4).BorderColor(Colors.Teal.Darken2)
                                .PaddingLeft(8).PaddingVertical(2)
                                .Text("TIME LOG SUMMARY")
                                .FontSize(10).Bold().FontColor(Colors.Teal.Darken2);

                            inner.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(90);
                                    columns.ConstantColumn(90);
                                    columns.ConstantColumn(45);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(FrTimeHeader).Text("Technician");
                                    header.Cell().Element(FrTimeHeader).Text("Email");
                                    header.Cell().Element(FrTimeHeader).Text("Task");
                                    header.Cell().Element(FrTimeHeader).AlignCenter().Text("Start");
                                    header.Cell().Element(FrTimeHeader).AlignCenter().Text("End");
                                    header.Cell().Element(FrTimeHeader).AlignCenter().Text("Min");
                                });

                                var ri = 0;
                                foreach (var tl in data.TimeLogs)
                                {
                                    var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(tl.UserName).SemiBold();
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(tl.UserEmail).FontColor(Colors.Grey.Darken2);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(tl.TaskTitle ?? "-");
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(tl.StartedAt.ToString("dd/MM HH:mm")).FontColor(Colors.Grey.Darken2);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(tl.EndedAt?.ToString("dd/MM HH:mm") ?? "Active")
                                        .FontColor(tl.EndedAt.HasValue ? Colors.Grey.Darken2 : Colors.Green.Darken2)
                                        .SemiBold();
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(tl.DurationMinutes.ToString());
                                }
                            });
                        });
                    }

                    // ── DIAGNOSIS HISTORY ───────────────────────────────────
                    if (data.DiagnosisLogs.Any())
                    {
                        col.Item().Column(inner =>
                        {
                            inner.Item()
                                .BorderLeft(4).BorderColor(Colors.Orange.Darken3)
                                .PaddingLeft(8).PaddingVertical(2)
                                .Text("DIAGNOSIS HISTORY")
                                .FontSize(10).Bold().FontColor(Colors.Orange.Darken3);

                            inner.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(95);
                                    columns.ConstantColumn(65);
                                    columns.ConstantColumn(100);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(FrDiagHeader).Text("Diagnosis Note");
                                    header.Cell().Element(FrDiagHeader).Text("Diagnosed By");
                                    header.Cell().Element(FrDiagHeader).AlignCenter().Text("Est. ETA");
                                    header.Cell().Element(FrDiagHeader).AlignRight().Text("Est. Price");
                                    header.Cell().Element(FrDiagHeader).AlignCenter().Text("Date");
                                });

                                var ri = 0;
                                foreach (var dl in data.DiagnosisLogs)
                                {
                                    var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(dl.DiagnosisNote);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(dl.CreatedByName).SemiBold();
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(dl.EstimatedEta?.ToString("dd MMM yyyy") ?? "-").FontColor(Colors.Grey.Darken2);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignRight()
                                        .Text(dl.EstimatedPrice.HasValue ? dl.EstimatedPrice.Value.ToString("N2") : "-")
                                        .FontColor(Colors.Grey.Darken2);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(dl.CreatedAt.ToString("dd MMM yyyy")).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        });
                    }

                    // ── PARTS REQUESTS ──────────────────────────────────────
                    if (data.PartRequests.Any())
                    {
                        col.Item().Column(inner =>
                        {
                            inner.Item()
                                .BorderLeft(4).BorderColor(Colors.Green.Darken3)
                                .PaddingLeft(8).PaddingVertical(2)
                                .Text("PARTS REQUESTS")
                                .FontSize(10).Bold().FontColor(Colors.Green.Darken3);

                            inner.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(58);
                                    columns.RelativeColumn(3);
                                    columns.ConstantColumn(35);
                                    columns.RelativeColumn(2);
                                    columns.ConstantColumn(78);
                                    columns.ConstantColumn(95);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(FrPartsHeader).Text("SKU");
                                    header.Cell().Element(FrPartsHeader).Text("Part Name");
                                    header.Cell().Element(FrPartsHeader).AlignCenter().Text("Qty");
                                    header.Cell().Element(FrPartsHeader).Text("Requested By");
                                    header.Cell().Element(FrPartsHeader).AlignCenter().Text("Status");
                                    header.Cell().Element(FrPartsHeader).AlignCenter().Text("Requested At");
                                });

                                var ri = 0;
                                foreach (var pr in data.PartRequests)
                                {
                                    var isApproved = pr.Status is "Ordered" or "Arrived" or "IssuedToJob";
                                    var isCancelled = pr.Status == "Cancelled";
                                    var statusColor = isApproved ? Colors.Green.Darken2 : isCancelled ? Colors.Red.Darken2 : Colors.Orange.Darken2;
                                    var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(pr.PartSku).FontColor(Colors.Grey.Darken2);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(pr.PartName);
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(pr.QuantityRequested.ToString("N0"));
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4)
                                        .Text(pr.RequestedByName).SemiBold();
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(pr.Status).FontColor(statusColor).SemiBold();
                                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(4).AlignCenter()
                                        .Text(pr.RequestedAt.ToString("dd MMM yyyy")).FontColor(Colors.Grey.Darken2);
                                }
                            });
                        });
                    }
                });

                // ── FOOTER ─────────────────────────────────────────────────
                page.Footer()
                    .BorderTop(1).BorderColor(Colors.Grey.Lighten2)
                    .PaddingTop(4)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"Job Card: {data.Header.JobCardNo}  |  {data.Header.BranchName}  |  {data.Header.Plate}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        row.ConstantItem(80).AlignRight().Text(x =>
                        {
                            x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                            x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Medium);
                            x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    });
            });
        });

        return document.GeneratePdf();
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
                         col.Item().Image(barcode.GetByteArray()).FitArea();
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

    public async Task<byte[]> RenderPublicReceiptPdfAsync(Guid jobCardId, string? token, string language = "en", CancellationToken ct = default)
    {
        var data = await _receiptService.GetPublicReceiptAsync(jobCardId, token, ct);
        var isSpanish = string.Equals(language, "es", StringComparison.OrdinalIgnoreCase);
        string T(string en, string es) => isSpanish ? es : en;

        bool requireToken = _config.GetValue<bool>("PublicReceipt:RequireToken", false);
        string payload = jobCardId.ToString();
        if (requireToken && !string.IsNullOrEmpty(token))
            payload = $"{jobCardId}|{token}";

        var baseUrl = _config["App:BaseUrl"] ?? "http://dashboard.motoritaller.es";
        var publicUrl = $"{baseUrl}/r/jobcards/{jobCardId}" + (string.IsNullOrEmpty(token) ? "" : $"?t={token}");

        var invoiceRef = jobCardId.ToString().Substring(0, 8).ToUpper();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9));

                // ── HEADER ─────────────────────────────────────────────────
                page.Header().Column(hcol =>
                {
                    // Top brand bar
                    hcol.Item()
                        .Background(Colors.Grey.Darken4)
                        .Padding(12)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(data.BranchName)
                                    .FontSize(20).Bold().FontColor(Colors.White);
                                if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                                    c.Item().Text(data.BranchAddress)
                                        .FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });

                            row.ConstantItem(160).Column(c =>
                            {
                                c.Item().AlignRight()
                                    .Text(T("SERVICE INVOICE", "FACTURA DE SERVICIO"))
                                    .FontSize(13).Bold().FontColor(Colors.White);
                                c.Item().AlignRight()
                                    .Text($"Ref: {invoiceRef}")
                                    .FontSize(9).FontColor(Colors.Grey.Lighten3);
                                c.Item().AlignRight()
                                    .Text($"{T("Date", "Fecha")}: {DateTimeOffset.UtcNow:dd MMM yyyy}")
                                    .FontSize(8).FontColor(Colors.Grey.Lighten3);
                                c.Item().AlignRight()
                                    .Text($"{T("Status", "Estado")}: {data.Status}")
                                    .FontSize(8).FontColor(Colors.Amber.Lighten2).SemiBold();
                            });
                        });

                    // Accent line
                    hcol.Item().Height(4).Background(Colors.Amber.Darken1);
                });

                // ── CONTENT ────────────────────────────────────────────────
                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(12);

                    // ── PARTIES ROW: Seller | Customer ─────────────────────
                    col.Item().Row(row =>
                    {
                        // Seller / Workshop
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                        {
                            c.Item()
                                .Background(Colors.Grey.Darken3)
                                .PaddingHorizontal(8).PaddingVertical(5)
                                .Text(T("FROM — WORKSHOP", "DE — TALLER"))
                                .FontSize(8).Bold().FontColor(Colors.White);
                            c.Item().PaddingHorizontal(8).PaddingVertical(8).Column(info =>
                            {
                                info.Spacing(3);
                                info.Item().Text(data.BranchName).FontSize(10).Bold();
                                if (!string.IsNullOrWhiteSpace(data.BranchAddress))
                                    info.Item().Text(data.BranchAddress).FontColor(Colors.Grey.Darken2);
                            });
                        });

                        row.ConstantItem(12);

                        // Customer / Vehicle Owner
                        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                        {
                            c.Item()
                                .Background(Colors.Grey.Darken3)
                                .PaddingHorizontal(8).PaddingVertical(5)
                                .Text(T("TO — VEHICLE OWNER", "PARA — PROPIETARIO"))
                                .FontSize(8).Bold().FontColor(Colors.White);
                            c.Item().PaddingHorizontal(8).PaddingVertical(8).Column(info =>
                            {
                                info.Spacing(3);
                                info.Item().Text(data.CustomerName).FontSize(10).Bold();
                                if (!string.IsNullOrWhiteSpace(data.CustomerPhone))
                                    info.Item().Text(t => { t.Span($"{T("Phone", "Teléfono")}: ").SemiBold(); t.Span(data.CustomerPhone!); });
                                if (!string.IsNullOrWhiteSpace(data.CustomerEmail))
                                    info.Item().Text(t => { t.Span("Email: ").SemiBold(); t.Span(data.CustomerEmail!); });
                                if (!string.IsNullOrWhiteSpace(data.CustomerNationalId))
                                    info.Item().Text(t => { t.Span($"{T("ID", "DNI/NIF")}: ").SemiBold(); t.Span(data.CustomerNationalId!); });
                                info.Item().Text(t => { t.Span($"{T("Type", "Tipo")}: ").SemiBold(); t.Span(data.CustomerType); });
                            });
                        });
                    });

                    // ── VEHICLE & JOB INFO ──────────────────────────────────
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Column(c =>
                    {
                        c.Item()
                            .Background(Colors.Amber.Darken1)
                            .PaddingHorizontal(8).PaddingVertical(5)
                            .Text(T("VEHICLE & JOB DETAILS", "VEHÍCULO Y DETALLES DEL TRABAJO"))
                            .FontSize(8).Bold().FontColor(Colors.White);

                        c.Item().PaddingHorizontal(8).PaddingVertical(8).Row(vrow =>
                        {
                            // Left: vehicle
                            vrow.RelativeItem().Column(vc =>
                            {
                                vc.Spacing(3);
                                vc.Item().Text(t => { t.Span($"{T("Plate", "Matrícula")}: ").SemiBold(); t.Span(data.Plate).FontSize(11).Bold(); });
                                if (!string.IsNullOrWhiteSpace(data.VehicleMake))
                                    vc.Item().Text(t => { t.Span($"{T("Make", "Marca")}: ").SemiBold(); t.Span(data.VehicleMake!); });
                                if (!string.IsNullOrWhiteSpace(data.VehicleModel))
                                    vc.Item().Text(t => { t.Span($"{T("Model", "Modelo")}: ").SemiBold(); t.Span(data.VehicleModel!); });
                                if (data.VehicleYear.HasValue)
                                    vc.Item().Text(t => { t.Span($"{T("Year", "Año")}: ").SemiBold(); t.Span(data.VehicleYear.Value.ToString()); });
                                if (data.Mileage.HasValue)
                                    vc.Item().Text(t => { t.Span($"{T("Mileage", "Kilometraje")}: ").SemiBold(); t.Span($"{data.Mileage.Value:N0} km"); });
                            });

                            // Middle: job dates
                            vrow.RelativeItem().Column(jc =>
                            {
                                jc.Spacing(3);
                                jc.Item().Text(t => { t.Span($"{T("Entry", "Entrada")}: ").SemiBold(); t.Span(data.EntryAt.ToString("dd MMM yyyy HH:mm")); });
                                if (data.ExitAt.HasValue)
                                    jc.Item().Text(t => { t.Span($"{T("Exit", "Salida")}: ").SemiBold(); t.Span(data.ExitAt.Value.ToString("dd MMM yyyy HH:mm")); });
                                if (data.RequestedEta.HasValue)
                                    jc.Item().Text(t => { t.Span($"{T("Req. ETA", "ETA Solicitada")}: ").SemiBold(); t.Span(data.RequestedEta.Value.ToString("dd MMM yyyy")); });
                                if (data.LatestEstimatedEta.HasValue)
                                    jc.Item().Text(t => { t.Span($"{T("Est. ETA", "ETA Estimada")}: ").SemiBold(); t.Span(data.LatestEstimatedEta.Value.ToString("dd MMM yyyy")); });
                            });

                            // Right: driver
                            vrow.RelativeItem().Column(dc =>
                            {
                                dc.Spacing(3);
                                if (!string.IsNullOrWhiteSpace(data.DriverName))
                                {
                                    dc.Item().Text($"{T("Driver", "Conductor")}").SemiBold().FontSize(8);
                                    dc.Item().Text(data.DriverName!).Bold();
                                    if (!string.IsNullOrWhiteSpace(data.DriverPhone))
                                        dc.Item().Text(t => { t.Span($"{T("Phone", "Teléfono")}: ").SemiBold(); t.Span(data.DriverPhone!); });
                                    if (!string.IsNullOrWhiteSpace(data.DriverLicenseNumber))
                                        dc.Item().Text(t => { t.Span($"{T("License", "Licencia")}: ").SemiBold(); t.Span(data.DriverLicenseNumber!); });
                                }
                            });
                        });

                        if (!string.IsNullOrWhiteSpace(data.InitialReport))
                        {
                            c.Item()
                                .BorderTop(1).BorderColor(Colors.Grey.Lighten3)
                                .Background(Colors.Grey.Lighten5)
                                .PaddingHorizontal(8).PaddingVertical(6)
                                .Text(t =>
                                {
                                    t.Span($"{T("Initial Report", "Informe inicial")}: ").SemiBold();
                                    t.Span(data.InitialReport!).FontColor(Colors.Grey.Darken2);
                                });
                        }
                    });

                    // ── LINE ITEMS ──────────────────────────────────────────
                    if (data.Invoice.HasInvoice && data.Invoice.Lines.Any())
                    {
                        col.Item().Column(inner =>
                        {
                            inner.Item()
                                .BorderLeft(4).BorderColor(Colors.Grey.Darken3)
                                .PaddingLeft(8).PaddingVertical(2)
                                .Text(T("SERVICE ITEMS", "LÍNEAS DE SERVICIO"))
                                .FontSize(10).Bold().FontColor(Colors.Grey.Darken3);

                            inner.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(5);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(75);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(FacturaHeader).Text(T("Description", "Descripción"));
                                    header.Cell().Element(FacturaHeader).AlignCenter().Text(T("Qty", "Cant."));
                                    header.Cell().Element(FacturaHeader).AlignRight().Text(T("Unit Price", "P. Unit."));
                                    header.Cell().Element(FacturaHeader).AlignRight().Text(T("Amount", "Importe"));
                                });

                                var ri = 0;
                                foreach (var line in data.Invoice.Lines)
                                {
                                    var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                    table.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6).Text(line.Name);
                                    table.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6).AlignCenter().Text(line.Qty.ToString("N2"));
                                    table.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text(line.UnitPrice.ToString("N2"));
                                    table.Cell().Background(bg).PaddingVertical(6).PaddingHorizontal(6).AlignRight().Text(line.Amount.ToString("N2")).SemiBold();
                                }
                            });
                        });
                    }

                    // ── PAYMENTS + TOTALS ───────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // Payment history
                        row.RelativeItem().Column(pcol =>
                        {
                            if (data.Payments.Any())
                            {
                                pcol.Item()
                                    .BorderLeft(4).BorderColor(Colors.Green.Darken2)
                                    .PaddingLeft(8).PaddingVertical(2)
                                    .Text(T("PAYMENTS RECEIVED", "PAGOS RECIBIDOS"))
                                    .FontSize(9).Bold().FontColor(Colors.Green.Darken2);

                                pcol.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(PaymentHeader).Text(T("Date", "Fecha"));
                                        header.Cell().Element(PaymentHeader).Text(T("Method", "Método"));
                                        header.Cell().Element(PaymentHeader).AlignRight().Text(T("Amount", "Importe"));
                                    });

                                    var ri = 0;
                                    foreach (var p in data.Payments)
                                    {
                                        var bg = ri++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                        table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(5).Text(p.PaidAt.ToString("dd MMM yyyy")).FontColor(Colors.Grey.Darken2);
                                        table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(5).Text(p.Method);
                                        table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(5).AlignRight().Text(p.Amount.ToString("N2")).FontColor(Colors.Green.Darken2).SemiBold();
                                    }
                                });
                            }
                        });

                        row.ConstantItem(16);

                        // Financial totals box
                        row.ConstantItem(210).Column(fcol =>
                        {
                            fcol.Item()
                                .Background(Colors.Grey.Darken3)
                                .PaddingHorizontal(10).PaddingVertical(6)
                                .Text(T("FINANCIAL SUMMARY", "RESUMEN FINANCIERO"))
                                .FontSize(8).Bold().FontColor(Colors.White);

                            fcol.Item()
                                .Border(1).BorderColor(Colors.Grey.Lighten2)
                                .PaddingHorizontal(10).PaddingVertical(8)
                                .Column(fs =>
                                {
                                    fs.Spacing(4);
                                    fs.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text(T("Subtotal", "Subtotal")).FontColor(Colors.Grey.Darken2);
                                        r.ConstantItem(70).AlignRight().Text(data.Invoice.Subtotal.ToString("N2"));
                                    });
                                    if (data.Invoice.Discount > 0)
                                    {
                                        fs.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text($"{T("Discount", "Descuento")} ({data.Invoice.Discount}%)").FontColor(Colors.Red.Medium);
                                            r.ConstantItem(70).AlignRight().Text($"-{data.Invoice.DiscountAmount:N2}").FontColor(Colors.Red.Medium);
                                        });
                                    }
                                    if (data.Invoice.Tax > 0)
                                    {
                                        fs.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text($"{T("Tax", "IVA")} ({data.Invoice.Tax}%)").FontColor(Colors.Grey.Darken2);
                                            r.ConstantItem(70).AlignRight().Text(data.Invoice.TaxAmount.ToString("N2")).FontColor(Colors.Grey.Darken2);
                                        });
                                    }
                                    fs.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(r =>
                                    {
                                        r.RelativeItem().Text(T("TOTAL", "TOTAL")).Bold().FontSize(11);
                                        r.ConstantItem(70).AlignRight().Text(data.Invoice.Total.ToString("N2")).Bold().FontSize(11);
                                    });
                                    fs.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text(T("Paid", "Pagado")).FontColor(Colors.Green.Darken2).SemiBold();
                                        r.ConstantItem(70).AlignRight().Text(data.Invoice.Paid.ToString("N2")).FontColor(Colors.Green.Darken2).SemiBold();
                                    });
                                    fs.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text(T("Balance Due", "Saldo Pendiente")).FontColor(data.Invoice.Due > 0 ? Colors.Red.Medium : Colors.Grey.Darken2).Bold();
                                        r.ConstantItem(70).AlignRight().Text(data.Invoice.Due.ToString("N2")).FontColor(data.Invoice.Due > 0 ? Colors.Red.Medium : Colors.Grey.Darken2).Bold();
                                    });
                                });
                        });
                    });

                    // ── DISCLAIMER + BARCODE ────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(dc =>
                        {
                            dc.Spacing(4);
                            dc.Item().Text(T("Disclaimer", "Descargo de responsabilidad")).FontSize(8).SemiBold().FontColor(Colors.Grey.Darken2);
                            dc.Item().Text(T(
                                "This is an electronically generated summary. Final invoice may vary based on actual work performed.",
                                "Este es un resumen generado electrónicamente. La factura final puede variar según el trabajo realizado."))
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                            dc.Item().Text(T(
                                "All work is subject to our standard terms and conditions. Please keep this document for your records.",
                                "Todo el trabajo está sujeto a nuestros términos y condiciones. Conserve este documento para sus registros."))
                                .FontSize(7).FontColor(Colors.Grey.Medium);
                        });

                        row.ConstantItem(160).AlignCenter().Column(bc =>
                        {
                            var barcode = new Barcode(payload, NetBarcode.Type.Code128, true);
                            bc.Item().AlignCenter().Width(140).Image(barcode.GetByteArray());
                            bc.Item().AlignCenter().Text(T("Scan for full details", "Escanear para detalles")).FontSize(7).FontColor(Colors.Grey.Medium);
                        });
                    });
                });

                // ── FOOTER ─────────────────────────────────────────────────
                page.Footer().Column(f =>
                {
                    f.Item().Height(4).Background(Colors.Amber.Darken1);
                    f.Item()
                        .Background(Colors.Grey.Darken4)
                        .PaddingHorizontal(14).PaddingVertical(6)
                        .Row(frow =>
                        {
                            frow.RelativeItem()
                                .Text(T("Thank you for choosing our workshop!", "¡Gracias por elegirnos!"))
                                .FontSize(9).SemiBold().FontColor(Colors.White);
                            frow.ConstantItem(100).AlignRight().Text(x =>
                            {
                                x.Span(T("Page ", "Página ")).FontSize(8).FontColor(Colors.Grey.Lighten3);
                                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Lighten3);
                                x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Lighten3);
                                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Lighten3);
                            });
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

    private static IContainer FrTaskHeader(IContainer c) =>
        c.Background(Colors.Blue.Darken4).PaddingVertical(6).PaddingHorizontal(5)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer FrTimeHeader(IContainer c) =>
        c.Background(Colors.Teal.Darken2).PaddingVertical(6).PaddingHorizontal(5)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer FrDiagHeader(IContainer c) =>
        c.Background(Colors.Orange.Darken3).PaddingVertical(6).PaddingHorizontal(5)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer FrPartsHeader(IContainer c) =>
        c.Background(Colors.Green.Darken3).PaddingVertical(6).PaddingHorizontal(5)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer FacturaHeader(IContainer c) =>
        c.Background(Colors.Grey.Darken3).PaddingVertical(6).PaddingHorizontal(6)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

    private static IContainer PaymentHeader(IContainer c) =>
        c.Background(Colors.Green.Darken3).PaddingVertical(5).PaddingHorizontal(5)
         .DefaultTextStyle(x => x.FontSize(8).Bold().FontColor(Colors.White));

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
                           select new JobCardPrintTaskDto(t.Id, t.Title, t.Status.ToString(), t.Status.ToString(), u.Email, t.StartedAt, t.EndedAt, t.Notes))
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
                                  join ep in _db.EmployeeProfiles on u.Id equals ep.UserId into eps
                                  from ep in eps.DefaultIfEmpty()
                                  join s in _db.Suppliers on r.SupplierId equals s.Id into suppliers
                                  from s in suppliers.DefaultIfEmpty()
                                  select new JobCardPrintPartRequestDto(p.Sku, p.Name, r.Qty, r.Status.ToString(), u != null ? u.Email : "N/A", ep != null ? ep.FullName : (u != null ? u.Email : "N/A"), r.RequestedAt, s.Name, null))
                                  .ToListAsync(ct);

        var roadblockers = await (from rb in _db.Roadblockers.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                  join u in _db.Users on rb.CreatedByUserId equals u.Id
                                  join ru in _db.Users on rb.ResolvedByUserId equals ru.Id into rusers
                                  from ru in rusers.DefaultIfEmpty()
                                  select new JobCardPrintRoadblockerDto(rb.Type.ToString(), rb.IsResolved ? "Resolved" : "Active", u.Email, rb.CreatedAt, ru.Email, rb.ResolvedAt, rb.Description))
                                  .ToListAsync(ct);

        var timeLogs = await (from tl in _db.JobCardTimeLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                              join u in _db.Users on tl.TechnicianUserId equals u.Id
                              join ep in _db.EmployeeProfiles on u.Id equals ep.UserId into eps
                              from ep in eps.DefaultIfEmpty()
                              join t in _db.JobTasks on tl.JobTaskId equals t.Id into tasks_
                              from t in tasks_.DefaultIfEmpty()
                              select new JobCardPrintTimeLogDto(u.Email, ep != null ? ep.FullName : u.Email, t.Title, tl.StartAt, tl.EndAt, tl.TotalMinutes))
                              .ToListAsync(ct);

        var communications = await (from c in _db.CommunicationLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                    join u in _db.Users on c.CreatedByUserId equals u.Id
                                    select new JobCardPrintCommunicationDto(c.Type.ToString(), c.Direction.ToString(), c.Summary, c.Details, c.OccurredAt, u.Email))
                                    .ToListAsync(ct);

        var diagnosisLogs = await (from dl in _db.JobCardDiagnosisLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                   join u in _db.Users on dl.CreatedByUserId equals u.Id into users
                                   from u in users.DefaultIfEmpty()
                                   join ep in _db.EmployeeProfiles on u.Id equals ep.UserId into eps
                                   from ep in eps.DefaultIfEmpty()
                                   orderby dl.CreatedAt descending
                                   select new JobCardPrintDiagnosisLogDto(dl.DiagnosisNote, dl.EstimatedEta, dl.EstimatedPrice, u != null ? u.Email : "N/A", ep != null ? ep.FullName : (u != null ? u.Email : "N/A"), dl.CreatedAt))
                                   .ToListAsync(ct);

        var taskWorkerTimeRows = await (from tl in _db.JobCardTimeLogs.Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
                                        join u in _db.Users on tl.TechnicianUserId equals u.Id
                                        join t in _db.JobTasks on tl.JobTaskId equals t.Id into tasks_
                                        from t in tasks_.DefaultIfEmpty()
                                        group tl by new { TaskTitle = t != null ? t.Title : "Unknown Task", WorkerEmail = u.Email } into g
                                        select new
                                        {
                                            g.Key.TaskTitle,
                                            g.Key.WorkerEmail,
                                            TotalMinutes = g.Sum(x => x.TotalMinutes)
                                        })
                                        .OrderBy(x => x.TaskTitle)
                                        .ThenBy(x => x.WorkerEmail)
                                        .ToListAsync(ct);

        var taskWorkerTimes = taskWorkerTimeRows
            .Select(x => new JobCardTaskWorkerTimeDto(
                x.TaskTitle,
                x.WorkerEmail,
                x.TotalMinutes,
                Math.Round(x.TotalMinutes / 60m, 2)))
            .ToList();

        var currentGarage = await _db.JobCardWorkStationHistories
            .Where(x => x.JobCardId == jobCardId && !x.IsDeleted)
            .OrderByDescending(x => x.MovedAt)
            .Select(x => x.WorkStation != null ? x.WorkStation.Name : null)
            .FirstOrDefaultAsync(ct);

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

        return new JobCardPrintResponse(header, job.Diagnosis, job.LatestDiagnosisSummary, job.RequestedEta, job.LatestEstimatedEta, currentGarage, partRequests.Count, partsUsed.Count, tasks, taskWorkerTimes, partsUsed, partRequests, roadblockers, timeLogs, communications, financial, diagnosisLogs);
    }
}
