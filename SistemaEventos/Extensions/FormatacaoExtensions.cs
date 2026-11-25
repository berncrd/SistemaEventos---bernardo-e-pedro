using System.Globalization;

namespace SistemaEventos.Extensions
{
    /// <summary>
    /// Formatação de datas e valores no padrão brasileiro.
    /// </summary>
    public static class FormatacaoExtensions
    {
        private static readonly CultureInfo Cultura = new("pt-BR");

        public static string Moeda(this decimal valor) => valor.ToString("C", Cultura);

        public static string PrecoOuGratuito(this decimal valor) => valor == 0 ? "Gratuito" : valor.ToString("C", Cultura);

        public static string DataCurta(this DateTime data) => data.ToString("dd/MM/yyyy", Cultura);

        public static string DataHora(this DateTime data) => data.ToString("dd/MM/yyyy HH:mm", Cultura);

        public static string Hora(this DateTime data) => data.ToString("HH:mm", Cultura);

        public static string DiaSemana(this DateTime data) => data.ToString("dddd", Cultura);

        /// <summary>Ex.: "28/11/2026, das 09:00 às 18:00" ou "28/11/2026 09:00 até 29/11/2026 18:00".</summary>
        public static string Periodo(DateTime inicio, DateTime fim)
        {
            if (inicio.Date == fim.Date)
            {
                return $"{inicio.DataCurta()}, das {inicio.Hora()} às {fim.Hora()}";
            }

            return $"{inicio.DataHora()} até {fim.DataHora()}";
        }
    }
}
