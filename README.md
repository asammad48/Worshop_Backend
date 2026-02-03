Workshop Backend v2 (.NET 8 + PostgreSQL) – Ready to run
=======================================================

This solution extends the MVP with the 4 operational gaps we identified from your form:
1) WorkStations + Job movement history
2) Supervisor/Cashier approvals (traceable)
3) Technician time logs (start/stop, total minutes)
4) Invoice + payment status (Pending/PartiallyPaid/Paid) + JobCard auto-mark Paid

What is included
----------------
- Clean Architecture layout: Api / Application / Domain / Infrastructure / Shared
- JWT Auth + bcrypt + seeded users
- Branches (HQ only), Customers, Vehicles, JobCards (entry/exit, status, diagnosis)
- WorkStations (branch-scoped) + JobCard station history
- JobCard approvals (Supervisor/Cashier)
- JobCard time logs (technician)
- Invoices + payments + payment status

Included in v3 (from the 34-prompt roadmap)
------------------------------------------------------------
Inventory + finance slices included in v3:
- Suppliers, Parts
- Locations, PartStock (snapshot)
- StockLedger (immutable movements)
- Purchase Orders + receiving (stock-in)
- JobCard Part Usage (stock-out) [minimal; billing integration is still simple]
- Stock Adjustments
- Stock Transfers (multi-branch)
- Expenses + Wage payments
- Reports: summary + stuck vehicles + top roadblockers + inventory snapshot
- Attachments metadata (optional storage later)
- AuditLog (critical actions)


Quick start
-----------
1) Start Postgres:
   docker compose up -d

2) Configure:
   Copy src/Api/appsettings.Development.example.json to src/Api/appsettings.Development.json
   Update connection string if needed.

3) Run migrations:
   dotnet tool install --global dotnet-ef
   dotnet ef migrations add InitialV2 -p src/Infrastructure/Infrastructure.csproj -s src/Api/Api.csproj
   dotnet ef database update -p src/Infrastructure/Infrastructure.csproj -s src/Api/Api.csproj

4) Run API:
   dotnet run --project src/Api/Api.csproj

Swagger:
- http://localhost:5000/swagger (or port in console)

Seed users:
- admin@demo.com / Admin@123 (HQ_ADMIN)
- manager@branch1.com / Manager@123 (BRANCH_MANAGER, Branch 1)
- store@branch1.com / Store@123 (STOREKEEPER, Branch 1)
- cashier@branch1.com / Cashier@123 (CASHIER, Branch 1)
- tech@branch1.com / Tech@123 (TECHNICIAN, Branch 1)  [added in v2]


## v4 additions
- JobCard parts consumption (stock-out + ledger)
- Roadblockers CRUD (create/resolve/list)
- Attachments metadata endpoints
- Audit log middleware for mutating HTTP requests (HQ can query)
