using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEventos.Models;

namespace SistemaEventos.ViewModels
{
    public class PainelViewModel
    {
        public int EventosAbertos { get; set; }
        public int TotalEventos { get; set; }
        public int InscricoesAtivas { get; set; }
        public int InscricoesPendentes { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalCategorias { get; set; }
        public decimal ReceitaConfirmada { get; set; }
        public List<EventoResumoViewModel> ProximosEventos { get; set; } = new();
        public List<InscricaoItemViewModel> UltimasInscricoes { get; set; } = new();
    }

    public class CategoriaListaViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int TotalEventos { get; set; }
    }

    public class AdminEventosViewModel
    {
        public string? Busca { get; set; }
        public int? CategoriaId { get; set; }
        public string? Situacao { get; set; }
        public IEnumerable<SelectListItem> Categorias { get; set; } = Enumerable.Empty<SelectListItem>();
        public List<EventoResumoViewModel> Eventos { get; set; } = new();
    }

    public class AdminEventoDetalhesViewModel
    {
        public EventoResumoViewModel Evento { get; set; } = new();
        public List<InscricaoItemViewModel> Inscricoes { get; set; } = new();
    }

    public class AdminInscricoesViewModel
    {
        public string? Busca { get; set; }
        public StatusInscricao? Status { get; set; }
        public int? EventoId { get; set; }
        public IEnumerable<SelectListItem> Eventos { get; set; } = Enumerable.Empty<SelectListItem>();
        public List<InscricaoItemViewModel> Inscricoes { get; set; } = new();
    }

    public class AdminInscricaoDetalhesViewModel
    {
        public InscricaoItemViewModel Inscricao { get; set; } = new();
        public int EventoVagas { get; set; }
        public int EventoInscritosAtivos { get; set; }
        public StatusInscricao NovoStatus { get; set; }
    }

    public class UsuarioItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefone { get; set; }
        public DateTime DataCadastro { get; set; }
        public string Perfis { get; set; } = string.Empty;
        public int TotalInscricoes { get; set; }
        public int InscricoesAtivas { get; set; }

        public bool EhAdmin => Perfis.Contains(Models.Perfis.Admin);
    }

    public class AdminUsuarioDetalhesViewModel
    {
        public UsuarioItemViewModel Usuario { get; set; } = new();
        public List<InscricaoItemViewModel> Inscricoes { get; set; } = new();
    }
}
