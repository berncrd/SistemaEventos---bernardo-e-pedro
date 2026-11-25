using System.ComponentModel.DataAnnotations;

namespace SistemaEventos.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Informe o nome da categoria.")]
        [StringLength(60, MinimumLength = 3, ErrorMessage = "O nome deve ter entre {2} e {1} caracteres.")]
        [Display(Name = "Nome")]
        public string Nome { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "A descrição deve ter no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        // Relacionamento 1:N — uma categoria agrupa vários eventos
        public ICollection<Evento> Eventos { get; set; } = new List<Evento>();
    }
}
