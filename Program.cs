using System.Drawing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var config = new ConfigurationBuilder()
        .AddJsonFile("config.json")
        .Build();

var aspectRatios = config.GetSection("AspectRatios")
    .Get<List<Size>>();

var imageFiles = Directory.EnumerateFiles("./inputs", "*.*", SearchOption.AllDirectories)
    .Where(file => file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("png")); 

string outputDirectory = "./outputs";
Directory.CreateDirectory(outputDirectory);

string logFilePath = Path.Combine(outputDirectory, "resize_log.csv");
using StreamWriter logFile = new StreamWriter(logFilePath);

await logFile.WriteLineAsync("Old File Name,New File Name,Old Width,Old Height,New Width,New Height,Old Aspect Ratio,New Aspect Ratio");

await Task.WhenAll(imageFiles.Select(file => Task.Run(async () =>
{
    try
    {
        using (var originalImage = Image.FromFile(file))
        {
            var nearestAspectRatio = aspectRatios
                .OrderBy(ratio => Math.Abs((double)originalImage.Width / originalImage.Height - (double)ratio.Width / ratio.Height))
                .First();
            var newWidth = originalImage.Height * nearestAspectRatio.Width / nearestAspectRatio.Height;
            var newHeight = originalImage.Width * nearestAspectRatio.Height / nearestAspectRatio.Width;
            var newSize = originalImage.Width > originalImage.Height ? 
                new Size(newWidth, originalImage.Height) : 
                new Size(originalImage.Width, newHeight);

            var newFilePath = Path.Combine(outputDirectory,
                Path.GetFileNameWithoutExtension(file) + "_resized" + Path.GetExtension(file));

            using (var newImage = new Bitmap(originalImage, newSize))
            {
                newImage.Save(newFilePath, originalImage.RawFormat);
            }

            string oldAspectRatio = $"{originalImage.Width}:{originalImage.Height}";
            string newAspectRatio = $"{newSize.Width}:{newSize.Height}";

            await logFile.WriteLineAsync($"{file},{newFilePath},{originalImage.Width},{originalImage.Height},{newSize.Width},{newSize.Height},{oldAspectRatio},{newAspectRatio}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {file}. Error: {ex.Message}");
    }
})));
    
Console.WriteLine("Image resizing operation has been completed.");