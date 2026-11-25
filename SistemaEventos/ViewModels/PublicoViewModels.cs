using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEventos.Models;

namespace SistemaEventos.ViewModels
{
    public class HomeViewModel
    {
        public List<EventoResumoViewModel> ProximosEventos { get; set; } = new();
        public int TotalEventosAbertos { get; set; }
        public int TotalCategorias { get; set; }
        public int TotalParticipacoes { get; set; }
    }

    /// <summary>Catálogo de eventos com busca e filtros.</summary>
    public class CatalogoViewModel
    {
        public string? Busca { get; set; }
        public int? CategoriaId { get; set; }
        public bool SomenteComVagas { get; set; }
        public IEnumerable<SelectListItem> Categorias { get; set; } = Enumerable.Empty<SelectListItem>();
        public List<EventoResumoViewModel> Eventos { get; set; } = new();

        public bool FiltroAtivo => !string.IsNullOrWhiteSpace(Busca) || CategoriaId.HasValue || SomenteComVagas;
    }

    public class EventoDetalhesViewModel
    {
        public EventoResumoViewModel Evento { get; set; } = new();
        public int? MinhaInscricaoId { get; set; }
        public StatusInscricao? MinhaInscricaoStatus { get; set; }

        public bool InscricaoAtiva => MinhaInscricaoStatus.HasValue && MinhaInscricaoStatus != StatusInscricao.Cancelada;
    }

    /// <summary>Formulário de confirmação da inscrição.</summary>
    public class NovaInscricaoViewModel
    {
        [Required(ErrorMessage = "Evento não informado.")]
        public int EventoId { get; set; }

        [StringLength(300, ErrorMessage = "A observação deve ter no máximo {1} caracteres.")]
        [Display(Name = "Observação (opcional)")]
        public string? Observacao { get; set; }

        [ValidateNever]
        public EventoResumoViewModel? Evento { get; set; }
    }

    public class MinhasInscricoesViewModel
    {
        public StatusInscricao? StatusFiltro { get; set; }
        public List<InscricaoItemViewModel> Inscricoes { get; set; } = new();
        public Dictionary<StatusInscricao, int> Contagens { get; set; } = new();
        public int Total => Contagens.Values.Sum();

        public int Contagem(StatusInscricao status) => Contagens.TryGetValue(status, out var total) ? total : 0;
    }
}
