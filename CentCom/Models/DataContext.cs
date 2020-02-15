using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Contracts;

namespace CentCom.Models
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Server> Servers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Contract.Requires(modelBuilder != null);

            // Set the Server primary key to be based off ip and port.
            modelBuilder.Entity<Server>().HasKey(s => new { s.Ip, s.Port });
        }
    }
}