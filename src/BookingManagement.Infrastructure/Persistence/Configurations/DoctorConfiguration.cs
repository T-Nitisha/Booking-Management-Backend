using BookingManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingManagement.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FullName).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Specialization).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Email).IsRequired().HasMaxLength(250);
        builder.Property(d => d.AuthUserId).HasMaxLength(200);

        builder.HasIndex(d => d.AuthUserId);

        builder.HasMany(d => d.Slots)
            .WithOne(s => s.Doctor)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
