using System.Text.Json;
using System.Text.Json.Serialization;
using Freshwalk.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Freshwalk.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private static readonly JsonSerializerOptions BookingJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<RiderProfile> RiderProfiles => Set<RiderProfile>();
    public DbSet<OrderRiderAssignment> OrderRiderAssignments => Set<OrderRiderAssignment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OrderAuditLog> OrderAuditLogs => Set<OrderAuditLog>();
    public DbSet<OrderOtpCode> OrderOtpCodes => Set<OrderOtpCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("FreshwalkUsers");
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.CustomerReference).HasMaxLength(40).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasIndex(x => x.PhoneNumber).IsUnique();
            e.HasIndex(x => x.CustomerReference).IsUnique();
        });
        builder.Entity<IdentityRole<Guid>>().ToTable("FreshwalkRoles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("FreshwalkUserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("FreshwalkUserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("FreshwalkUserLogins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("FreshwalkUserTokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("FreshwalkRoleClaims");

        builder.Entity<Booking>(e =>
        {
            e.Property(x => x.PriceKes).HasPrecision(10, 2);
            e.Property(x => x.SubtotalBeforeDiscountKes).HasPrecision(10, 2);
            e.Property(x => x.BundleDiscountPercent).HasPrecision(5, 4);
            e.Property(x => x.DiscountAmountKes).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
            var addOnsComparer = new ValueComparer<List<string>>(
                (a, b) => a!.SequenceEqual(b!),
                v => v.Aggregate(0, (h, x) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(x))),
                v => v.ToList());
            var linesComparer = new ValueComparer<List<ShoeLineItem>>(
                (a, b) => a!.Count == b!.Count && a.Zip(b).All(p => p.First.Color == p.Second.Color && p.First.Quantity == p.Second.Quantity),
                v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.Color.GetHashCode(StringComparison.Ordinal), x.Quantity)),
                v => v.Select(x => new ShoeLineItem { Color = x.Color, Quantity = x.Quantity }).ToList());

            e.Property(x => x.AddOns).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, BookingJsonOptions),
                    v => JsonSerializer.Deserialize<List<string>>(v, BookingJsonOptions) ?? new List<string>())
                .Metadata.SetValueComparer(addOnsComparer);
            e.Property(x => x.SneakersLines).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, BookingJsonOptions),
                    v => JsonSerializer.Deserialize<List<ShoeLineItem>>(v, BookingJsonOptions) ?? new List<ShoeLineItem>())
                .Metadata.SetValueComparer(linesComparer);
            e.Property(x => x.SuedeLines).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, BookingJsonOptions),
                    v => JsonSerializer.Deserialize<List<ShoeLineItem>>(v, BookingJsonOptions) ?? new List<ShoeLineItem>())
                .Metadata.SetValueComparer(linesComparer);
            e.Property(x => x.NubuckLines).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, BookingJsonOptions),
                    v => JsonSerializer.Deserialize<List<ShoeLineItem>>(v, BookingJsonOptions) ?? new List<ShoeLineItem>())
                .Metadata.SetValueComparer(linesComparer);
            e.Property(x => x.OfficialLeatherLines).HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, BookingJsonOptions),
                    v => JsonSerializer.Deserialize<List<ShoeLineItem>>(v, BookingJsonOptions) ?? new List<ShoeLineItem>())
                .Metadata.SetValueComparer(linesComparer);
            e.Property(x => x.BookingReference).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.BookingReference).IsUnique();
            e.HasOne(x => x.Payment).WithOne(x => x.Booking).HasForeignKey<Payment>(x => x.BookingId);
            e.HasOne(x => x.Order).WithOne(x => x.Booking).HasForeignKey<Order>(x => x.BookingId);
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.PaymentReference).HasMaxLength(40).IsRequired();
            e.Property(x => x.PayerPhoneNumber).HasMaxLength(20);
            e.HasIndex(x => x.PaymentReference).IsUnique();
        });

        builder.Entity<OrderOtpCode>(e =>
        {
            e.Property(x => x.OtpKind).HasMaxLength(20).IsRequired();
            e.Property(x => x.Code).HasMaxLength(12).IsRequired();
            e.HasIndex(x => new { x.OrderId, x.OtpKind }).IsUnique();
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Order>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.OrderReference).HasMaxLength(40);
            e.HasIndex(x => x.OrderReference).IsUnique();
        });

        builder.Entity<OrderRiderAssignment>(e =>
        {
            e.Property(x => x.AssignmentType).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.AssignmentReference).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.AssignmentReference).IsUnique();
        });

        builder.Entity<Notification>(e =>
        {
            e.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Subject).HasMaxLength(200);
        });

        builder.Entity<OrderAuditLog>(e =>
        {
            e.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(40);
            e.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(40);
        });

        var roles = new[]
        {
            new IdentityRole<Guid> { Id = Guid.Parse("8f66ba7e-18f6-4f87-93c5-5f15ba8a9301"), Name = "Customer", NormalizedName = "CUSTOMER" },
            new IdentityRole<Guid> { Id = Guid.Parse("2f238bb8-6a7a-4cc6-a10d-761f223d83d9"), Name = "Agent", NormalizedName = "AGENT" },
            new IdentityRole<Guid> { Id = Guid.Parse("285df5e0-a4e0-4e10-8db0-f481446cd241"), Name = "Rider", NormalizedName = "RIDER" },
            new IdentityRole<Guid> { Id = Guid.Parse("13ff8f8e-2f39-4f2f-9309-7cc846255f5b"), Name = "Admin", NormalizedName = "ADMIN" }
        };
        builder.Entity<IdentityRole<Guid>>().HasData(roles);
    }
}
