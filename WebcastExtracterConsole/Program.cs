using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;


namespace WebcastExtracterConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = "Iridium-1 Technical Webcast.mp4";
            var directoryPath = @"C:\Users\agelo\Downloads\";
            var startTime = new TimeSpan(0, 19, 30);

            var filePath = Path.Combine(directoryPath, fileName);

            var inputFile = new MediaFile { Filename = filePath };
            var outputFile = new MediaFile {Filename = "temp.bmp"};
            
            //var ocrEngine = new OcrEngine(OcrLanguage.English);

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);

                var frameRate = inputFile.Metadata.VideoData.Fps;
                var duration = inputFile.Metadata.Duration;
                var frameDuration = 1.0 / frameRate;
                var frameCount = (int)(frameRate * duration.TotalSeconds);

                var startingFrame = (int) (frameRate * startTime.TotalSeconds);

                for (int frameNumber = startingFrame; frameNumber < frameCount; frameNumber++)
                {
                    // Saves the frame located on the 15th second of the video.
                    var options = new ConversionOptions {Seek = TimeSpan.FromSeconds(frameCount * frameDuration)};
                    engine.GetThumbnail(inputFile, outputFile, options);

                    // This main API call to extract text from image.
                    //var ocrResult = await ocrEngine.RecognizeAsync((uint) bitmap.PixelHeight, (uint) bitmap.PixelWidth,bitmap.PixelBuffer.ToArray());

                    //// OCR result does not contain any lines, no text was recognized. 
                    //if (ocrResult.Lines != null)
                    //{
                    //}
                }
            }
        }
    }
}
