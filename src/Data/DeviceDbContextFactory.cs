namespace iPhoneController.Data
{
    using Microsoft.EntityFrameworkCore;

    public static class DbContextFactory
    {
        public static DeviceDbContext Create(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DeviceDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            var context = new DeviceDbContext(optionsBuilder.Options);
            context.Database.EnsureCreated();

            return context;
        }
    }
}