using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SistemaEventos.Models
{
    /// <summary>
    /// Usuário do sistema. Estende o IdentityUser (tabela AspNetUsers)
    /// com o nome completo e a data de cadastro.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(100, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Display(Name = "Cadastrado em")]
        public DateTime DataCadastro { get; set; } = DateTime.Now;

        // Relacionamento 1:N — um usuário possui várias inscrições
        public ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();
    }
}
