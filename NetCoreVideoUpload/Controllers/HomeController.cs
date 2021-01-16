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
    [RequireHttps]
    public class HomeController : Controller
    {
        private readonly VideoDbContext _context;w
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ILogger<HomeController> logger,
            VideoDbContext videoDbContext)
        {
            _logger = logger;
            _context = videoDbContext;
        }

        public async Task<IActionResult> Watch()
        {
            // Query the Database for all the videos stored
            List<Videos> videoList = await (from dbVideo in _context.Videos orderby dbVideo.UploadDate descending select dbVideo).ToListAsync();

            if (videoList.Count() > 1)
            {
                // Get the uploadpath/Directory
                string uploadPath = Path.GetDirectoryName(videoList.FirstOrDefault().VideoPath);
                // Get all the files stored in the uploadpath/directory
                string[] fileEntries = Directory.GetFiles(uploadPath);
                // Check if there is any files stored in the Database that is not stored physically
                var notStoredPhysical = fileEntries.Where(x => !videoList.Select(n => n.VideoPath).Contains(x));
                if (notStoredPhysical.Any())
                {
                    _logger.LogInformation("Trying to remove: " + notStoredPhysical.Count() + " files from physical drive");
                    foreach (var file in notStoredPhysical)
                    {
                        FileInfo fileCheck = new FileInfo(file);
                        if (fileCheck.Exists && fileCheck.LastAccessTime.AddMinutes(5) < DateTime.Now)
                        {
                            try
                            {
                                _logger.LogInformation("Removing: " + fileCheck.Name);
                                //fileCheck.Delete(); --- lets not do this yet :-)
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex.ToString());
                                throw;
                            }
                        }
                        else
                        {
                            _logger.LogError("Failed");
                        }
                    }
                }
                // Check if there is any files stored physically that is not in the database
                IEnumerable<Videos> notStored = videoList.Where(x => !fileEntries.Select(n => n).Contains(x.VideoPath));
                if (notStored.Any())
                {
                    Console.WriteLine("Removing: " + notStored.Count() + " items from the database.");
                    foreach (var file in notStored)
                    {
                        _logger.LogInformation("FileName: " + file.FileName);
                        _context.Videos.Remove(file);
                    }
                    try
                    {
                        _context.SaveChanges();
                        videoList = await _context.Videos.ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.ToString());
                        throw;
                    }
                }
            }
            return View(videoList);
        }

        //public async Task<IActionResult> StartDownload(Videos videoId)
        //{
        //    // Get filepath to the file that was requested
        //    Videos fileToDown = await _context.Videos.FirstOrDefaultAsync(x => x.Id.Equals(videoId.Id));
        //    // Create the FileName for the file that was requested
        //    string fileName = string.Concat(fileToDown.VideoTitle, fileToDown.Extension);
        //    byte[] data = new WebClient().DownloadData(fileToDown.VideoPath);
        //    Stream content = new MemoryStream(data);
        //    string contentType = "Video/mp4";
        //    // Return the file
        //    return File(content, contentType, fileName);
        //}

        //public async Task<IActionResult> StartConversion(string Start)
        //{
        //    // Get for files that need to be converted
        //    var FilesToConvert = _context.Videos.ToList().Where(x => x.Extension != ".mp4");
        //    if (FilesToConvert.Any())
        //    {
        //        Console.WriteLine("Starting conversion on: " + FilesToConvert.FirstOrDefault().FileName + " files");
        //        // Get Information about the file that is about to be converted
        //        var fileToConvert = FilesToConvert.FirstOrDefault();
        //        string oldStoragePath = fileToConvert.VideoPath;
        //        string oldFileName = Path.GetFileName(fileToConvert.FileName);
        //        string newFileName = Path.ChangeExtension(oldFileName, ".mp4").ToString();
        //        string outputPath = Path.ChangeExtension(fileToConvert.VideoPath, ".mp4");
        //        // Define Conversion parameters
        //        var conversion = Conversion.ToMp4(fileToConvert.VideoPath, outputPath).SetOverwriteOutput(true).UseMultiThread(2);
        //        conversion.OnProgress += (async (sender, args) =>
        //        {
        //            await Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {fileToConvert.FileName}");
        //        });
        //        // Launch Conversion
        //        await conversion.Start();
        //        // Update the database with information about the new file
        //        fileToConvert.FileName = newFileName;
        //        fileToConvert.VideoPath = outputPath;
        //        fileToConvert.Extension = Path.GetExtension(newFileName);
        //        _context.Videos.Update(fileToConvert);
        //        await _context.SaveChangesAsync();
        //        try
        //        {
        //            // Try to delete the old file that got converted
        //            FileInfo file = new FileInfo(Path.GetFullPath(oldStoragePath));
        //            file.Delete();
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex.ToString());
        //            throw;
        //        }
        //    }
        //    return RedirectToAction(nameof(Index));
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(UploadVideosVM model)
        {
            // Get if model contains any information/files
            if ( model?.UploadedFile != null && model.UploadedFile.Length > 0)
            {
                try
                {
                    // Get the original filename of the file uploaded
                    string fileName = Path.GetFileName(model.UploadedFile.FileName);
                    // Get the extension from the original fileName
                    string extension = Path.GetExtension(fileName);
                    // Create new filename as a Guid
                    string newFileName = string.Concat(Guid.NewGuid(), extension);
                    // Get the directory of where the files are to be stored and how the path should look like
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\UploadedVideos", newFileName);
                    // Create the files
                    using (var stream = new FileStream(uploadPath, FileMode.Create))
                    {
                        await model.UploadedFile.CopyToAsync(stream);
                    }
                    // Populate the model before it gets sent to the Database
                    var videos = new Videos()
                    {
                        Id = Guid.NewGuid(),
                        Description = model.Description,
                        Extension = extension,
                        UploadDate = DateTime.Now,
                        VideoTitle = fileName, //model.VideoTitle,
                        FileName = newFileName,
                        OldFileName = fileName,
                        VideoPath = uploadPath,
                    };
                    // Add and save the changes to the database
                    await _context.Videos.AddAsync(videos);
                    await _context.SaveChangesAsync();
                    return RedirectToActionPermanent(nameof(ThankYou));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.ToString());
                    throw;
                }
            }

            return View(model);
        }

        public IActionResult ThankYou()
        {
            return View();
        }

    }
}