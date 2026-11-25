using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaEventos.Models;

namespace SistemaEventos.Extensions
{
    public static class StatusInscricaoExtensions
    {
        public static string Nome(this StatusInscricao status) => status switch
        {
            StatusInscricao.Pendente => "Pendente",
            StatusInscricao.Confirmada => "Confirmada",
            StatusInscricao.Cancelada => "Cancelada",
            StatusInscricao.Concluida => "Concluída",
            _ => status.ToString()
        };

        public static string ClasseBadge(this StatusInscricao status) => status switch
        {
            StatusInscricao.Pendente => "badge-status status-pendente",
            StatusInscricao.Confirmada => "badge-status status-confirmada",
            StatusInscricao.Cancelada => "badge-status status-cancelada",
            StatusInscricao.Concluida => "badge-status status-concluida",
            _ => "badge-status"
        };

        public static string Icone(this StatusInscricao status) => status switch
        {
            StatusInscricao.Pendente => "bi-hourglass-split",
            StatusInscricao.Confirmada => "bi-check2-circle",
            StatusInscricao.Cancelada => "bi-x-circle",
            StatusInscricao.Concluida => "bi-patch-check",
            _ => "bi-circle"
        };

        /// <summary>Lista de status para preencher um &lt;select&gt;.</summary>
        public static List<SelectListItem> ListaParaSelect(StatusInscricao? selecionado = null) =>
            Enum.GetValues<StatusInscricao>()
                .Select(s => new SelectListItem(s.Nome(), s.ToString(), selecionado == s))
                .ToList();
    }
}
