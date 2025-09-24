using EnergyBackend.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EnergyBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Device>Devices { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Device>()
            .HasIndex(d => d.DeviceIdentifier)
            .IsUnique();
    }
}
