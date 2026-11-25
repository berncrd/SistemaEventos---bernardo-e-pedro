using System.ComponentModel.DataAnnotations;

namespace SistemaEventos.Models
{
    /// <summary>
    /// Situação de uma inscrição. É gravada no banco como texto (nvarchar(20)).
    /// </summary>
    public enum StatusInscricao
    {
        [Display(Name = "Pendente")]
        Pendente = 0,

        [Display(Name = "Confirmada")]
        Confirmada = 1,

        [Display(Name = "Cancelada")]
        Cancelada = 2,

        [Display(Name = "Concluída")]
        Concluida = 3
    }
}
