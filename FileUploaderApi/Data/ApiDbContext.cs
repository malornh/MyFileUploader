using FileUploaderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileUploaderApi.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FileDetails> Files { get; set; }
    }
}
