using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

public class ImageProcessingController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessImages(List<IFormFile> imageFiles)
    {
        var processedImagePaths = new List<string>();
        try
        {
            if (imageFiles != null && imageFiles.Count > 0)
            {
                foreach (var imageFile in imageFiles)
                {
                    using (var stream = imageFile.OpenReadStream())
                    {
                        using (var image = Image.Load(stream))
                        {
                            var resizeTask = ResizeImageAsync(image);
                            var filterTask = ApplyFiltersAsync(image);
                            await Task.WhenAll(resizeTask, filterTask);

                            var processedImagePath = Path.Combine("wwwroot", "images", imageFile.FileName);
                            image.Save(processedImagePath);
                            processedImagePaths.Add(imageFile.FileName);
                        }
                    }
                }
            }

            var jsonResult = new JsonResult(new { ProcessedImagePaths = processedImagePaths });
            return jsonResult;
        }
        catch (Exception ex)
        {
            // Redirect to the "Index" action in case of an error
            return RedirectToAction("Index", new { error = ex.Message });
        }
    }



    [HttpGet]
    public IActionResult DownloadProcessedImages(List<string> imageNames)
    {
        try
        {
            // Create a zip file to store the processed images
            var zipFileName = $"processed_images_{DateTime.Now:yyyyMMddHHmmss}.zip";
            var zipFilePath = Path.Combine("wwwroot", "downloads", zipFileName);

            using (var zipFileStream = new FileStream(zipFilePath, FileMode.Create))
            using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
            {
                foreach (var imageName in imageNames)
                {
                    var imagePath = Path.Combine("wwwroot", "images", imageName);

                    if (!System.IO.File.Exists(imagePath))
                    {
                        return NotFound(); // Or handle the situation accordingly
                    }

                    var entry = archive.CreateEntry(imageName);
                    using (var entryStream = entry.Open())
                    using (var fileStream = System.IO.File.OpenRead(imagePath))
                    {
                        fileStream.CopyTo(entryStream);
                    }
                }
            }

            var fileBytes = System.IO.File.ReadAllBytes(zipFilePath);
            return File(fileBytes, "application/zip", zipFileName);
        }
        catch (Exception ex)
        {
            // Handle exceptions accordingly
            return RedirectToAction("Index", new { error = ex.Message });
        }
    }


    private async Task ResizeImageAsync(Image image)
    {
        await Task.Run(() =>
        {
            image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(500, 500) }));
        });
    }

    private async Task ApplyFiltersAsync(Image image)
    {
        await Task.Run(() =>
        {
            image.Mutate(x => x.Grayscale());
        });
    }

    private async Task AdjustBrightnessContrastAsync(Image image)
    {
        await Task.Run(() =>
        {
            image.Mutate(x => x.Contrast(1.2f));
        });
    }
}
