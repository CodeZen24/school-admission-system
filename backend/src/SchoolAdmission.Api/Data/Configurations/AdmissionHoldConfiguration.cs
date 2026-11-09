using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data.Configurations;

public sealed class AdmissionHoldConfiguration : IEntityTypeConfiguration<AdmissionHold>
{
    public void Configure(EntityTypeBuilder<AdmissionHold> builder)
    {
        builder.ToTable("admission_holds", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_student_id_format",
                "student_id ~ '^[0-9]{17}$'");

            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_status_valid",
                "status IN ('HELD', 'CONFIRMED', 'CANCELLED', 'EXPIRED')");
        });

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id");

        builder.Property(h => h.ClassId)
            .HasColumnName("class_id")
            .IsRequired();

        builder.Property(h => h.StudentId)
            .HasColumnName("student_id")
            .HasMaxLength(17)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(h => h.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(h => h.ConfirmedAt)
            .HasColumnName("confirmed_at");

        builder.Property(h => h.CancelledAt)
            .HasColumnName("cancelled_at");

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasIndex(h => new { h.ClassId, h.StudentId })
            .IsUnique()
            .HasFilter("status IN ('HELD', 'CONFIRMED')")
            .HasDatabaseName("ix_admission_holds_class_id_student_id_active");

        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_admission_holds_class_id_status_expires_at");

        builder.HasIndex(h => h.ExpiresAt)
            .HasFilter("status = 'HELD'")
            .HasDatabaseName("ix_admission_holds_expires_at_held");

        builder.HasIndex(h => new { h.StudentId, h.CreatedAt })
            .HasDatabaseName("ix_admission_holds_student_id_created_at");
    }
}
