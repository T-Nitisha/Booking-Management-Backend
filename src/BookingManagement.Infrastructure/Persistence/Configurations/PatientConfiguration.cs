using BookingManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingManagement.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(250);
        builder.Property(p => p.Phone).HasMaxLength(30);
        builder.Property(p => p.AuthUserId).HasMaxLength(200);

        builder.HasIndex(p => p.Email).IsUnique();
        builder.HasIndex(p => p.AuthUserId);
    }
}
