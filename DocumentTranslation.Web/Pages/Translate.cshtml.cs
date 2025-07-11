using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;

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
        public List<string> TranslatedFiles { get; set; } = new();

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
                // First, test if CLI is accessible and configured
                var configTestResult = await TestCliConfigurationAsync();
                if (!configTestResult.Success)
                {
                    ErrorMessage = $"CLI Configuration Error: {configTestResult.Error}";
                    return Page();
                }

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
                var result = await RunTranslationAsync(filePath, outputPath, TargetLanguage, SourceLanguage);
                
                if (result.Success)
                {
                    Message = $"Translation completed successfully! File translated to {TargetLanguage}.";
                    
                    // Get list of translated files
                    if (Directory.Exists(outputPath))
                    {
                        var files = Directory.GetFiles(outputPath);
                        TranslatedFiles = files.Select(f => Path.GetFileName(f)).ToList();
                    }
                }
                else
                {
                    ErrorMessage = $"Translation failed: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation");
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            return Page();
        }

        private async Task<(bool Success, string Error)> TestCliConfigurationAsync()
        {
            try
            {
                var wrapperPath = Path.Combine(Directory.GetCurrentDirectory(), "run-doctr.py");
                
                if (!System.IO.File.Exists(wrapperPath))
                {
                    return (false, $"CLI wrapper script not found at: {wrapperPath}");
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = $"{wrapperPath} config test",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    return (false, "Failed to start CLI process");
                }

                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                try
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await process.WaitForExitAsync(timeoutCts.Token);

                    if (process.ExitCode == 0)
                    {
                        return (true, string.Empty);
                    }
                    else
                    {
                        var errorMsg = !string.IsNullOrEmpty(error) ? error : output;
                        return (false, $"Configuration test failed: {errorMsg}");
                    }
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(true); } catch { }
                    return (false, "Configuration test timed out");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error testing configuration: {ex.Message}");
            }
        }

        private async Task<(bool Success, string Error)> RunTranslationAsync(string inputFile, string outputDir, string targetLang, string sourceLang)
        {
            try
            {
                // Use the Python wrapper script instead of calling doctr directly
                var wrapperPath = Path.Combine(Directory.GetCurrentDirectory(), "run-doctr.py");
                
                // Build command arguments
                var args = $"{wrapperPath} translate \"{inputFile}\" \"{outputDir}\" --to {targetLang}";
                if (!string.IsNullOrEmpty(sourceLang))
                {
                    args += $" --from {sourceLang}";
                }

                _logger.LogInformation($"Running command: python3 {args}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Directory.GetCurrentDirectory()
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    _logger.LogError("Failed to start translation process");
                    return (false, "Failed to start translation process");
                }

                // Set a timeout for the process (e.g., 10 minutes)
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                
                try
                {
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    // Wait for process completion with timeout
                    await process.WaitForExitAsync(timeoutCts.Token);
                    
                    var output = await outputTask;
                    var error = await errorTask;

                    _logger.LogInformation($"Translation output: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"Translation error: {error}");
                    }
                    
                    // Check if the process completed successfully
                    if (process.ExitCode == 0)
                    {
                        // Verify that output files were created
                        if (Directory.Exists(outputDir) && Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories).Length > 0)
                        {
                            return (true, string.Empty);
                        }
                        else
                        {
                            return (false, "Translation completed but no output files were generated");
                        }
                    }
                    else
                    {
                        // Process failed - extract meaningful error message
                        string errorMsg = "Translation failed";
                        
                        if (!string.IsNullOrEmpty(error))
                        {
                            errorMsg = error.Trim();
                        }
                        else if (!string.IsNullOrEmpty(output) && output.Contains("FAIL"))
                        {
                            errorMsg = output.Trim();
                        }
                        
                        _logger.LogWarning($"Translation failed with exit code {process.ExitCode}: {errorMsg}");
                        return (false, errorMsg);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Translation process timed out");
                    try { process.Kill(true); } catch { }
                    return (false, "Translation process timed out");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run translation command");
                return (false, $"Failed to run translation command: {ex.Message}");
            }
        }

        public async Task<IActionResult> OnGetDownloadAsync(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return NotFound();
            }

            var outputPath = Path.Combine(_environment.WebRootPath, "uploads", "output");
            var filePath = Path.Combine(outputPath, fileName);

            // Security check - ensure the file is within the output directory
            var fullPath = Path.GetFullPath(filePath);
            var fullOutputPath = Path.GetFullPath(outputPath);
            
            if (!fullPath.StartsWith(fullOutputPath))
            {
                return NotFound();
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var contentType = GetContentType(fileName);
            return File(memory, contentType, fileName);
        }

        public async Task<IActionResult> OnGetDownloadAllAsync()
        {
            var outputPath = Path.Combine(_environment.WebRootPath, "uploads", "output");
            
            if (!Directory.Exists(outputPath))
            {
                return NotFound();
            }

            var files = Directory.GetFiles(outputPath);
            if (files.Length == 0)
            {
                return NotFound();
            }

            // If there's only one file, download it directly
            if (files.Length == 1)
            {
                var fileName = Path.GetFileName(files[0]);
                return await OnGetDownloadAsync(fileName);
            }

            // If multiple files, create a zip archive
            var zipStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var entry = archive.CreateEntry(fileName);
                    
                    using var entryStream = entry.Open();
                    using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            zipStream.Position = 0;
            return File(zipStream, "application/zip", "translated_files.zip");
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".txt" => "text/plain",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".md" => "text/markdown",
                _ => "application/octet-stream"
            };
        }
    }

    public record LanguageOption(string Code, string Name);
}
