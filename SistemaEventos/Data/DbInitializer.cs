using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaEventos.Models;

namespace SistemaEventos.Data
{
    /// <summary>
    /// Executado na inicialização: aplica as migrações pendentes, cria os perfis
    /// (Admin e Usuario) e garante que exista uma conta de administrador.
    /// Os dados de demonstração (eventos, inscrições...) ficam no script Database/populacao.sql.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InicializarAsync(IServiceProvider services)
        {
            using var escopo = services.CreateScope();
            var provedor = escopo.ServiceProvider;
            var logger = provedor.GetRequiredService<ILoggerFactory>().CreateLogger("Inicializacao");

            try
            {
                var context = provedor.GetRequiredService<ApplicationDbContext>();
                await context.Database.MigrateAsync();

                var roleManager = provedor.GetRequiredService<RoleManager<IdentityRole>>();
                foreach (var perfil in new[] { Perfis.Admin, Perfis.Usuario })
                {
                    if (!await roleManager.RoleExistsAsync(perfil))
                    {
                        await roleManager.CreateAsync(new IdentityRole(perfil));
                    }
                }

                var configuracao = provedor.GetRequiredService<IConfiguration>();
                var nome = configuracao["AdminPadrao:Nome"] ?? "Administrador do Sistema";
                var email = configuracao["AdminPadrao:Email"] ?? "admin@eventos.com";
                var senha = configuracao["AdminPadrao:Senha"] ?? "Admin@123";

                var userManager = provedor.GetRequiredService<UserManager<ApplicationUser>>();
                var admin = await userManager.FindByEmailAsync(email);
                if (admin == null)
                {
                    admin = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true,
                        NomeCompleto = nome,
                        DataCadastro = DateTime.Now
                    };

                    var resultado = await userManager.CreateAsync(admin, senha);
                    if (!resultado.Succeeded)
                    {
                        var erros = string.Join("; ", resultado.Errors.Select(e => e.Description));
                        logger.LogError("Não foi possível criar o administrador padrão: {Erros}", erros);
                        return;
                    }

                    logger.LogInformation("Administrador padrão criado: {Email}", email);
                }

                if (!await userManager.IsInRoleAsync(admin, Perfis.Admin))
                {
                    await userManager.AddToRoleAsync(admin, Perfis.Admin);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Falha ao preparar o banco de dados. Verifique a string de conexão 'ConexaoSqlServer' no appsettings.json e se o SQL Server está em execução.");
                throw;
            }
        }
    }
}
