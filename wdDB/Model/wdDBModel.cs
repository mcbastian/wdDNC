using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

/* cd SOLUTION_HOME
* cd wdDB
* dotnet ef migrations add MIGRATIONNAME --startup-project=../wdWeb
* cd ../wdWeb
* dotnet ef database update
*/
namespace wdDB.Model
{
    public class Model1Factory : IDesignTimeDbContextFactory<wdDBModel>
    {
        public wdDBModel CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<wdDBModel>();
            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=SUPERSECRET;Database=wd");
            return new wdDBModel(optionsBuilder.Options);
        }
    }
    public class wdDBModel : IdentityDbContext<ApplicationUser>
    {
        public virtual DbSet<Job> Job { get; set; }
        public wdDBModel(DbContextOptions<wdDBModel> options) : base(options)
        {
            
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.UseSerialColumns();
        }
    }
}