namespace SistemaEventos.Models
{
    /// <summary>
    /// Nomes dos perfis (roles) do ASP.NET Core Identity usados no sistema.
    /// Usar constantes evita erros de digitação em [Authorize(Roles = ...)].
    /// </summary>
    public static class Perfis
    {
        public const string Admin = "Admin";
        public const string Usuario = "Usuario";
    }
}
