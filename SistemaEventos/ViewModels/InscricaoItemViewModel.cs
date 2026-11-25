using System.Linq.Expressions;
using SistemaEventos.Models;

namespace SistemaEventos.ViewModels
{
    /// <summary>
    /// Uma inscrição com os dados do evento e do participante,
    /// usada em "Minhas inscrições" e no acompanhamento do administrador.
    /// </summary>
    public class InscricaoItemViewModel
    {
        public int Id { get; set; }

        public int EventoId { get; set; }
        public string EventoTitulo { get; set; } = string.Empty;
        public string CategoriaNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Local { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }

        public string UsuarioId { get; set; } = string.Empty;
        public string UsuarioNome { get; set; } = string.Empty;
        public string UsuarioEmail { get; set; } = string.Empty;
        public string? UsuarioTelefone { get; set; }

        public StatusInscricao Status { get; set; }
        public DateTime DataInscricao { get; set; }
        public DateTime? DataAtualizacao { get; set; }
        public decimal Valor { get; set; }
        public string? Observacao { get; set; }

        public string Imagem => string.IsNullOrWhiteSpace(ImagemUrl) ? EventoResumoViewModel.ImagemPadrao : ImagemUrl;
        public bool EventoIniciado => DataInicio <= DateTime.Now;

        /// <summary>Regra: o participante só cancela inscrições ativas e antes do início do evento.</summary>
        public bool PodeCancelar =>
            (Status == StatusInscricao.Pendente || Status == StatusInscricao.Confirmada) && !EventoIniciado;

        public static readonly Expression<Func<Inscricao, InscricaoItemViewModel>> Projecao = i => new InscricaoItemViewModel
        {
            Id = i.Id,
            EventoId = i.EventoId,
            EventoTitulo = i.Evento!.Titulo,
            CategoriaNome = i.Evento!.Categoria!.Nome,
            DataInicio = i.Evento!.DataInicio,
            DataFim = i.Evento!.DataFim,
            Local = i.Evento!.Local,
            ImagemUrl = i.Evento!.ImagemUrl,
            UsuarioId = i.UsuarioId,
            UsuarioNome = i.Usuario!.NomeCompleto,
            UsuarioEmail = i.Usuario!.Email ?? string.Empty,
            UsuarioTelefone = i.Usuario!.PhoneNumber,
            Status = i.Status,
            DataInscricao = i.DataInscricao,
            DataAtualizacao = i.DataAtualizacao,
            Valor = i.Valor,
            Observacao = i.Observacao
        };
    }
}
