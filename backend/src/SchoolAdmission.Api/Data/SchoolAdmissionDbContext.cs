using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Domain;

namespace SchoolAdmission.Api.Data;

/// <summary>
/// Database context for school admission persistence.
/// </summary>
public sealed class SchoolAdmissionDbContext : DbContext
{
    public SchoolAdmissionDbContext(DbContextOptions<SchoolAdmissionDbContext> options)
        : base(options)
    {
    }

    public DbSet<AdmissionClass> Classes => Set<AdmissionClass>();

    public DbSet<AdmissionHold> Holds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolAdmissionDbContext).Assembly);
    }
}
