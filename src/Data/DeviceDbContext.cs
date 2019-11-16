namespace iPhoneController.Data
{
    using Microsoft.EntityFrameworkCore;

    using iPhoneController.Data.Models;

    public class DeviceDbContext : DbContext
    {
        public DeviceDbContext(DbContextOptions<DeviceDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Device>()
                .Property(e => e.Uuid)
                .HasColumnType("text");
        }
    }
}