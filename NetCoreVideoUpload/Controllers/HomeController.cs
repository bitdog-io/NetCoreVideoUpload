using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCoreVideoUpload.Data;
using NetCoreVideoUpload.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace NetCoreVideoUpload.Controllers
{
    public class HomeController : Controller
    {
        private readonly VideoDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger,
            VideoDbContext videoDbContext)
        {
            _logger = logger;
            _context = videoDbContext;
        }

        public async Task<IActionResult> Index()
        {
            List<Videos> model = await _context.Videos.ToListAsync();
            var uploadPath = Path.GetDirectoryName(model.FirstOrDefault().VideoPath);
            string[] fileEntries = Directory.GetFiles(uploadPath);
            var notStoredPhysical = fileEntries.Where(x => !model.Select(n => n.VideoPath).Contains(x));
            if (notStoredPhysical.Any())
            {
                Console.WriteLine("Trying to remove: " + notStoredPhysical.Count() + " files from physical drive");
                foreach (var file in notStoredPhysical)
                {
                    FileInfo fileCheck = new FileInfo(file);
                    if (fileCheck.Exists && fileCheck.LastAccessTime.AddMinutes(5) < DateTime.Now)
                    {
                        try
                        {
                            Console.WriteLine("Removing: " + fileCheck.Name);
                            fileCheck.Delete();
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed");
                    }
                }
            }

            IEnumerable<Videos> notStored = model.Where(x => !fileEntries.Select(n => n).Contains(x.VideoPath));
            if (notStored.Any())
            {
                Console.WriteLine("Removing: " + notStored.Count() + " items from the database.");
                foreach (var file in notStored)
                {
                    Console.WriteLine("FileName: " + file.FileName);
                    _context.Videos.Remove(file);
                }
                try
                {
                    _context.SaveChanges();
                    model = await _context.Videos.ToListAsync();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return View(model);
        }

        public async Task<IActionResult> StartDownload(Videos videoId)
        {
            var fileToDown = await _context.Videos.FirstOrDefaultAsync(x => x.Id.Equals(videoId.Id));
            string fileName = string.Concat(fileToDown.VideoTitle, fileToDown.Extension);
            var data = new WebClient().DownloadData(fileToDown.VideoPath);
            var content = new MemoryStream(data);
            var contentType = "Video/mp4";
            return File(content, contentType, fileName);
        }

        public async Task<IActionResult> StartConversion(string Start)
        {
            var FilesToConvert = _context.Videos.ToList().Where(x => x.Extension != ".mp4");
            if (FilesToConvert.Any())
            {
                Console.WriteLine("Starting conversion on: " + FilesToConvert.FirstOrDefault().FileName + " files");
                var fileToConvert = FilesToConvert.FirstOrDefault();
                string oldStoragePath = fileToConvert.VideoPath;
                string oldFileName = Path.GetFileName(fileToConvert.FileName);
                string newFileName = Path.ChangeExtension(oldFileName, ".mp4").ToString();
                string outputPath = Path.ChangeExtension(fileToConvert.VideoPath, ".mp4");
                var conversion = Conversion.ToMp4(fileToConvert.VideoPath, outputPath).SetOverwriteOutput(true).UseMultiThread(2);
                await Task.Run(() => conversion.OnProgress += async (sender, args) =>
               {
                   await Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {fileToConvert.FileName}");
               });
                fileToConvert.FileName = newFileName;
                fileToConvert.VideoPath = outputPath;
                fileToConvert.Extension = Path.GetExtension(newFileName);
                _context.Videos.Update(fileToConvert);
                await _context.SaveChangesAsync();
                Task<IConversionResult> task = Task.Run(async () => await conversion.Start());
                Console.WriteLine("Conversion took: " + task.Result.Duration);
                FileInfo file = new FileInfo(Path.GetFullPath(oldStoragePath));
                file.Delete();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(UploadVideosVM model)
        {
            if (model.UploadedFile != null && model.UploadedFile.Length > 0)
            {
                try
                {
                    var fileName = Path.GetFileName(model.UploadedFile.FileName);
                    var extension = Path.GetExtension(fileName);
                    var newFileName = string.Concat(Convert.ToString(Guid.NewGuid()), extension);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\UploadedVideos", newFileName);
                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await model.UploadedFile.CopyToAsync(stream);
                    }
                    var videos = new Videos()
                    {
                        Id = Guid.NewGuid(),
                        Description = model.Description,
                        Extension = extension,
                        UploadDate = DateTime.Now,
                        VideoTitle = model.VideoTitle,
                        FileName = newFileName,
                        OldFileName = fileName,
                        VideoPath = uploadPath,
                    };
                    await _context.Videos.AddAsync(videos);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
            return View(model);
        }
    }
}