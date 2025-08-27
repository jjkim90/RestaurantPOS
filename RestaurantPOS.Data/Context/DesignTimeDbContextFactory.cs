using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace RestaurantPOS.Data.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RestaurantContext>
    {
        public RestaurantContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RestaurantContext>();
            
            // 연결 문자열 직접 지정 (개발 환경용)
            var connectionString = @"Server=.\SQLEXPRESS;Database=RestaurantPOS;Integrated Security=true;TrustServerCertificate=true;";
            
            optionsBuilder.UseSqlServer(connectionString);

            return new RestaurantContext(optionsBuilder.Options);
        }
    }
}