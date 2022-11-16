using ImageOptimApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.StaticFiles;

namespace WebImageOptimApi.Pages
{
    public class IndexModel : PageModel
    {
        private readonly Client _client;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger, Client client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [BindProperty]
        public Format? FileFormat { get; set; }

        [BindProperty]
        public IFormFile? FormFile { get; set; }

        [BindProperty]
        public int? Height { get; set; }

        [BindProperty]
        public string? Username { get; set; }

        [BindProperty]
        public int? Width { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPost()
        {
            if (FormFile == null)
            {
                _logger.LogError("No uploaded image supplied.");
                return StatusCode(StatusCodes.Status500InternalServerError, "No uploaded image supplied.");
            }

            string filename = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(Path.GetTempFileName())
                + Path.GetExtension(FormFile.FileName));

            OptimizedImageResult optimized;
            try
            {
                using var stream = new FileStream(filename, FileMode.Create);
                await FormFile.CopyToAsync(stream);
                stream.Close();
                if (FileFormat.HasValue)
                {
                    _client.Format = FileFormat.Value;
                }
                if (Width > 0)
                {
                    _client.Width = Width.Value;
                }
                if (Height > 0)
                {
                    _client.Height = Height.Value;
                }
                _client.Username = Username;
                optimized = await _client.OptimizeAsync(filename);
                _logger.LogInformation("Image optimization took {ElapsedSeconds}s", optimized.ElapsedSeconds);
            }
            catch (ParameterException pex)
            {
                _logger.LogError("Error with image submission: ", pex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error with image submission: {pex.Message}");
            }
            finally
            {
                System.IO.File.Delete(filename);
            }
            if (optimized != null)
            {
                if (optimized.Status == Status.Success)
                {
                    string newFilename = _client.Format == Format.Auto
                        ? FormFile.FileName
                        : Path.GetFileNameWithoutExtension(FormFile.FileName)
                            + GetExtension(_client.Format);
                    var provider = new FileExtensionContentTypeProvider();
                    provider.TryGetContentType(newFilename, out var contentType);
                    return File(optimized.File, contentType ?? "application/octet-stream", newFilename);
                }
                else
                {
                    _logger.LogError("Problem optimizing image: {ErrorStatus} {ErrorMessage}", optimized.Status, optimized.StatusMessage);
                    return StatusCode(StatusCodes.Status500InternalServerError, $"{optimized.Status}: {optimized.StatusMessage}");
                }
            }
            else
            {
                _logger.LogError("No optimized image returned.");
                return StatusCode(StatusCodes.Status500InternalServerError, "No optimized image returned.");
            }
        }

        private static string GetExtension(Format format) => format switch
        {
            Format.Png => ".png",
            Format.Jpeg => ".jpg",
            Format.WebM => ".webm",
            Format.H264 => ".h264",
            _ => string.Empty,
        };
    }
}
