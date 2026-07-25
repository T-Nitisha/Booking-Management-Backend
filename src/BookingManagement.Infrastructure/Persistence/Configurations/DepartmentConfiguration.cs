using BookingManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingManagement.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.HasMany(d => d.Doctors)
            .WithOne(doc => doc.Department)
            .HasForeignKey(doc => doc.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
