using ChromeRiverService.Db.NciCommon.DbViewsModels;
using Microsoft.EntityFrameworkCore;

namespace ChromeRiverService.Db.NciCommon;


public partial class NciCommonContext(DbContextOptions<NciCommonContext> options, IConfiguration configuration) : DbContext(options)

{
    private readonly IConfiguration _config = configuration;
    public virtual DbSet<VwChromeRiverGetAllAllocation> VwChromeRiverGetAllAllocations { get; set; }

    public virtual DbSet<VwChromeRiverGetAllEntity> VwChromeRiverGetAllEntities { get; set; }

    public virtual DbSet<VwChromeRiverGetVendorInfo> VwChromeRiverGetVendorInfos { get; set; }

    public virtual DbSet<VwGetChromeRiverRole> VwGetChromeRiverRole { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
           => optionsBuilder.UseSqlServer(_config.GetValue<string>("NCI_LOCAL_CONNECTION_STRING"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VwChromeRiverGetAllAllocation>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwChromeRiverGetAllAllocation");

            entity.Property(e => e.AllocationName)
                .HasMaxLength(186)
                .IsUnicode(false);
            entity.Property(e => e.AllocationNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ClientName)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.ClientNumber)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.CloseDate).HasMaxLength(30);
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.OnSelect1EntityTypeCode)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("onSelect1EntityTypeCode");
            entity.Property(e => e.OnSelect2EntityTypeCode)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("onSelect2EntityTypeCode");
            entity.Property(e => e.Type)
                .HasMaxLength(3)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwChromeRiverGetAllEntity>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwChromeRiverGetAllEntity");

            entity.Property(e => e.EntityCode)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EntityName)
                .HasMaxLength(207)
                .IsUnicode(false);
            entity.Property(e => e.EntitytypeCode)
                .HasMaxLength(13)
                .IsUnicode(false);
            entity.Property(e => e.Extradata1)
                .HasMaxLength(6)
                .IsUnicode(false);
            entity.Property(e => e.SortOrder)
                .HasMaxLength(1)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwChromeRiverGetVendorInfo>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwChromeRiverGetVendorInfo");

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.EmployeeName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("Employee Name");
            entity.Property(e => e.VendorCode1)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.VendorCode2)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwGetChromeRiverRole>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vwGetChromeRiverRole");

            entity.Property(e => e.ApRole)
                .HasMaxLength(13)
                .IsUnicode(false)
                .HasColumnName("AP_Role");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(15)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
        });
        modelBuilder.HasSequence("CountBy1", "Test");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
