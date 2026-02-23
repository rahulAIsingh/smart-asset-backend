using Microsoft.EntityFrameworkCore;
using SmartAssetManager.Api.Data;
using SmartAssetManager.Api.Domain.Entities;

namespace SmartAssetManager.Api.Services;

public class TestDataSeeder
{
    private const string SeedPrefix = "e2e-";
    private static readonly DateTimeOffset SeedNow = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private readonly AppDbContext _db;

    public TestDataSeeder(AppDbContext db)
    {
        _db = db;
    }

    public async Task ResetAndSeedAsync(CancellationToken cancellationToken)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            await _db.Database.EnsureDeletedAsync(cancellationToken);
            await _db.Database.EnsureCreatedAsync(cancellationToken);
            await SeedBaselineAsync(cancellationToken);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    public async Task SeedScenarioAsync(string? scenario, CancellationToken cancellationToken)
    {
        await SeedLock.WaitAsync(cancellationToken);
        try
        {
            var key = (scenario ?? "baseline").Trim().ToLowerInvariant();
            switch (key)
            {
                case "":
                case "baseline":
                    await SeedBaselineAsync(cancellationToken);
                    break;
                case "assets":
                case "issuance":
                    await ClearAssetDomainAsync(cancellationToken);
                    await SeedAssetsAndOperationsAsync(cancellationToken);
                    break;
                case "requests":
                    await ClearRequestDomainAsync(cancellationToken);
                    await SeedRequestWorkflowAsync(cancellationToken);
                    break;
                case "finance":
                    await ClearFinanceDomainAsync(cancellationToken);
                    await SeedFinanceAsync(cancellationToken);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported seed scenario '{scenario}'.");
            }
        }
        finally
        {
            SeedLock.Release();
        }
    }

    public async Task SeedBaselineAsync(CancellationToken cancellationToken)
    {
        await ClearSeedDataAsync(cancellationToken);
        await SeedReferenceDataAsync(cancellationToken);
        await SeedAssetsAndOperationsAsync(cancellationToken);
        await SeedFinanceAsync(cancellationToken);
        await SeedRequestWorkflowAsync(cancellationToken);
    }

    private async Task ClearSeedDataAsync(CancellationToken cancellationToken)
    {
        await ClearRequestDomainAsync(cancellationToken);
        await ClearFinanceDomainAsync(cancellationToken);
        await ClearAssetDomainAsync(cancellationToken);
        await ClearReferenceDataAsync(cancellationToken);
    }

