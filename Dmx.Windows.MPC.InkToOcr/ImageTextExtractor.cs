using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;

namespace Dmx.Win.MPC.InkToOcr
{
    public static class OfflineImageTextExtractor
    {        
        public static async Task<string> ExtractTextFromImageAsync(StorageFile file, OcrEngine ocrEngine)
        {
            using (var stream = await file.OpenAsync(FileAccessMode.Read))
            {
                // Create image decoder.
                var decoder = await BitmapDecoder.CreateAsync(stream);

                // Load bitmap.
                var bitmap = await decoder.GetSoftwareBitmapAsync();
                
                // Extract text from image.
                OcrResult result = await ocrEngine.RecognizeAsync(bitmap);

                // Display recognized text.
                var res = result.Text;
                return res;
            }
        }
    }
}
