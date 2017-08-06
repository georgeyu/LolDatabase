using LolApi;
using System.Data.Entity;

namespace LolDatabase
{
    public class LolDbContext : DbContext
    {
        public LolDbContext() : base("name=SqlConnection") { }

        public DbSet<Match> Matches { get; set; }
    }
}
