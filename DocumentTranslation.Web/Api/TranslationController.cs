using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using DocumentTranslation.Web.Hubs;
using DocumentTranslationService.Core;

namespace DocumentTranslation.Web.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly ILogger<TranslationController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly DocumentTranslationService.Core.DocumentTranslationService _translationService;
        private readonly IHubContext<TranslationProgressHub> _hubContext;

        public TranslationController(
            ILogger<TranslationController> logger,
            IWebHostEnvironment environment,
            DocumentTranslationService.Core.DocumentTranslationService translationService,
            IHubContext<TranslationProgressHub> hubContext)
        {
            _logger = logger;
            _environment = environment;
            _translationService = translationService;
            _hubContext = hubContext;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> TranslateAsync(
            IFormFile file,
            string targetLanguage,
            string? sourceLanguage = null,
            string? connectionId = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            if (string.IsNullOrEmpty(targetLanguage))
            {
                return BadRequest("Target language is required");
            }

            try
            {
                var translationBusiness = new DocumentTranslationBusiness(_translationService);
                
                // Set up event handlers for real-time progress updates
                if (!string.IsNullOrEmpty(connectionId))
                {
                    translationBusiness.OnStatusUpdate += async (sender, status) =>
                    {
                        await _hubContext.Clients.Group($"translation_{connectionId}")
                            .SendAsync("StatusUpdate", new { 
                                message = status.Message ?? "Translating...",
                                status = status.Status?.Status.ToString() ?? "InProgress"
                            });
                    };

                    translationBusiness.OnUploadStart += async (sender, e) =>
                    {
                        await _hubContext.Clients.Group($"translation_{connectionId}")
                            .SendAsync("StatusUpdate", new { 
                                message = "Uploading file...",
                                status = "Uploading"
                            });
                    };

                    translationBusiness.OnUploadComplete += async (sender, e) =>
                    {
                        await _hubContext.Clients.Group($"translation_{connectionId}")
                            .SendAsync("StatusUpdate", new { 
                                message = "Upload complete. Starting translation...",
                                status = "Translating"
                            });
                    };

                    translationBusiness.OnDownloadComplete += async (sender, e) =>
                    {
                        await _hubContext.Clients.Group($"translation_{connectionId}")
                            .SendAsync("StatusUpdate", new { 
                                message = "Translation complete. Preparing download...",
                                status = "Completing"
                            });
                    };

                    translationBusiness.OnThereWereErrors += async (sender, errors) =>
                    {
                        await _hubContext.Clients.Group($"translation_{connectionId}")
                            .SendAsync("TranslationError", new { 
                                message = $"Translation error: {errors}",
                                error = errors
                            });
                    };
                }

                // Initialize the translation service
                await _translationService.InitializeAsync();

                // Create uploads directory
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Save uploaded file
                var fileName = Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create output directory
                var outputPath = Path.Combine(uploadsPath, "output");
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, true);
                }
                Directory.CreateDirectory(outputPath);

                // Run translation
                var filesToTranslate = new List<string> { filePath };
                var targetLanguages = new string[] { targetLanguage };
                var fromLanguage = string.IsNullOrEmpty(sourceLanguage) ? null : sourceLanguage;

                await translationBusiness.RunAsync(
                    filestotranslate: filesToTranslate,
                    fromlanguage: fromLanguage,
                    tolanguages: targetLanguages,
                    glossaryfiles: null,
                    targetFolder: outputPath);

                // Get list of translated files
                var translatedFiles = new List<string>();
                if (Directory.Exists(outputPath))
                {
                    var files = Directory.GetFiles(outputPath);
                    translatedFiles = files.Select(f => Path.GetFileName(f)).ToList();
                }

                // Notify completion
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Group($"translation_{connectionId}")
                        .SendAsync("TranslationComplete", new { 
                            message = "Translation completed successfully!",
                            files = translatedFiles
                        });
                }

                return Ok(new { 
                    success = true, 
                    message = "Translation completed successfully!",
                    files = translatedFiles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during translation");
                
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Group($"translation_{connectionId}")
                        .SendAsync("TranslationError", new { 
                            message = $"Translation failed: {ex.Message}",
                            error = ex.Message
                        });
                }
                
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Translation failed: {ex.Message}" 
                });
            }
        }
    }
}