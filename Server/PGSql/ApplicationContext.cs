using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.PGSql
{
    public class ApplicationContext : DbContext
    {
        private readonly string ConnectionString = "Host=localhost;Database=Test;Username=op;Password=Op@1234";
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(ConnectionString);

        public DbSet<Common.User.UserInfoData> Users { get; set; }
    }
}
