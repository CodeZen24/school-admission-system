using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data.Configurations;

public sealed class AdmissionClassConfiguration : IEntityTypeConfiguration<AdmissionClass>
{
    public void Configure(EntityTypeBuilder<AdmissionClass> builder)
    {
        builder.ToTable("admission_classes", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_admission_classes_name_nonempty",
                "length(trim(name)) > 0");

            tableBuilder.HasCheckConstraint(
                "ck_admission_classes_total_seats_positive",
                "total_seats > 0");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.TotalSeats)
            .HasColumnName("total_seats")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasMany(c => c.Holds)
            .WithOne(h => h.Class)
            .HasForeignKey(h => h.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
