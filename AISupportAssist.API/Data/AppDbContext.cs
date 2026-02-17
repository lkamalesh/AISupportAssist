using AISupportAssist.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AISupportAssist.API.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>  
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<Faq> Faqs { get; set; }
    }
}
