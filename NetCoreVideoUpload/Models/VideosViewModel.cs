using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreVideoUpload.Models
{
    public class VideosViewModel
    {
        public Guid Id { get; set; }
        public string VideoTitle { get; set; }
        public string Description { get; set; }
        public string VideoPath { get; set; }
        public int? VideoLength { get; set; }
        public string Extension { get; set; }
        public DateTime UploadDate { get; set; }
        public Videos GetVideos { get; set; }

}

public class UploadVideosVM
    {
        [Required(ErrorMessage = "Please enter a title for the video you're uploading")]
        public string VideoTitle { get; set; }
        [Display(Name = "Please enter a video description ( Optional )")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Please select a file")]
        public IFormFile UploadedFile { get; set; }
    }

    public class ViewVideoVM
    {
        public string VideoTitle { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public string VideoLength { get; set; }
        public string Extension { get; set; }
        public DateTime UploadDate { get; set; }
        public Videos GetVideos { get; set; }

    }
}
