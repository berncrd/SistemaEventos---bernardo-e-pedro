namespace SistemaEventos.Services
{
    public interface IImagemService
    {
        /// <summary>Retorna uma mensagem de erro ou null se o arquivo for válido (ou não foi enviado).</summary>
        string? Validar(IFormFile? arquivo);

        /// <summary>Salva a imagem em wwwroot/uploads/eventos e retorna a URL relativa.</summary>
        Task<string> SalvarAsync(IFormFile arquivo);

        /// <summary>Exclui uma imagem enviada por upload (as imagens de demonstração não são apagadas).</summary>
        void Excluir(string? url);
    }

    public class ImagemService : IImagemService
    {
        private const string PastaUploads = "/uploads/eventos/";
        private const long TamanhoMaximo = 2 * 1024 * 1024; // 2 MB
        private static readonly string[] ExtensoesPermitidas = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly IWebHostEnvironment _ambiente;

        public ImagemService(IWebHostEnvironment ambiente)
        {
            _ambiente = ambiente;
        }

        public string? Validar(IFormFile? arquivo)
        {
            if (arquivo == null || arquivo.Length == 0)
            {
                return null;
            }

            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            if (!ExtensoesPermitidas.Contains(extensao) || !arquivo.ContentType.StartsWith("image/"))
            {
                return "Envie uma imagem nos formatos JPG, PNG, WEBP ou GIF.";
            }

            if (arquivo.Length > TamanhoMaximo)
            {
                return "A imagem deve ter no máximo 2 MB.";
            }

            return null;
        }

        public async Task<string> SalvarAsync(IFormFile arquivo)
        {
            var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
            var nomeArquivo = $"{Guid.NewGuid():N}{extensao}";

            var pasta = Path.Combine(_ambiente.WebRootPath, "uploads", "eventos");
            Directory.CreateDirectory(pasta);

            var caminho = Path.Combine(pasta, nomeArquivo);
            await using (var stream = new FileStream(caminho, FileMode.Create))
            {
                await arquivo.CopyToAsync(stream);
            }

            return PastaUploads + nomeArquivo;
        }

        public void Excluir(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith(PastaUploads))
            {
                return;
            }

            var nomeArquivo = Path.GetFileName(url);
            var caminho = Path.Combine(_ambiente.WebRootPath, "uploads", "eventos", nomeArquivo);
            if (File.Exists(caminho))
            {
                File.Delete(caminho);
            }
        }
    }
}
