using Microsoft.EntityFrameworkCore;

namespace CapExample
{
    public class Person
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public override string ToString()
        {
            return $"Name:{Name}, Id:{Id}";
        }
    }


    public class AppDbContext : DbContext
    {
        public const string ConnectionString = "Server=10.27.254.167;Port=3306;Database=cap;Uid=root;Pwd=root;";

        public DbSet<Person>? Persons { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(ConnectionString, ServerVersion.AutoDetect(ConnectionString));
        }
    }
}
