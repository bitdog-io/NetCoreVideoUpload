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
using System.Threading.Tasks;

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
            for (int i = 0; i < model.Count; i++)
            {
                // Check if the file currently exist in the upload folder
                if (!fileEntries[].Contains(model[i].FileName))
                {
                    _context.Remove(model[i]);
                    await _context.SaveChangesAsync();
                    model = await _context.Videos.ToListAsync();
                }
            }
            return View(model);
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
                var fileName = Path.GetFileName(model.UploadedFile.FileName);
                var extension = Path.GetExtension(fileName);
                var newFileName = string.Concat(Convert.ToString(Guid.NewGuid()), extension);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\UploadedVideos", newFileName);

                var videos = new Videos()
                {
                    Id = Guid.NewGuid(),
                    Description = model.Description,
                    Extension = extension,
                    UploadDate = DateTime.Now,
                    VideoTitle = model.VideoTitle,
                    FileName = newFileName,
                    VideoPath = uploadPath,
                };
                using (var stream = new FileStream(uploadPath, FileMode.Create))
                {
                    await model.UploadedFile.CopyToAsync(stream);
                }

                await _context.Videos.AddAsync(videos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }
}