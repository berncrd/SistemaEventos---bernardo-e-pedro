using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Models;

namespace SistemaEventos.Data
{
    /// <summary>
    /// Contexto do Entity Framework. Herda de IdentityDbContext para incluir
    /// as tabelas do ASP.NET Core Identity (AspNetUsers, AspNetRoles, ...).
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Categoria> Categorias { get; set; } = default!;
        public DbSet<Evento> Eventos { get; set; } = default!;
        public DbSet<Inscricao> Inscricoes { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Mantém a configuração das tabelas do Identity
            base.OnModelCreating(builder);

            builder.Entity<Categoria>(entidade =>
            {
                entidade.ToTable("tbCategorias");
                entidade.HasIndex(c => c.Nome).IsUnique();
            });

            builder.Entity<Evento>(entidade =>
            {
                entidade.ToTable("tbEventos");
                entidade.Property(e => e.Preco).HasPrecision(10, 2);

                entidade.HasOne(e => e.Categoria)
                        .WithMany(c => c.Eventos)
                        .HasForeignKey(e => e.CategoriaId)
                        .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Inscricao>(entidade =>
            {
                entidade.ToTable("tbInscricoes");
                entidade.Property(i => i.Valor).HasPrecision(10, 2);

                // Grava o status como texto para facilitar a leitura no banco
                entidade.Property(i => i.Status)
                        .HasConversion<string>()
                        .HasMaxLength(20);

                // Um usuário só pode ter uma inscrição por evento
                entidade.HasIndex(i => new { i.EventoId, i.UsuarioId }).IsUnique();

                entidade.HasOne(i => i.Evento)
                        .WithMany(e => e.Inscricoes)
                        .HasForeignKey(i => i.EventoId)
                        .OnDelete(DeleteBehavior.Cascade);

                entidade.HasOne(i => i.Usuario)
                        .WithMany(u => u.Inscricoes)
                        .HasForeignKey(i => i.UsuarioId)
                        .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
