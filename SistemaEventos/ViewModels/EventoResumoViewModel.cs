using System.Linq.Expressions;
using SistemaEventos.Extensions;
using SistemaEventos.Models;

namespace SistemaEventos.ViewModels
{
    /// <summary>
    /// Dados de um evento para exibição (cards, listas e detalhes),
    /// já com a contagem de inscrições ativas calculada pelo banco.
    /// </summary>
    public class EventoResumoViewModel
    {
        public const string ImagemPadrao = "/images/evento-padrao.svg";

        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Local { get; set; } = string.Empty;
        public int Vagas { get; set; }
        public int InscritosAtivos { get; set; }
        public decimal Preco { get; set; }
        public string? ImagemUrl { get; set; }
        public bool Ativo { get; set; }

        /// <summary>Preenchido pelo controller quando o usuário logado já está inscrito.</summary>
        public bool UsuarioInscrito { get; set; }

        public int VagasDisponiveis => Math.Max(Vagas - InscritosAtivos, 0);
        public bool Lotado => InscritosAtivos >= Vagas;
        public bool Encerrado => DataInicio <= DateTime.Now;
        public bool Realizado => DataFim < DateTime.Now;
        public bool Gratuito => Preco == 0;
        public string Imagem => string.IsNullOrWhiteSpace(ImagemUrl) ? ImagemPadrao : ImagemUrl;
        public string Periodo => FormatacaoExtensions.Periodo(DataInicio, DataFim);
        public int PercentualOcupacao => Vagas == 0 ? 0 : Math.Min(100, InscritosAtivos * 100 / Vagas);

        /// <summary>Situação para exibição no painel administrativo.</summary>
        public string Situacao => !Ativo ? "Rascunho" : Realizado ? "Realizado" : Encerrado ? "Em andamento" : Lotado ? "Lotado" : "Inscrições abertas";

        public string ClasseSituacao => !Ativo ? "text-bg-secondary" : Realizado ? "text-bg-dark" : Encerrado ? "text-bg-info" : Lotado ? "text-bg-danger" : "text-bg-success";

        /// <summary>
        /// Projeção usada nas consultas LINQ (o EF Core traduz para um único SELECT).
        /// </summary>
        public static readonly Expression<Func<Evento, EventoResumoViewModel>> Projecao = e => new EventoResumoViewModel
        {
            Id = e.Id,
            Titulo = e.Titulo,
            Descricao = e.Descricao,
            CategoriaId = e.CategoriaId,
            CategoriaNome = e.Categoria!.Nome,
            DataInicio = e.DataInicio,
            DataFim = e.DataFim,
            Local = e.Local,
            Vagas = e.Vagas,
            InscritosAtivos = e.Inscricoes.Count(i => i.Status != StatusInscricao.Cancelada),
            Preco = e.Preco,
            ImagemUrl = e.ImagemUrl,
            Ativo = e.Ativo
        };
    }
}
