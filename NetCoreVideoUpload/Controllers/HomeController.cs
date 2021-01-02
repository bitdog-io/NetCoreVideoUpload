using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCoreVideoUpload.Data;
using NetCoreVideoUpload.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\UploadedVideos");
            string[] fileEntries = Directory.GetFiles(uploadPath);
            IEnumerable<Videos> notStored = model.Where(x => !fileEntries.Select(n => n).Contains(x.VideoPath));
            var notStoredPhysical = fileEntries.Where(x => !model.Select(n => n.VideoPath).Contains(x));
            if (notStoredPhysical.Count() > 0)
            {
                Console.WriteLine("Removing: " + notStoredPhysical.Count() + " files from physical drive");
                foreach (var file in notStoredPhysical)
                {
                    FileInfo fileCheck = new FileInfo(file);
                    if (fileCheck.Exists)
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
                }
            }

            if (notStored.Count() > 0)
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

        public async Task<IActionResult> StartConversion(string Start)
        {
            var FilesToConvert = _context.Videos.ToList().Where(x => x.Extension != ".mp4");
            if (FilesToConvert.Any())
            {
                Console.WriteLine("Starting conversion on: " + FilesToConvert.FirstOrDefault().FileName + " files");
                var fileToConvert = FilesToConvert.FirstOrDefault();
                string oldFileName = Path.GetFileName(fileToConvert.FileName);
                string newFileName = Path.ChangeExtension(oldFileName, ".mp4").ToString();
                string outputPath = Path.ChangeExtension(fileToConvert.VideoPath, ".mp4");
                var conversion = Conversion.ToMp4(fileToConvert.VideoPath, outputPath).SetOverwriteOutput(true).UseMultiThread(2);
                conversion.OnProgress += async (sender, args) =>
                {
                    await Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {fileToConvert.FileName}");
                };
                await conversion.Start();
                await Console.Out.WriteLineAsync($"Finished converting file [{oldFileName}] to [{newFileName}]");
                try
                {
                    fileToConvert.FileName = newFileName;
                    fileToConvert.VideoPath = outputPath;
                    fileToConvert.Extension = Path.GetExtension(newFileName);
                    _context.Videos.Update(fileToConvert);
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {

                    throw;
                }

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