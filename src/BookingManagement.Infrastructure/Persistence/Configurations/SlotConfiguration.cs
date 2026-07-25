using BookingManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingManagement.Infrastructure.Persistence.Configurations;

public class SlotConfiguration : IEntityTypeConfiguration<Slot>
{
    public void Configure(EntityTypeBuilder<Slot> builder)
    {
        builder.ToTable("slots");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        // Optimistic concurrency token: incremented manually whenever a Slot is
        // updated (e.g. Available -> Booked). Prevents double-booking races.
        builder.Property(s => s.Version).IsConcurrencyToken();

        builder.HasIndex(s => new { s.DoctorId, s.StartTimeUtc });

        builder.HasOne(s => s.Appointment)
            .WithOne(a => a.Slot)
            .HasForeignKey<Appointment>(a => a.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
