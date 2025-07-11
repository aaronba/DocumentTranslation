using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;

namespace DocumentTranslation.Web.Pages
{
    public class TranslateModel : PageModel
    {
        private readonly ILogger<TranslateModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public TranslateModel(ILogger<TranslateModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        [BindProperty]
        public IFormFile? UploadedFile { get; set; }

        [BindProperty]
        public string TargetLanguage { get; set; } = "fr";

        [BindProperty]
        public string SourceLanguage { get; set; } = "";

        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsTranslating { get; set; }
        public string? DownloadPath { get; set; }

        public List<LanguageOption> AvailableLanguages { get; set; } = new()
        {
            new("fr", "French"),
            new("es", "Spanish"),
            new("de", "German"),
            new("it", "Italian"),
            new("pt", "Portuguese"),
            new("zh", "Chinese (Simplified)"),
            new("ja", "Japanese"),
            new("ko", "Korean"),
            new("ru", "Russian"),
            new("ar", "Arabic")
        };

        public void OnGet()
        {
            // Page load
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ErrorMessage = "Please select a file to upload.";
                return Page();
            }

            if (string.IsNullOrEmpty(TargetLanguage))
            {
                ErrorMessage = "Please select a target language.";
                return Page();
            }

            try
            {
                // Create uploads directory
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Save uploaded file
                var fileName = Path.GetFileName(UploadedFile.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await UploadedFile.CopyToAsync(stream);
                }

                // Create output directory
                var outputPath = Path.Combine(uploadsPath, "output");
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                // Run translation
                var success = await RunTranslationAsync(filePath, outputPath, TargetLanguage, SourceLanguage);
                
                if (success)
                {
                    Message = $"Translation completed successfully! File translated to {TargetLanguage}.";
                    DownloadPath = $"/uploads/output";
                }
                else
                {
                    ErrorMessage = "Translation failed. Please check your configuration and try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation");
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            return Page();
        }

        private async Task<bool> RunTranslationAsync(string inputFile, string outputDir, string targetLang, string sourceLang)
        {
            try
            {
                // Path to the CLI executable
                var cliPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DocumentTranslation.CLI", "bin", "Debug", "net8.0", "doctr");
                
                // Build command arguments
                var args = $"translate \"{inputFile}\" \"{outputDir}\" --to {targetLang}";
                if (!string.IsNullOrEmpty(sourceLang))
                {
                    args += $" --from {sourceLang}";
                }

                _logger.LogInformation($"Running command: {cliPath} {args}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(cliPath)
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    _logger.LogError("Failed to start translation process");
                    return false;
                }

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                _logger.LogInformation($"Translation output: {output}");
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogWarning($"Translation error: {error}");
                }

                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run translation command");
                return false;
            }
        }
    }

    public record LanguageOption(string Code, string Name);
}
