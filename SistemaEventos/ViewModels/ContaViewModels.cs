using System.ComponentModel.DataAnnotations;

namespace SistemaEventos.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe sua senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Manter conectado")]
        public bool LembrarMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class RegistroViewModel
    {
        [Required(ErrorMessage = "Informe seu nome completo.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe seu e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [StringLength(256, ErrorMessage = "O e-mail deve ter no máximo {1} caracteres.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Informe um telefone válido.")]
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo {1} caracteres.")]
        [Display(Name = "Telefone (opcional)")]
        public string? Telefone { get; set; }

        [Required(ErrorMessage = "Informe uma senha.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos {2} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        [Display(Name = "Confirmar senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }

    public class MinhaContaViewModel
    {
        [Required(ErrorMessage = "Informe seu nome completo.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O nome deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Informe um telefone válido.")]
        [StringLength(20, ErrorMessage = "O telefone deve ter no máximo {1} caracteres.")]
        [Display(Name = "Telefone")]
        public string? Telefone { get; set; }

        // Somente exibição (não são enviados pelo formulário)
        public string? Email { get; set; }
        public string? Perfil { get; set; }
        public DateTime? DataCadastro { get; set; }
        public int TotalInscricoes { get; set; }
        public int InscricoesAtivas { get; set; }
    }

    public class AlterarSenhaViewModel
    {
        [Required(ErrorMessage = "Informe a senha atual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha atual")]
        public string SenhaAtual { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a nova senha.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter pelo menos {2} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova senha")]
        public string NovaSenha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a nova senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NovaSenha), ErrorMessage = "As senhas não conferem.")]
        [Display(Name = "Confirmar nova senha")]
        public string ConfirmarNovaSenha { get; set; } = string.Empty;
    }
}
