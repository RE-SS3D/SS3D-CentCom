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
        public DbSet<GameServer> Servers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameServer>().HasKey(u => new { u.Address, u.QueryPort });
        }
    }
}