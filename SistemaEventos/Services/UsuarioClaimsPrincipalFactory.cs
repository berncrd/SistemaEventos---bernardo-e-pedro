using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SistemaEventos.Models;

namespace SistemaEventos.Services
{
    /// <summary>
    /// Adiciona o nome completo do usuário ao cookie de autenticação,
    /// para exibi-lo no menu sem consultar o banco a cada requisição.
    /// </summary>
    public class UsuarioClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public const string ClaimNomeCompleto = "NomeCompleto";

        public UsuarioClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identidade = await base.GenerateClaimsAsync(user);
            identidade.AddClaim(new Claim(ClaimNomeCompleto, user.NomeCompleto ?? string.Empty));
            return identidade;
        }
    }
}
