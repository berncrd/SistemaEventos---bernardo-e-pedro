using System.ComponentModel.DataAnnotations;

namespace SistemaEventos.Models
{
    /// <summary>
    /// Inscrição de um usuário em um evento (operação principal do sistema).
    /// </summary>
    public class Inscricao
    {
        public int Id { get; set; }

        [Display(Name = "Evento")]
        public int EventoId { get; set; }
        public Evento? Evento { get; set; }

        [Required]
        [Display(Name = "Participante")]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser? Usuario { get; set; }

        [Display(Name = "Data da inscrição")]
        public DateTime DataInscricao { get; set; } = DateTime.Now;

        [Display(Name = "Situação")]
        public StatusInscricao Status { get; set; } = StatusInscricao.Pendente;

        /// <summary>
        /// Valor do evento no momento da inscrição (preço preservado).
        /// </summary>
        [Display(Name = "Valor")]
        public decimal Valor { get; set; }

        [StringLength(300, ErrorMessage = "A observação deve ter no máximo {1} caracteres.")]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        [Display(Name = "Última atualização")]
        public DateTime? DataAtualizacao { get; set; }
    }
}
