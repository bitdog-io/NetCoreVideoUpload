using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace NetCoreVideoUpload.Utilities
{
    public class Converter
    {
        public async Task Run()
        {
            string uploadFolderPath = Environment.CurrentDirectory + "wwwroot\\UploadedVideos";
            Queue filesToConvert = new Queue((ICollection)GetFilesToConvert(uploadFolderPath));
            FFmpeg.SetExecutablesPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FFmpeg"));
            await RunConversion(filesToConvert);
            
        }
        private static IEnumerable GetFilesToConvert(string DirectoryPath)
        {
            return new DirectoryInfo(DirectoryPath).GetFiles().Where(x => x.Extension != ".mp4");
        }

        private static async Task RunConversion(Queue filesToConvert)
        {
            Console.WriteLine("Running conversion of files on: " + filesToConvert.Count + "files");
            while(filesToConvert.Count > 0)
            {
                FileInfo fileToConvert = (FileInfo)filesToConvert.Dequeue();
                string outputFileName = Path.ChangeExtension(fileToConvert.FullName, ".mp4");

                var conversion = Conversion.ToMp4(fileToConvert.FullName, outputFileName).SetOverwriteOutput(true);
                conversion.OnProgress += async (sender, args) =>
                {
                    await Console.Out.WriteLineAsync($"[{args.Duration}/{args.TotalLength}][{args.Percent}%] {fileToConvert.Name}");
                };

                await conversion.Start();
                //var startConversion = await FFmpeg.Conversions.FromSnippet.ToMp4(fileToConvert.FullName, outputFileName);
                //await startConversion.Start();
                await Console.Out.WriteLineAsync($"Finished converting file [{fileToConvert.Name}]");
            }
        }
    }
}
