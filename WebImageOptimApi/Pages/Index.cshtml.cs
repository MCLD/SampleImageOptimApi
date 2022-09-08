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
        public IFormFile? FormFile { get; set; }

        [BindProperty]
        public string? Username { get; set; }

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

            string filename = Path.Combine(Path.GetTempPath(), FormFile.FileName);
            using var stream = new FileStream(filename, FileMode.Create);
            OptimizedImageResult optimized;
            try
            {
                await FormFile.CopyToAsync(stream);
                stream.Close();
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
                    var provider = new FileExtensionContentTypeProvider();
                    provider.TryGetContentType(FormFile.FileName, out var contentType);
                    return File(optimized.File, contentType ?? "application/octet-stream", FormFile.FileName);
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
    }
}