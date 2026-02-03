using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<JobCard> JobCards => Set<JobCard>();
    public DbSet<WorkStation> WorkStations => Set<WorkStation>();
    public DbSet<JobCardWorkStationHistory> JobCardWorkStationHistories => Set<JobCardWorkStationHistory>();
    public DbSet<JobCardApproval> JobCardApprovals => Set<JobCardApproval>();
    public DbSet<JobCardTimeLog> JobCardTimeLogs => Set<JobCardTimeLog>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<PartStock> PartStocks => Set<PartStock>();
    public DbSet<StockLedger> StockLedgers => Set<StockLedger>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<JobCardPartUsage> JobCardPartUsages => Set<JobCardPartUsage>();
    public DbSet<JobLineItem> JobLineItems => Set<JobLineItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<EmployeeProfile> EmployeeProfiles => Set<EmployeeProfile>();
    public DbSet<WagePayment> WagePayments => Set<WagePayment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Roadblocker> Roadblockers => Set<Roadblocker>();
    public DbSet<JobTask> JobTasks => Set<JobTask>();
    public DbSet<JobPartRequest> JobPartRequests => Set<JobPartRequest>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        static void Base<T>(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> e) where T : BaseEntity
        {
            e.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        }

        modelBuilder.Entity<Branch>(b =>
        {
            b.ToTable("branches");
            b.HasKey(x => x.Id);
            Base(b);
            b.Property(x => x.Name).HasColumnName("name").IsRequired();
            b.Property(x => x.Address).HasColumnName("address");
            b.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            b.HasIndex(x => x.Name).HasDatabaseName("ix_branches_name");
        });

        modelBuilder.Entity<User>(u =>
        {
            u.ToTable("users");
            u.HasKey(x => x.Id);
            Base(u);
            u.Property(x => x.Email).HasColumnName("email").IsRequired();
            u.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
            u.Property(x => x.Role).HasColumnName("role").HasConversion<short>().IsRequired();
            u.Property(x => x.BranchId).HasColumnName("branch_id");
            u.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            u.HasIndex(x => x.Email).IsUnique().HasDatabaseName("uq_users_email");
            u.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(c =>
        {
            c.ToTable("customers");
            c.HasKey(x => x.Id);
            Base(c);
            c.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
            c.Property(x => x.Phone).HasColumnName("phone");
            c.Property(x => x.Email).HasColumnName("email");
            c.Property(x => x.NationalId).HasColumnName("national_id");
            c.HasIndex(x => x.Phone).HasDatabaseName("ix_customers_phone");
        });

        modelBuilder.Entity<Vehicle>(v =>
        {
            v.ToTable("vehicles");
            v.HasKey(x => x.Id);
            Base(v);
            v.Property(x => x.Plate).HasColumnName("plate").IsRequired();
            v.Property(x => x.Make).HasColumnName("make");
            v.Property(x => x.Model).HasColumnName("model");
            v.Property(x => x.Year).HasColumnName("year");
            v.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            v.HasIndex(x => x.Plate).IsUnique().HasDatabaseName("uq_vehicles_plate");
            v.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobCard>(j =>
        {
            j.ToTable("job_cards");
            j.HasKey(x => x.Id);
            Base(j);
            j.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            j.Property(x => x.CustomerId).HasColumnName("customer_id").IsRequired();
            j.Property(x => x.VehicleId).HasColumnName("vehicle_id").IsRequired();
            j.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            j.Property(x => x.EntryAt).HasColumnName("entry_at");
            j.Property(x => x.ExitAt).HasColumnName("exit_at");
            j.Property(x => x.Mileage).HasColumnName("mileage");
            j.Property(x => x.InitialReport).HasColumnName("initial_report");
            j.Property(x => x.Diagnosis).HasColumnName("diagnosis");
            j.HasIndex(x => new { x.BranchId, x.Status }).HasDatabaseName("ix_job_cards_branch_status");
            j.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            j.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            j.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkStation>(w =>
        {
            w.ToTable("work_stations");
            w.HasKey(x => x.Id);
            Base(w);
            w.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            w.Property(x => x.Code).HasColumnName("code").IsRequired();
            w.Property(x => x.Name).HasColumnName("name").IsRequired();
            w.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            w.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasDatabaseName("uq_workstations_branch_code");
            w.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobCardWorkStationHistory>(h =>
        {
            h.ToTable("jobcard_station_history");
            h.HasKey(x => x.Id);
            Base(h);
            h.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            h.Property(x => x.WorkStationId).HasColumnName("workstation_id").IsRequired();
            h.Property(x => x.MovedAt).HasColumnName("moved_at").HasDefaultValueSql("now()");
            h.Property(x => x.MovedByUserId).HasColumnName("moved_by_user_id").IsRequired();
            h.Property(x => x.Notes).HasColumnName("notes");
            h.HasIndex(x => x.JobCardId).HasDatabaseName("ix_station_history_jobcard");
            h.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
            h.HasOne(x => x.WorkStation).WithMany().HasForeignKey(x => x.WorkStationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobCardApproval>(a =>
        {
            a.ToTable("jobcard_approvals");
            a.HasKey(x => x.Id);
            Base(a);
            a.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            a.Property(x => x.Role).HasColumnName("role").HasConversion<short>().IsRequired();
            a.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id").IsRequired();
            a.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasDefaultValueSql("now()");
            a.Property(x => x.Notes).HasColumnName("notes");
            a.HasIndex(x => new { x.JobCardId, x.Role }).IsUnique().HasDatabaseName("uq_jobcard_approval_role");
            a.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobCardTimeLog>(t =>
        {
            t.ToTable("jobcard_time_logs");
            t.HasKey(x => x.Id);
            Base(t);
            t.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            t.Property(x => x.TechnicianUserId).HasColumnName("technician_user_id").IsRequired();
            t.Property(x => x.StartAt).HasColumnName("start_at").IsRequired();
            t.Property(x => x.EndAt).HasColumnName("end_at");
            t.Property(x => x.TotalMinutes).HasColumnName("total_minutes").HasDefaultValue(0);
            t.HasIndex(x => x.JobCardId).HasDatabaseName("ix_timelogs_jobcard");
            t.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Invoice>(i =>
        {
            i.ToTable("invoices");
            i.HasKey(x => x.Id);
            Base(i);
            i.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            i.Property(x => x.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(12,2)");
            i.Property(x => x.Discount).HasColumnName("discount").HasColumnType("numeric(12,2)");
            i.Property(x => x.Tax).HasColumnName("tax").HasColumnType("numeric(12,2)");
            i.Property(x => x.Total).HasColumnName("total").HasColumnType("numeric(12,2)");
            i.Property(x => x.PaymentStatus).HasColumnName("payment_status").HasConversion<short>().IsRequired();
            i.HasIndex(x => x.JobCardId).IsUnique().HasDatabaseName("uq_invoice_jobcard");
            i.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(p =>
        {
            p.ToTable("payments");
            p.HasKey(x => x.Id);
            Base(p);
            p.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
            p.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            p.Property(x => x.Method).HasColumnName("method").HasConversion<short>().IsRequired();
            p.Property(x => x.PaidAt).HasColumnName("paid_at").HasDefaultValueSql("now()");
            p.Property(x => x.ReceivedByUserId).HasColumnName("received_by_user_id").IsRequired();
            p.Property(x => x.Notes).HasColumnName("notes");
            p.HasIndex(x => x.InvoiceId).HasDatabaseName("ix_payments_invoice");
            p.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Supplier>(s =>
        {
            s.ToTable("suppliers");
            s.HasKey(x => x.Id);
            Base(s);
            s.Property(x => x.Name).HasColumnName("name").IsRequired();
            s.Property(x => x.Phone).HasColumnName("phone");
            s.Property(x => x.Email).HasColumnName("email");
            s.Property(x => x.Address).HasColumnName("address");
            s.HasIndex(x => x.Name).HasDatabaseName("ix_suppliers_name");
        });

        modelBuilder.Entity<Part>(p =>
        {
            p.ToTable("parts");
            p.HasKey(x => x.Id);
            Base(p);
            p.Property(x => x.Sku).HasColumnName("sku").IsRequired();
            p.Property(x => x.Name).HasColumnName("name").IsRequired();
            p.Property(x => x.Brand).HasColumnName("brand");
            p.Property(x => x.Unit).HasColumnName("unit");
            p.HasIndex(x => x.Sku).IsUnique().HasDatabaseName("uq_parts_sku");
        });

        modelBuilder.Entity<Location>(l =>
        {
            l.ToTable("locations");
            l.HasKey(x => x.Id);
            Base(l);
            l.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            l.Property(x => x.Code).HasColumnName("code").IsRequired();
            l.Property(x => x.Name).HasColumnName("name").IsRequired();
            l.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            l.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasDatabaseName("uq_locations_branch_code");
        });

        modelBuilder.Entity<PartStock>(ps =>
        {
            ps.ToTable("part_stocks");
            ps.HasKey(x => x.Id);
            Base(ps);
            ps.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            ps.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();
            ps.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            ps.Property(x => x.QuantityOnHand).HasColumnName("qty_on_hand").HasColumnType("numeric(12,2)");
            ps.HasIndex(x => new { x.BranchId, x.LocationId, x.PartId }).IsUnique().HasDatabaseName("uq_partstock_branch_loc_part");
        });

        modelBuilder.Entity<StockLedger>(sl =>
        {
            sl.ToTable("stock_ledger");
            sl.HasKey(x => x.Id);
            Base(sl);
            sl.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            sl.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();
            sl.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            sl.Property(x => x.MovementType).HasColumnName("movement_type").HasConversion<short>().IsRequired();
            sl.Property(x => x.ReferenceType).HasColumnName("reference_type").IsRequired();
            sl.Property(x => x.ReferenceId).HasColumnName("reference_id");
            sl.Property(x => x.QuantityDelta).HasColumnName("qty_delta").HasColumnType("numeric(12,2)");
            sl.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(12,4)");
            sl.Property(x => x.Notes).HasColumnName("notes");
            sl.Property(x => x.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired();
            sl.Property(x => x.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("now()");
            sl.HasIndex(x => x.BranchId).HasDatabaseName("ix_stockledger_branch");
        });

        modelBuilder.Entity<PurchaseOrder>(po =>
        {
            po.ToTable("purchase_orders");
            po.HasKey(x => x.Id);
            Base(po);
            po.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            po.Property(x => x.SupplierId).HasColumnName("supplier_id").IsRequired();
            po.Property(x => x.OrderNo).HasColumnName("order_no").IsRequired();
            po.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            po.Property(x => x.OrderedAt).HasColumnName("ordered_at");
            po.Property(x => x.ReceivedAt).HasColumnName("received_at");
            po.Property(x => x.Notes).HasColumnName("notes");
            po.HasIndex(x => new { x.BranchId, x.OrderNo }).IsUnique().HasDatabaseName("uq_po_branch_orderno");
        });

        modelBuilder.Entity<PurchaseOrderItem>(poi =>
        {
            poi.ToTable("purchase_order_items");
            poi.HasKey(x => x.Id);
            Base(poi);
            poi.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id").IsRequired();
            poi.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            poi.Property(x => x.Qty).HasColumnName("qty").HasColumnType("numeric(12,2)");
            poi.Property(x => x.UnitCost).HasColumnName("unit_cost").HasColumnType("numeric(12,4)");
            poi.Property(x => x.ReceivedQty).HasColumnName("received_qty").HasColumnType("numeric(12,2)");
            poi.HasIndex(x => new { x.PurchaseOrderId, x.PartId }).HasDatabaseName("ix_po_items_po_part");
        });

        modelBuilder.Entity<StockAdjustment>(sa =>
        {
            sa.ToTable("stock_adjustments");
            sa.HasKey(x => x.Id);
            Base(sa);
            sa.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            sa.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();
            sa.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            sa.Property(x => x.QuantityDelta).HasColumnName("qty_delta").HasColumnType("numeric(12,2)");
            sa.Property(x => x.Reason).HasColumnName("reason").IsRequired();
            sa.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            sa.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id");
        });

        modelBuilder.Entity<StockTransfer>(st =>
        {
            st.ToTable("stock_transfers");
            st.HasKey(x => x.Id);
            Base(st);
            st.Property(x => x.FromBranchId).HasColumnName("from_branch_id").IsRequired();
            st.Property(x => x.FromLocationId).HasColumnName("from_location_id").IsRequired();
            st.Property(x => x.ToBranchId).HasColumnName("to_branch_id").IsRequired();
            st.Property(x => x.ToLocationId).HasColumnName("to_location_id").IsRequired();
            st.Property(x => x.TransferNo).HasColumnName("transfer_no").IsRequired();
            st.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            st.Property(x => x.RequestedAt).HasColumnName("requested_at");
            st.Property(x => x.ShippedAt).HasColumnName("shipped_at");
            st.Property(x => x.ReceivedAt).HasColumnName("received_at");
            st.Property(x => x.Notes).HasColumnName("notes");
            st.HasIndex(x => x.TransferNo).IsUnique().HasDatabaseName("uq_transfers_no");
        });

        modelBuilder.Entity<StockTransferItem>(sti =>
        {
            sti.ToTable("stock_transfer_items");
            sti.HasKey(x => x.Id);
            Base(sti);
            sti.Property(x => x.StockTransferId).HasColumnName("stock_transfer_id").IsRequired();
            sti.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            sti.Property(x => x.Qty).HasColumnName("qty").HasColumnType("numeric(12,2)");
        });

        modelBuilder.Entity<JobCardPartUsage>(u =>
        {
            u.ToTable("jobcard_part_usages");
            u.HasKey(x => x.Id);
            Base(u);
            u.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            u.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            u.Property(x => x.LocationId).HasColumnName("location_id").IsRequired();
            u.Property(x => x.QuantityUsed).HasColumnName("qty_used").HasColumnType("numeric(12,2)");
            u.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)");
            u.Property(x => x.UsedAt).HasColumnName("used_at").HasDefaultValueSql("now()");
            u.Property(x => x.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired();
            u.HasIndex(x => x.JobCardId).HasDatabaseName("ix_usage_jobcard");
        });

        modelBuilder.Entity<Expense>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(x => x.Id);
            Base(e);
            e.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            e.Property(x => x.Category).HasColumnName("category").HasConversion<short>().IsRequired();
            e.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.ExpenseAt).HasColumnName("expense_at").IsRequired();
            e.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            e.HasIndex(x => x.BranchId).HasDatabaseName("ix_expenses_branch");
        });

        modelBuilder.Entity<EmployeeProfile>(ep =>
        {
            ep.ToTable("employee_profiles");
            ep.HasKey(x => x.Id);
            Base(ep);
            ep.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            ep.Property(x => x.FullName).HasColumnName("full_name").IsRequired();
            ep.Property(x => x.CNIC).HasColumnName("cnic");
            ep.Property(x => x.BaseSalary).HasColumnName("base_salary").HasColumnType("numeric(12,2)");
            ep.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            ep.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("uq_employeeprofile_user");
        });

        modelBuilder.Entity<WagePayment>(wp =>
        {
            wp.ToTable("wage_payments");
            wp.HasKey(x => x.Id);
            Base(wp);
            wp.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            wp.Property(x => x.EmployeeUserId).HasColumnName("employee_user_id").IsRequired();
            wp.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(12,2)");
            wp.Property(x => x.PeriodStart).HasColumnName("period_start").IsRequired();
            wp.Property(x => x.PeriodEnd).HasColumnName("period_end").IsRequired();
            wp.Property(x => x.PaidAt).HasColumnName("paid_at").HasDefaultValueSql("now()");
            wp.Property(x => x.PaidByUserId).HasColumnName("paid_by_user_id").IsRequired();
            wp.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<Attachment>(a =>
        {
            a.ToTable("attachments");
            a.HasKey(x => x.Id);
            Base(a);
            a.Property(x => x.OwnerType).HasColumnName("owner_type").IsRequired();
            a.Property(x => x.OwnerId).HasColumnName("owner_id").IsRequired();
            a.Property(x => x.FileName).HasColumnName("file_name").IsRequired();
            a.Property(x => x.ContentType).HasColumnName("content_type").IsRequired();
            a.Property(x => x.SizeBytes).HasColumnName("size_bytes").IsRequired();
            a.Property(x => x.StorageKey).HasColumnName("storage_key").IsRequired();
            a.Property(x => x.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("now()");
            a.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id").IsRequired();
        });

        modelBuilder.Entity<AuditLog>(a =>
        {
            a.ToTable("audit_logs");
            a.HasKey(x => x.Id);
            Base(a);
            a.Property(x => x.BranchId).HasColumnName("branch_id");
            a.Property(x => x.Action).HasColumnName("action").IsRequired();
            a.Property(x => x.EntityType).HasColumnName("entity_type").IsRequired();
            a.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
            a.Property(x => x.OldValue).HasColumnName("old_value");
            a.Property(x => x.NewValue).HasColumnName("new_value");
            a.Property(x => x.PerformedByUserId).HasColumnName("performed_by_user_id").IsRequired();
            a.Property(x => x.PerformedAt).HasColumnName("performed_at").HasDefaultValueSql("now()");
            a.HasIndex(x => x.PerformedAt).HasDatabaseName("ix_audit_performed_at");
        });

        modelBuilder.Entity<Roadblocker>(r =>
        {
            r.ToTable("roadblockers");
            r.HasKey(x => x.Id);
            Base(r);
            r.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            r.Property(x => x.Type).HasColumnName("type").HasConversion<short>().IsRequired();
            r.Property(x => x.Description).HasColumnName("description");
            r.Property(x => x.IsResolved).HasColumnName("is_resolved").HasDefaultValue(false);
            r.Property(x => x.CreatedAtLocal).HasColumnName("created_at_local").HasDefaultValueSql("now()");
            r.Property(x => x.ResolvedAt).HasColumnName("resolved_at");
            r.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            r.HasIndex(x => x.JobCardId).HasDatabaseName("ix_roadblockers_jobcard");
        });

        modelBuilder.Entity<JobTask>(t =>
        {
            t.ToTable("job_tasks");
            t.HasKey(x => x.Id);
            Base(t);
            t.Property(x => x.JobCardId).HasColumnName("job_card_id").IsRequired();
            t.Property(x => x.StationCode).HasColumnName("station_code").IsRequired();
            t.Property(x => x.Title).HasColumnName("title").IsRequired();
            t.Property(x => x.StartedAt).HasColumnName("started_at");
            t.Property(x => x.EndedAt).HasColumnName("ended_at");
            t.Property(x => x.StartedByUserId).HasColumnName("started_by_user_id");
            t.Property(x => x.EndedByUserId).HasColumnName("ended_by_user_id");
            t.Property(x => x.TotalMinutes).HasColumnName("total_minutes").HasDefaultValue(0);
            t.Property(x => x.Notes).HasColumnName("notes");
            t.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            t.HasIndex(x => x.JobCardId).HasDatabaseName("ix_job_tasks_jobcard");
            t.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Approval>(a =>
        {
            a.ToTable("approvals");
            a.HasKey(x => x.Id);
            Base(a);
            a.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            a.Property(x => x.TargetType).HasColumnName("target_type").IsRequired();
            a.Property(x => x.TargetId).HasColumnName("target_id").IsRequired();
            a.Property(x => x.ApprovalType).HasColumnName("approval_type").HasConversion<short>().IsRequired();
            a.Property(x => x.ApprovedByUserId).HasColumnName("approved_by_user_id").IsRequired();
            a.Property(x => x.ApprovedAt).HasColumnName("approved_at").HasDefaultValueSql("now()");
            a.Property(x => x.Note).HasColumnName("note");
            a.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            a.HasIndex(x => new { x.BranchId, x.TargetType, x.TargetId }).HasDatabaseName("ix_approvals_target");
            a.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CommunicationLog>(c =>
        {
            c.ToTable("communication_logs");
            c.HasKey(x => x.Id);
            Base(c);
            c.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            c.Property(x => x.JobCardId).HasColumnName("job_card_id").IsRequired();
            c.Property(x => x.Channel).HasColumnName("channel").HasConversion<short>().IsRequired();
            c.Property(x => x.MessageType).HasColumnName("message_type").HasConversion<short>().IsRequired();
            c.Property(x => x.SentAt).HasColumnName("sent_at").HasDefaultValueSql("now()");
            c.Property(x => x.Notes).HasColumnName("notes");
            c.Property(x => x.SentByUserId).HasColumnName("sent_by_user_id").IsRequired();
            c.HasIndex(x => x.JobCardId).HasDatabaseName("ix_commlogs_jobcard");
            c.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
            c.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobPartRequest>(r =>
        {
            r.ToTable("job_part_requests");
            r.HasKey(x => x.Id);
            Base(r);
            r.Property(x => x.BranchId).HasColumnName("branch_id").IsRequired();
            r.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            r.Property(x => x.PartId).HasColumnName("part_id").IsRequired();
            r.Property(x => x.Qty).HasColumnName("qty").HasColumnType("numeric(12,2)");
            r.Property(x => x.StationCode).HasColumnName("station_code").IsRequired();
            r.Property(x => x.RequestedAt).HasColumnName("requested_at").IsRequired();
            r.Property(x => x.OrderedAt).HasColumnName("ordered_at");
            r.Property(x => x.ArrivedAt).HasColumnName("arrived_at");
            r.Property(x => x.StationSignedByUserId).HasColumnName("station_signed_by_user_id");
            r.Property(x => x.OfficeSignedByUserId).HasColumnName("office_signed_by_user_id");
            r.Property(x => x.Status).HasColumnName("status").HasConversion<short>().IsRequired();
            r.Property(x => x.SupplierId).HasColumnName("supplier_id");
            r.Property(x => x.PurchaseOrderId).HasColumnName("purchase_order_id");

            r.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
            r.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.StationSignedByUser).WithMany().HasForeignKey(x => x.StationSignedByUserId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.OfficeSignedByUser).WithMany().HasForeignKey(x => x.OfficeSignedByUserId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            r.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JobLineItem>(i =>
        {
            i.ToTable("job_line_items");
            i.HasKey(x => x.Id);
            Base(i);
            i.Property(x => x.JobCardId).HasColumnName("jobcard_id").IsRequired();
            i.Property(x => x.Type).HasColumnName("type").HasConversion<short>().IsRequired();
            i.Property(x => x.Title).HasColumnName("title").IsRequired();
            i.Property(x => x.Qty).HasColumnName("qty").HasColumnType("numeric(12,2)");
            i.Property(x => x.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)");
            i.Property(x => x.Total).HasColumnName("total").HasColumnType("numeric(12,2)");
            i.Property(x => x.Notes).HasColumnName("notes");
            i.Property(x => x.PartId).HasColumnName("part_id");
            i.Property(x => x.JobPartRequestId).HasColumnName("job_part_request_id");

            i.HasOne(x => x.JobCard).WithMany().HasForeignKey(x => x.JobCardId).OnDelete(DeleteBehavior.Cascade);
            i.HasOne(x => x.Part).WithMany().HasForeignKey(x => x.PartId).OnDelete(DeleteBehavior.Restrict);
            i.HasOne(x => x.JobPartRequest).WithMany().HasForeignKey(x => x.JobPartRequestId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        TouchEntities();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchEntities()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
