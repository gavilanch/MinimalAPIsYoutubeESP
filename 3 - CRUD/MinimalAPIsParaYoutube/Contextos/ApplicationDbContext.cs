using Microsoft.EntityFrameworkCore;
using MinimalAPIsParaYoutube.Entidades;

namespace MinimalAPIsParaYoutube.Contextos
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Persona> Personas { get; set; }
    }
}
