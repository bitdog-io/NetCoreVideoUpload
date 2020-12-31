﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace NetCoreVideoUpload.Models
{
    public class Videos
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string VideoTitle { get; set; }
        public string FileName { get; set; }
        public string OldFileName { get; set; }
        public string Description { get; set; }
        [Required]
        public string VideoPath { get; set; }
        public int? VideoLength { get; set; }
        public string Extension { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
