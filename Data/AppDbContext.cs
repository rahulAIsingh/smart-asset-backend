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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<AssetEntity>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(255);
            entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.SerialNumber).IsUnique();
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
    }
}