    private async Task ClearRequestDomainAsync(CancellationToken cancellationToken)
    {
        await _db.AssetRequestApprovals.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.AssetRequestNotifications.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.AssetRequestComments.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.AssetRequestAttachments.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.AssetRequestAudits.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.AssetRequests.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ClearFinanceDomainAsync(CancellationToken cancellationToken)
    {
        await _db.FinanceAssetOverrides.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.FinanceProfiles.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ClearAssetDomainAsync(CancellationToken cancellationToken)
    {
        await _db.Maintenance.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.StockTransactions.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.Issuances.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.Assets.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
    }

    private async Task ClearReferenceDataAsync(CancellationToken cancellationToken)
    {
        await _db.Users.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.Categories.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.Departments.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
        await _db.Vendors.Where(x => x.Id.StartsWith(SeedPrefix)).ExecuteDeleteAsync(cancellationToken);
    }

    private Task SeedReferenceDataAsync(CancellationToken cancellationToken)
    {
        var users = new[]
        {
            new UserEntity
            {
                Id = "e2e-user-admin",
                Email = "admin@demo.com",
                Role = "admin",
                Department = "IT",
                Name = "E2E Admin",
                ManagerEmail = "boss@demo.com",
                BossEmail = "boss@demo.com",
                ReportingToEmail = "boss@demo.com",
                BossApproverEmail = "boss@demo.com",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            },
            new UserEntity
            {
                Id = "e2e-user-support",
                Email = "support@demo.com",
                Role = "support",
                Department = "IT",
                Name = "E2E Support",
                ManagerEmail = "boss@demo.com",
                BossEmail = "boss@demo.com",
                ReportingToEmail = "boss@demo.com",
                BossApproverEmail = "boss@demo.com",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            },
            new UserEntity
            {
                Id = "e2e-user-pm",
                Email = "pm@demo.com",
                Role = "pm",
                Department = "IT",
                Name = "E2E PM",
                ManagerEmail = "boss@demo.com",
                BossEmail = "boss@demo.com",
                ReportingToEmail = "boss@demo.com",
                BossApproverEmail = "boss@demo.com",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            },
            new UserEntity
            {
                Id = "e2e-user-boss",
                Email = "boss@demo.com",
                Role = "boss",
                Department = "Management",
                Name = "E2E Boss",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            },
            new UserEntity
            {
                Id = "e2e-user-standard",
                Email = "user@demo.com",
                Role = "user",
                Department = "Finance",
                Name = "E2E User",
                ManagerEmail = "pm@demo.com",
                BossEmail = "boss@demo.com",
                ReportingToEmail = "pm@demo.com",
                BossApproverEmail = "boss@demo.com",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            },
            new UserEntity
            {
                Id = "e2e-user-rahul-admin",
                Email = "s.rahul@kavitechsolution.com",
                Role = "admin",
                Department = "IT",
                Name = "Rahul Admin",
                ManagerEmail = "boss@demo.com",
                BossEmail = "boss@demo.com",
                ReportingToEmail = "boss@demo.com",
                BossApproverEmail = "boss@demo.com",
                CreatedAt = SeedNow,
                UpdatedAt = SeedNow
            }
        };

        var categories = new[]
        {
            new CategoryEntity { Id = "e2e-cat-laptop", Label = "Laptop", Value = "laptop", Icon = "laptop", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new CategoryEntity { Id = "e2e-cat-printer", Label = "Printer", Value = "printer", Icon = "printer", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new CategoryEntity { Id = "e2e-cat-scanner", Label = "Scanner", Value = "scanner", Icon = "scanner", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new CategoryEntity { Id = "e2e-cat-mobile", Label = "Smartphone", Value = "smartphone", Icon = "smartphone", CreatedAt = SeedNow, UpdatedAt = SeedNow }
        };

        var departments = new[]
        {
            new DepartmentEntity { Id = "e2e-dept-it", Name = "IT", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new DepartmentEntity { Id = "e2e-dept-finance", Name = "Finance", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new DepartmentEntity { Id = "e2e-dept-hr", Name = "HR", CreatedAt = SeedNow, UpdatedAt = SeedNow }
        };

        var vendors = new[]
        {
            new VendorEntity { Id = "e2e-vendor-dell", Name = "Dell", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new VendorEntity { Id = "e2e-vendor-lenovo", Name = "Lenovo", CreatedAt = SeedNow, UpdatedAt = SeedNow },
            new VendorEntity { Id = "e2e-vendor-zebra", Name = "Zebra", CreatedAt = SeedNow, UpdatedAt = SeedNow }
        };

        _db.Users.AddRange(users);
        _db.Categories.AddRange(categories);
        _db.Departments.AddRange(departments);
        _db.Vendors.AddRange(vendors);
        return _db.SaveChangesAsync(cancellationToken);
    }

    private Task SeedAssetsAndOperationsAsync(CancellationToken cancellationToken)
    {
        var assets = new[]
        {
            new AssetEntity
            {
                Id = "e2e-asset-anchor",
                Name = "Stock Ledger Anchor",
                Category = "laptop",
                SerialNumber = "SAM-STOCK-ANCHOR",
                Company = "System",
                Model = "Anchor",
                Department = "IT",
                Location = "System Ledger ||| Company: System | Model: Anchor | Department: IT",
                Status = "available",
                CreatedAt = SeedNow.AddDays(-60),
                UpdatedAt = SeedNow.AddDays(-1)
            },
            new AssetEntity
            {
                Id = "e2e-asset-available",
                Name = "Dell Latitude 7420",
                Category = "laptop",
                SerialNumber = "E2E-LAP-001",
                DeviceSerialNumber = "SN-LAP-001",
                Company = "Dell",
                Model = "Latitude 7420",
                Department = "IT",
                WarrantyStart = "2025-01-01",
                WarrantyEnd = "2028-01-01",
                WarrantyVendor = "Dell",
                Configuration = "CPU: i7 | RAM: 16GB | SSD: 512GB",
                Location = "Main Office ||| Company: Dell | Model: Latitude 7420 | Department: IT",
                Status = "available",
                CreatedAt = SeedNow.AddDays(-40),
                UpdatedAt = SeedNow.AddDays(-1)
            },
            new AssetEntity
            {
                Id = "e2e-asset-issued",
                Name = "Lenovo ThinkPad T14",
                Category = "laptop",
                SerialNumber = "E2E-LAP-002",
                DeviceSerialNumber = "SN-LAP-002",
                Company = "Lenovo",
                Model = "ThinkPad T14",
                Department = "Finance",
                WarrantyStart = "2025-02-15",
                WarrantyEnd = "2028-02-15",
                WarrantyVendor = "Lenovo",
                Configuration = "CPU: i5 | RAM: 16GB | SSD: 512GB",
                Location = "Main Office ||| Company: Lenovo | Model: ThinkPad T14 | Department: Finance",
                Status = "issued",
                CreatedAt = SeedNow.AddDays(-35),
                UpdatedAt = SeedNow.AddDays(-1)
            },
            new AssetEntity
            {
                Id = "e2e-asset-maintenance",
                Name = "HP LaserJet 4000",
                Category = "printer",
                SerialNumber = "E2E-PRN-001",
                DeviceSerialNumber = "SN-PRN-001",
                Company = "HP",
                Model = "LaserJet 4000",
                Department = "HR",
                WarrantyStart = "2024-09-10",
                WarrantyEnd = "2027-09-10",
                WarrantyVendor = "HP",
                Configuration = "Mono Printer",
                Location = "HQ Floor 2 ||| Company: HP | Model: LaserJet 4000 | Department: HR",
                Status = "maintenance",
                CreatedAt = SeedNow.AddDays(-80),
                UpdatedAt = SeedNow.AddDays(-2)
            },
            new AssetEntity
            {
                Id = "e2e-asset-returned",
                Name = "Zebra Scanner TC52",
                Category = "scanner",
                SerialNumber = "E2E-SCN-001",
                DeviceSerialNumber = "SN-SCN-001",
                Company = "Zebra",
                Model = "TC52",
                Department = "Operations",
                WarrantyStart = "2025-03-01",
                WarrantyEnd = "2028-03-01",
                WarrantyVendor = "Zebra",
                Configuration = "Barcode scanner",
                Location = "Warehouse ||| Company: Zebra | Model: TC52 | Department: Operations",
                Status = "returned",
                CreatedAt = SeedNow.AddDays(-65),
                UpdatedAt = SeedNow.AddDays(-1)
            }
        };

        var issuances = new[]
        {
            new IssuanceEntity
            {
                Id = "e2e-iss-active",
                AssetId = "e2e-asset-issued",
                UserName = "E2E User",
                UserEmail = "user@demo.com",
                Status = "active",
                IssueDate = SeedNow.AddDays(-10),
                CreatedAt = SeedNow.AddDays(-10),
                UpdatedAt = SeedNow.AddDays(-10)
            },
            new IssuanceEntity
            {
                Id = "e2e-iss-returned",
                AssetId = "e2e-asset-returned",
                UserName = "E2E Support",
                UserEmail = "support@demo.com",
                Status = "returned",
                IssueDate = SeedNow.AddDays(-20),
                ReturnDate = SeedNow.AddDays(-5),
                CreatedAt = SeedNow.AddDays(-20),
                UpdatedAt = SeedNow.AddDays(-5)
            },
            new IssuanceEntity
            {
                Id = "e2e-ticket-open",
                AssetId = "e2e-asset-issued",
                UserName = "E2E User ||| hardware/Battery/Power ||| high ||| Lenovo ThinkPad T14 ||| Battery drains quickly",
                UserEmail = "user@demo.com",
                Status = "ticket_open",
                IssueDate = SeedNow.AddDays(-3),
                CreatedAt = SeedNow.AddDays(-3),
                UpdatedAt = SeedNow.AddDays(-3)
            },
            new IssuanceEntity
            {
                Id = "e2e-ticket-progress",
                AssetId = "e2e-asset-maintenance",
                UserName = "E2E Support ||| hardware/Display/Screen ||| medium ||| HP LaserJet 4000 ||| Screen flicker ||| Technician assigned",
                UserEmail = "support@demo.com",
                Status = "ticket_in-progress",
                IssueDate = SeedNow.AddDays(-4),
                CreatedAt = SeedNow.AddDays(-4),
                UpdatedAt = SeedNow.AddDays(-2)
            },
            new IssuanceEntity
            {
                Id = "e2e-ticket-resolved",
                AssetId = "e2e-asset-maintenance",
                UserName = "E2E User ||| software/Operating System ||| low ||| HP LaserJet 4000 ||| Driver issue ||| Driver reinstalled",
                UserEmail = "user@demo.com",
                Status = "ticket_resolved",
                IssueDate = SeedNow.AddDays(-7),
                CreatedAt = SeedNow.AddDays(-7),
                UpdatedAt = SeedNow.AddDays(-1)
            },
            new IssuanceEntity
            {
                Id = "e2e-ticket-return",
                AssetId = "e2e-asset-issued",
                UserName = "E2E User ||| hardware/Other ||| medium ||| Lenovo ThinkPad T14 ||| Requesting return process",
                UserEmail = "user@demo.com",
                Status = "ticket_return",
                IssueDate = SeedNow.AddDays(-1),
                CreatedAt = SeedNow.AddDays(-1),
                UpdatedAt = SeedNow.AddDays(-1)
            }
        };

        var maintenance = new[]
        {
            new MaintenanceEntity
            {
                Id = "e2e-maint-001",
                AssetId = "e2e-asset-issued",
                Type = "assignment",
                Description = "Issued to E2E User (user@demo.com)",
                Date = SeedNow.AddDays(-10),
                PerformedBy = "system"
            },
            new MaintenanceEntity
            {
                Id = "e2e-maint-002",
                AssetId = "e2e-asset-maintenance",
                Type = "issue",
                Description = "Resolved ticket: software/Operating System. Note: Driver reinstalled",
                Date = SeedNow.AddDays(-1),
                PerformedBy = "System (Auto-log)"
            }
        };

        var stockTransactions = new[]
        {
            new StockTransactionEntity
            {
                Id = "e2e-stock-in-approved",
                AssetId = "e2e-asset-anchor",
                Type = "in",
                Quantity = 12,
                Reason = BuildStockMeta("laptop", "Dell Latitude 7420", "Main Office", "2026-01-10", "Initial stock", "admin@demo.com", "2026-01-10T10:00:00Z", serialNumber: "E2E-LAP-001", vendor: "Dell", quantity: 12, unitCost: 65000m, totalCost: 780000m, referenceNumber: "PO-E2E-001", approvalStatus: "approved"),
                CreatedAt = SeedNow.AddDays(-5)
            },
            new StockTransactionEntity
            {
                Id = "e2e-stock-out-pending",
                AssetId = "e2e-asset-anchor",
                Type = "out",
                Quantity = 2,
                Reason = BuildStockMeta("laptop", "Dell Latitude 7420", "Main Office", "2026-01-12", "Pending transfer", "pm@demo.com", "2026-01-12T10:00:00Z", quantity: 2, reason: "Project transfer", reasonType: "transfer", fromLocation: "Main Office", toLocation: "Branch Office", approvalStatus: "pending"),
                CreatedAt = SeedNow.AddDays(-3)
            },
            new StockTransactionEntity
            {
                Id = "e2e-stock-out-rejected",
                AssetId = "e2e-asset-anchor",
                Type = "out",
                Quantity = 1,
                Reason = BuildStockMeta("printer", "HP LaserJet 4000", "HQ Floor 2", "2026-01-13", "Scrap attempt rejected", "support@demo.com", "2026-01-13T10:00:00Z", serialNumber: "E2E-PRN-001", quantity: 1, reason: "Damaged unit", reasonType: "scrap", scrapVendor: "E2E Scrap Co", approvalStatus: "rejected", approvedBy: "admin@demo.com", approvedDate: "2026-01-14T10:00:00Z"),
                CreatedAt = SeedNow.AddDays(-2)
            }
        };

        _db.Assets.AddRange(assets);
        _db.Issuances.AddRange(issuances);
        _db.Maintenance.AddRange(maintenance);
        _db.StockTransactions.AddRange(stockTransactions);
        return _db.SaveChangesAsync(cancellationToken);
    }

    private Task SeedFinanceAsync(CancellationToken cancellationToken)
    {
        var profiles = new[]
        {
            new FinanceProfileEntity
            {
                Id = "e2e-fin-prof-laptop",
                Category = "laptop",
                Method = "straight_line",
                UsefulLifeMonths = 36,
                SalvageType = "percent",
                SalvageValue = 10,
                Frequency = "monthly",
                ExpenseGl = "6100",
                AccumDepGl = "1700",
                Active = true,
                CreatedBy = "admin@demo.com",
                CreatedDate = SeedNow.AddDays(-50),
                UpdatedBy = "admin@demo.com",
                UpdatedDate = SeedNow.AddDays(-10)
            },
            new FinanceProfileEntity
            {
                Id = "e2e-fin-prof-printer",
                Category = "printer",
                Method = "straight_line",
                UsefulLifeMonths = 48,
                SalvageType = "fixed",
                SalvageValue = 5000,
                Frequency = "monthly",
                ExpenseGl = "6200",
                AccumDepGl = "1710",
                Active = false,
                CreatedBy = "admin@demo.com",
                CreatedDate = SeedNow.AddDays(-70),
                UpdatedBy = "admin@demo.com",
                UpdatedDate = SeedNow.AddDays(-20)
            }
        };

        var overrides = new[]
        {
            new FinanceAssetOverrideEntity
            {
                Id = "e2e-fin-override-001",
                AssetId = "e2e-asset-issued",
                Method = "straight_line",
                UsefulLifeMonths = 30,
                SalvageType = "percent",
                SalvageValue = 8,
                EffectiveFrom = new DateOnly(2026, 1, 1),
                CreatedBy = "admin@demo.com",
                CreatedDate = SeedNow.AddDays(-15)
            }
        };

        _db.FinanceProfiles.AddRange(profiles);
        _db.FinanceAssetOverrides.AddRange(overrides);
        return _db.SaveChangesAsync(cancellationToken);
    }

    private Task SeedRequestWorkflowAsync(CancellationToken cancellationToken)
    {
        var requests = new[]
        {
            NewRequest("e2e-req-pending-pm", "REQ-2026-0001", "new_asset", "user@demo.com", "pending_pm", "pm"),
            NewRequest("e2e-req-pending-boss", "REQ-2026-0002", "replacement", "pm@demo.com", "pending_boss", "boss"),
            NewRequest("e2e-req-pending-it", "REQ-2026-0003", "transfer", "user@demo.com", "pending_it_fulfillment", "it"),
            NewRequest("e2e-req-rejected-pm", "REQ-2026-0004", "upgrade", "user@demo.com", "rejected_pm", "closed", closed: true),
            NewRequest("e2e-req-rejected-boss", "REQ-2026-0005", "return", "user@demo.com", "rejected_boss", "closed", closed: true),
            NewRequest("e2e-req-rejected-it", "REQ-2026-0006", "temporary_loan", "user@demo.com", "rejected_it", "closed", closed: true),
            NewRequest("e2e-req-returned-info", "REQ-2026-0007", "damage", "user@demo.com", "returned_for_info", "pm"),
            NewRequest("e2e-req-fulfilled", "REQ-2026-0008", "new_asset", "user@demo.com", "fulfilled", "it"),
            NewRequest("e2e-req-closed", "REQ-2026-0009", "return", "user@demo.com", "closed", "closed", closed: true)
        };

        var approvals = new[]
        {
            NewApproval("e2e-req-appr-001", "e2e-req-pending-boss", "pm", "pm@demo.com", "approved", "PM approved"),
            NewApproval("e2e-req-appr-002", "e2e-req-pending-it", "pm", "pm@demo.com", "approved", "PM approved"),
            NewApproval("e2e-req-appr-003", "e2e-req-pending-it", "boss", "boss@demo.com", "approved", "Boss approved"),
            NewApproval("e2e-req-appr-004", "e2e-req-rejected-pm", "pm", "pm@demo.com", "rejected", "Insufficient justification"),
            NewApproval("e2e-req-appr-005", "e2e-req-rejected-boss", "pm", "pm@demo.com", "approved", "PM approved"),
            NewApproval("e2e-req-appr-006", "e2e-req-rejected-boss", "boss", "boss@demo.com", "rejected", "Budget freeze"),
            NewApproval("e2e-req-appr-007", "e2e-req-rejected-it", "pm", "pm@demo.com", "approved", "PM approved"),
            NewApproval("e2e-req-appr-008", "e2e-req-rejected-it", "boss", "boss@demo.com", "approved", "Boss approved"),
            NewApproval("e2e-req-appr-009", "e2e-req-rejected-it", "it", "support@demo.com", "rejected", "No compatible stock"),
            NewApproval("e2e-req-appr-010", "e2e-req-returned-info", "pm", "pm@demo.com", "returned_for_info", "Need more details"),
            NewApproval("e2e-req-appr-011", "e2e-req-fulfilled", "pm", "pm@demo.com", "approved", "Approved"),
            NewApproval("e2e-req-appr-012", "e2e-req-fulfilled", "boss", "boss@demo.com", "approved", "Approved"),
            NewApproval("e2e-req-appr-013", "e2e-req-fulfilled", "it", "support@demo.com", "approved", "Fulfilled"),
            NewApproval("e2e-req-appr-014", "e2e-req-closed", "pm", "pm@demo.com", "approved", "Approved"),
            NewApproval("e2e-req-appr-015", "e2e-req-closed", "boss", "boss@demo.com", "approved", "Approved"),
            NewApproval("e2e-req-appr-016", "e2e-req-closed", "it", "support@demo.com", "approved", "Closed by IT")
        };

        var notifications = new[]
        {
            NewNotification("e2e-req-note-001", "e2e-req-pending-pm", "pm@demo.com", "request_submitted"),
            NewNotification("e2e-req-note-002", "e2e-req-pending-boss", "boss@demo.com", "pm_approved"),
            NewNotification("e2e-req-note-003", "e2e-req-pending-it", "it.support@company.com", "pending_it_fulfillment"),
            NewNotification("e2e-req-note-004", "e2e-req-returned-info", "user@demo.com", "returned_for_info")
        };

        var comments = new[]
        {
            new AssetRequestCommentEntity
            {
                Id = "e2e-req-comment-001",
                RequestId = "e2e-req-pending-pm",
                AuthorEmail = "user@demo.com",
                Comment = "Need this for onboarding project work.",
                CreatedAt = SeedNow.AddDays(-1)
            }
        };

        var audits = new[]
        {
            NewAudit("e2e-req-audit-001", "e2e-req-pending-pm", "REQ-2026-0001", "new_asset", "created", "user@demo.com", "user", null, "pending_pm", "submitted", "Request submitted"),
            NewAudit("e2e-req-audit-002", "e2e-req-pending-boss", "REQ-2026-0002", "replacement", "approved", "pm@demo.com", "pm", "pending_pm", "pending_boss", "approved", "PM approved"),
            NewAudit("e2e-req-audit-003", "e2e-req-pending-it", "REQ-2026-0003", "transfer", "approved", "boss@demo.com", "boss", "pending_boss", "pending_it_fulfillment", "approved", "Boss approved"),
            NewAudit("e2e-req-audit-004", "e2e-req-rejected-pm", "REQ-2026-0004", "upgrade", "rejected", "pm@demo.com", "pm", "pending_pm", "rejected_pm", "rejected", "Insufficient justification"),
            NewAudit("e2e-req-audit-005", "e2e-req-rejected-boss", "REQ-2026-0005", "return", "rejected", "boss@demo.com", "boss", "pending_boss", "rejected_boss", "rejected", "Budget freeze"),
            NewAudit("e2e-req-audit-006", "e2e-req-rejected-it", "REQ-2026-0006", "temporary_loan", "rejected", "support@demo.com", "support", "pending_it_fulfillment", "rejected_it", "rejected", "No compatible stock"),
            NewAudit("e2e-req-audit-007", "e2e-req-returned-info", "REQ-2026-0007", "damage", "returned_for_info", "pm@demo.com", "pm", "pending_pm", "returned_for_info", "returned_for_info", "Need more details"),
            NewAudit("e2e-req-audit-008", "e2e-req-fulfilled", "REQ-2026-0008", "new_asset", "it_fulfilled", "support@demo.com", "support", "pending_it_fulfillment", "fulfilled", "approved", "Issued from IT stock"),
            NewAudit("e2e-req-audit-009", "e2e-req-closed", "REQ-2026-0009", "return", "it_closed", "support@demo.com", "support", "fulfilled", "closed", "approved", "Return process complete")
        };

        _db.AssetRequests.AddRange(requests);
        _db.AssetRequestApprovals.AddRange(approvals);
        _db.AssetRequestNotifications.AddRange(notifications);
        _db.AssetRequestComments.AddRange(comments);
        _db.AssetRequestAudits.AddRange(audits);
        return _db.SaveChangesAsync(cancellationToken);
    }

    private static AssetRequestEntity NewRequest(
        string id,
        string number,
        string requestType,
        string requesterEmail,
        string status,
        string level,
        bool closed = false)
    {
        var created = SeedNow.AddDays(-12);
        var updated = SeedNow.AddDays(-1);
        return new AssetRequestEntity
        {
            Id = id,
            RequestNumber = number,
            RequestType = requestType,
            RequesterEmail = requesterEmail,
            RequesterName = requesterEmail.Split('@')[0],
            RequesterUserId = $"id-{requesterEmail}",
            Department = "IT",
            CostCenter = "CC-100",
            Location = "Main Office",
            BusinessJustification = "E2E seeded request",
            Urgency = "medium",
            Status = status,
            CurrentApprovalLevel = level,
            PmApproverEmail = "pm@demo.com",
            BossApproverEmail = "boss@demo.com",
            DestinationUserEmail = "user@demo.com",
            DestinationManagerEmail = "pm@demo.com",
            RelatedAssetId = "e2e-asset-issued",
            RequestedCategory = "laptop",
            RequestedConfigurationJson = "{\"cpu\":\"i7\",\"ram\":\"16GB\"}",
            SecurityIncidentFlag = requestType == "loss_theft",
            IncidentDate = requestType == "loss_theft" ? SeedNow.AddDays(-2) : null,
            IncidentLocation = requestType == "loss_theft" ? "Main Office" : null,
            PoliceReportNumber = requestType == "loss_theft" ? "E2E-PR-001" : null,
            CreatedAt = created,
            UpdatedAt = updated,
            ClosedAt = closed ? updated : null
        };
    }

    private static AssetRequestApprovalEntity NewApproval(
        string id,
        string requestId,
        string level,
        string approverEmail,
        string decision,
        string comment)
    {
        return new AssetRequestApprovalEntity
        {
            Id = id,
            RequestId = requestId,
            Level = level,
            ApproverEmail = approverEmail,
            Decision = decision,
            Comment = comment,
            DecidedAt = SeedNow.AddDays(-1)
        };
    }

    private static AssetRequestNotificationEntity NewNotification(
        string id,
        string requestId,
        string recipientEmail,
        string type)
    {
        return new AssetRequestNotificationEntity
        {
            Id = id,
            RequestId = requestId,
            RecipientEmail = recipientEmail,
            Channel = "in_app",
            Type = type,
            Status = "sent",
            SentAt = SeedNow.AddDays(-1)
        };
    }

    private static AssetRequestAuditEntity NewAudit(
        string id,
        string requestId,
        string requestNumber,
        string requestType,
        string action,
        string actorEmail,
        string actorRole,
        string? fromStatus,
        string? toStatus,
        string? decision,
        string? comment)
    {
        return new AssetRequestAuditEntity
        {
            Id = id,
            RequestId = requestId,
            RequestNumber = requestNumber,
            RequestType = requestType,
            Action = action,
            ActorEmail = actorEmail,
            ActorRole = actorRole,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Decision = decision,
            Comment = comment,
            CreatedAt = SeedNow.AddDays(-1)
        };
    }

    private static string BuildStockMeta(
        string category,
        string itemName,
        string location,
        string transactionDate,
        string note,
        string createdBy,
        string createdDate,
        string? serialNumber = null,
        string? vendor = null,
        decimal? quantity = null,
        decimal? unitCost = null,
        decimal? totalCost = null,
        string? referenceNumber = null,
        string? reason = null,
        string? issuedTo = null,
        string? reasonType = null,
        string? fromLocation = null,
        string? toLocation = null,
        string? approvalStatus = null,
        string? approvedBy = null,
        string? approvedDate = null,
        string? scrapVendor = null)
    {
        static string Enc(string value) => Uri.EscapeDataString((value ?? string.Empty).Trim());

        var parts = new List<string>
        {
            "v2",
            $"c={Enc(category)}",
            $"i={Enc(itemName)}",
            $"l={Enc(location)}",
            $"d={Enc(transactionDate)}",
            $"n={Enc(note)}",
            $"by={Enc(createdBy)}",
            $"cd={Enc(createdDate)}"
        };

        if (!string.IsNullOrWhiteSpace(serialNumber)) parts.Add($"sn={Enc(serialNumber)}");
        if (!string.IsNullOrWhiteSpace(vendor)) parts.Add($"v={Enc(vendor)}");
        if (!string.IsNullOrWhiteSpace(referenceNumber)) parts.Add($"r={Enc(referenceNumber)}");
        if (!string.IsNullOrWhiteSpace(reason)) parts.Add($"rs={Enc(reason)}");
        if (!string.IsNullOrWhiteSpace(issuedTo)) parts.Add($"to={Enc(issuedTo)}");
        if (!string.IsNullOrWhiteSpace(reasonType)) parts.Add($"rt={Enc(reasonType)}");
        if (!string.IsNullOrWhiteSpace(fromLocation)) parts.Add($"fl={Enc(fromLocation)}");
        if (!string.IsNullOrWhiteSpace(toLocation)) parts.Add($"tl={Enc(toLocation)}");
        if (!string.IsNullOrWhiteSpace(approvalStatus)) parts.Add($"as={Enc(approvalStatus)}");
        if (!string.IsNullOrWhiteSpace(approvedBy)) parts.Add($"ab={Enc(approvedBy)}");
        if (!string.IsNullOrWhiteSpace(approvedDate)) parts.Add($"ad={Enc(approvedDate)}");
        if (!string.IsNullOrWhiteSpace(scrapVendor)) parts.Add($"sv={Enc(scrapVendor)}");
        if (quantity.HasValue) parts.Add($"q={quantity.Value}");
        if (unitCost.HasValue) parts.Add($"u={unitCost.Value}");
        if (totalCost.HasValue) parts.Add($"t={totalCost.Value}");

        return string.Join("|", parts);
    }
}
