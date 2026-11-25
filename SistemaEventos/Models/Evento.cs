using System.ComponentModel.DataAnnotations;

namespace SistemaEventos.Models
{
    public class Evento : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o título do evento.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "O título deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a descrição do evento.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a data e a hora de início.")]
        [Display(Name = "Início")]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "Informe a data e a hora de término.")]
        [Display(Name = "Término")]
        public DateTime DataFim { get; set; }

        [Required(ErrorMessage = "Informe o local do evento.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "O local deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Local")]
        public string Local { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a quantidade de vagas.")]
        [Range(1, 10000, ErrorMessage = "A quantidade de vagas deve estar entre {1} e {2}.")]
        [Display(Name = "Vagas")]
        public int Vagas { get; set; }

        [Required(ErrorMessage = "Informe o preço (use 0 para eventos gratuitos).")]
        [Range(typeof(decimal), "0", "99999", ErrorMessage = "O preço deve estar entre R$ {1} e R$ {2}.")]
        [Display(Name = "Preço (R$)")]
        public decimal Preco { get; set; }

        [StringLength(300)]
        [Display(Name = "Imagem")]
        public string? ImagemUrl { get; set; }

        [Display(Name = "Publicado (aceita inscrições)")]
        public bool Ativo { get; set; } = true;

        [Required(ErrorMessage = "Selecione a categoria.")]
        [Display(Name = "Categoria")]
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        // Relacionamento 1:N — um evento recebe várias inscrições
        public ICollection<Inscricao> Inscricoes { get; set; } = new List<Inscricao>();

        /// <summary>
        /// Validação entre campos: o término precisa ser depois do início.
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFim <= DataInicio)
            {
                yield return new ValidationResult(
                    "O término deve ser posterior ao início do evento.",
                    new[] { nameof(DataFim) });
            }
        }
    }
}
