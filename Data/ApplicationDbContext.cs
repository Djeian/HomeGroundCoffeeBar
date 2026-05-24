    using Microsoft.EntityFrameworkCore;
    using HomeGroundCoffeeBar.Models;
    using Models;

    namespace HomeGroundCoffeeBar.Data
    {
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
            {
            }


            public DbSet<GCashModel> GCash { get; set; }
            public DbSet<PayMayaModel> PayMaya { get; set; }
            public DbSet<UserModel> Users { get; set; }
            public DbSet<Cart> Cart { get; set; }
            public DbSet<Order> Orders { get; set; }
            public DbSet<Product> Products { get; set; }
            public DbSet<RiderLocation> RiderLocations { get; set; }

            public DbSet<AppSetting> AppSettings { get; set; }


        }
    }
    //