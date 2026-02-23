using Microsoft.EntityFrameworkCore;
using SmartAssetManager.Api.Domain.Entities;

namespace SmartAssetManager.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<AssetEntity> Assets => Set<AssetEntity>();
    public DbSet<IssuanceEntity> Issuances => Set<IssuanceEntity>();
    public DbSet<MaintenanceEntity> Maintenance => Set<MaintenanceEntity>();
    public DbSet<StockTransactionEntity> StockTransactions => Set<StockTransactionEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<VendorEntity> Vendors => Set<VendorEntity>();
    public DbSet<FinanceProfileEntity> FinanceProfiles => Set<FinanceProfileEntity>();
    public DbSet<FinanceAssetOverrideEntity> FinanceAssetOverrides => Set<FinanceAssetOverrideEntity>();
    public DbSet<AssetRequestEntity> AssetRequests => Set<AssetRequestEntity>();
    public DbSet<AssetRequestApprovalEntity> AssetRequestApprovals => Set<AssetRequestApprovalEntity>();
    public DbSet<AssetRequestNotificationEntity> AssetRequestNotifications => Set<AssetRequestNotificationEntity>();
    public DbSet<AssetRequestAttachmentEntity> AssetRequestAttachments => Set<AssetRequestAttachmentEntity>();
    public DbSet<AssetRequestCommentEntity> AssetRequestComments => Set<AssetRequestCommentEntity>();
    public DbSet<AssetRequestAuditEntity> AssetRequestAudits => Set<AssetRequestAuditEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ManagerEmail).HasMaxLength(320);
            entity.Property(x => x.BossEmail).HasMaxLength(320);
            entity.Property(x => x.ReportingToEmail).HasMaxLength(320);
            entity.Property(x => x.BossApproverEmail).HasMaxLength(320);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AssetEntity>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(255);
            entity.Property(x => x.DeviceSerialNumber).HasMaxLength(255);
            entity.Property(x => x.Company).HasMaxLength(255);
            entity.Property(x => x.Model).HasMaxLength(255);
            entity.Property(x => x.Department).HasMaxLength(128);
            entity.Property(x => x.WarrantyStart).HasMaxLength(64);
            entity.Property(x => x.WarrantyEnd).HasMaxLength(64);
            entity.Property(x => x.WarrantyVendor).HasMaxLength(255);
            entity.Property(x => x.Configuration).HasMaxLength(4000);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.SerialNumber).IsUnique();
            entity.HasIndex(x => x.DeviceSerialNumber);
        });

        modelBuilder.Entity<IssuanceEntity>(entity =>
        {
            entity.ToTable("Issuances");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(512).IsRequired();
            entity.Property(x => x.UserEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.AssetId);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<MaintenanceEntity>(entity =>
        {
            entity.ToTable("Maintenance");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.PerformedBy).HasMaxLength(255).IsRequired();
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.AssetId);
        });

        modelBuilder.Entity<StockTransactionEntity>(entity =>
        {
            entity.ToTable("StockTransactions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(8000);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => x.AssetId);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Label).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Icon).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Value).IsUnique();
        });

        modelBuilder.Entity<DepartmentEntity>(entity =>
        {
            entity.ToTable("Departments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VendorEntity>(entity =>
        {
            entity.ToTable("Vendors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<FinanceProfileEntity>(entity =>
        {
            entity.ToTable("FinanceProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Category).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SalvageType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Frequency).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.Category).IsUnique();
        });

        modelBuilder.Entity<FinanceAssetOverrideEntity>(entity =>
        {
            entity.ToTable("FinanceAssetOverrides");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AssetId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Method).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SalvageType).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.AssetId).IsUnique();
        });

        modelBuilder.Entity<AssetRequestEntity>(entity =>
        {
            entity.ToTable("AssetRequests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RequestType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RequesterEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Department).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(255).IsRequired();
            entity.Property(x => x.BusinessJustification).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Urgency).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
            entity.Property(x => x.CurrentApprovalLevel).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PmApproverEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.BossApproverEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.RequestedConfigurationJson).HasMaxLength(12000);
            entity.HasIndex(x => x.RequestNumber).IsUnique();
            entity.HasIndex(x => x.RequesterEmail);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CurrentApprovalLevel);
            entity.HasIndex(x => x.PmApproverEmail);
            entity.HasIndex(x => x.BossApproverEmail);
        });

        modelBuilder.Entity<AssetRequestApprovalEntity>(entity =>
        {
            entity.ToTable("AssetRequestApprovals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Level).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ApproverEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Decision).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(4000);
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RequestId);
            entity.HasIndex(x => x.ApproverEmail);
        });

        modelBuilder.Entity<AssetRequestNotificationEntity>(entity =>
        {
            entity.ToTable("AssetRequestNotifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecipientEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RequestId);
            entity.HasIndex(x => x.RecipientEmail);
        });

        modelBuilder.Entity<AssetRequestAttachmentEntity>(entity =>
        {
            entity.ToTable("AssetRequestAttachments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.BlobPath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.UploadedBy).HasMaxLength(320).IsRequired();
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RequestId);
        });

        modelBuilder.Entity<AssetRequestCommentEntity>(entity =>
        {
            entity.ToTable("AssetRequestComments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthorEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Comment).HasMaxLength(4000).IsRequired();
            entity.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RequestId);
        });

        modelBuilder.Entity<AssetRequestAuditEntity>(entity =>
        {
            entity.ToTable("AssetRequestAudits");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.RequestNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RequestType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ActorEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.ActorRole).HasMaxLength(64);
            entity.Property(x => x.FromStatus).HasMaxLength(64);
            entity.Property(x => x.ToStatus).HasMaxLength(64);
            entity.Property(x => x.Decision).HasMaxLength(64);
            entity.Property(x => x.Comment).HasMaxLength(4000);
            entity.HasIndex(x => x.RequestId);
            entity.HasIndex(x => x.RequestNumber);
            entity.HasIndex(x => x.ActorEmail);
            entity.HasIndex(x => x.Action);
            entity.HasIndex(x => x.CreatedAt);
        });
    }
}
