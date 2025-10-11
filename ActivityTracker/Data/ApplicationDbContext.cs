using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ActivityTracker.Models;

namespace ActivityTracker.Data
{
    // Poprawka: Dodano `using Microsoft.EntityFrameworkCore;`
    // Poprawka: Konstruktor przekazuje `options` do klasy bazowej `base(options)`
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}

