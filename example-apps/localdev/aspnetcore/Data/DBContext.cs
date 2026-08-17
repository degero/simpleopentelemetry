// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;

namespace SimpleOpenTelemetry.Examples.AspNetCore.Data;


public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}