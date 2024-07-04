using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shop.Models;

namespace Shop.Data
{
    /// <summary>
    /// Represents the database context for the application.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        /// <summary>
        /// Constructor for initializing the application database context.
        /// </summary>
        /// <param name="options">The DbContext options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DbSet for sales records.
        /// </summary>
        public DbSet<Sale> Sales { get; set; }

        /// <summary>
        /// Configures the model schema and relationships between entities.
        /// </summary>
        /// <param name="builder">The model builder instance.</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Customize the ASP.NET Identity model and override configuration if needed
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
}
