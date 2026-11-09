using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data.Configurations;

public sealed class AdmissionHoldConfiguration : IEntityTypeConfiguration<AdmissionHold>
{
    /// <summary>Partial unique index allowing one HELD or CONFIRMED hold per student and class.</summary>
    public const string ActiveApplicationIndexName = "ix_admission_holds_class_id_student_id_active";

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

            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_expiry_after_creation",
                "expires_at > created_at");

            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_updated_after_creation",
                "updated_at >= created_at");

            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_confirmation_state",
                "(status = 'CONFIRMED') = (confirmed_at IS NOT NULL)");

            tableBuilder.HasCheckConstraint(
                "ck_admission_holds_cancellation_state",
                "(status = 'CANCELLED') = (cancelled_at IS NOT NULL)");
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
            .HasDatabaseName(ActiveApplicationIndexName);

        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_admission_holds_class_id_status_expires_at");

        builder.HasIndex(h => h.ExpiresAt)
            .HasFilter("status = 'HELD'")
            .HasDatabaseName("ix_admission_holds_expires_at_held");

        builder.HasIndex(h => new { h.StudentId, h.CreatedAt })
            .HasDatabaseName("ix_admission_holds_student_id_created_at");
    }
}
