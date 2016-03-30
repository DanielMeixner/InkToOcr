using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;

namespace Dmx.Win.MPC.InkToOcr
{
    public static class ImageCropper
    {
        // crops part of image into new file. 
        async public static Task<StorageFile> SaveCroppedBitmapAsync(StorageFile originalImgFile, StorageFile newImgFile, Point startPoint, Size size, Size workingImageSize)
        {
            // Convert start point and size to integer.
            uint startPointX = (uint)Math.Floor(startPoint.X);
            uint startPointY = (uint)Math.Floor(startPoint.Y);
            uint height = (uint)Math.Floor(size.Height);
            uint width = (uint)Math.Floor(size.Width);

            using (IRandomAccessStream originalImgFileStream = await originalImgFile.OpenReadAsync())
            {
                BitmapDecoder decoder1 = await BitmapDecoder.CreateAsync(originalImgFileStream);

                //normalize for image size
                var hfac = workingImageSize.Height / decoder1.PixelHeight;
                var wfac = workingImageSize.Width / decoder1.PixelWidth;

                startPointX = (uint) Math.Floor(startPointX / wfac);
                startPointY = (uint)Math.Floor(startPointY / hfac);
                height = (uint)Math.Floor(height / hfac);
                width = (uint)Math.Floor(width / wfac);

                // Refine the start point and the size. 
                if (startPointX + width > decoder1.PixelWidth)
                {
                    width = decoder1.PixelWidth - startPointX;
                }

                if (startPointY + height > decoder1.PixelHeight)
                {
                    height = decoder1.PixelHeight - startPointY;
                }

                // Create cropping BitmapTransform to define the bounds.
                BitmapTransform transform = new BitmapTransform();
                BitmapBounds bounds = new BitmapBounds();
                bounds.X = startPointX;
                bounds.Y = startPointY;
                bounds.Height = height;
                bounds.Width = width;
                transform.Bounds = bounds;

                // Get the cropped pixels within the the bounds of transform.
                PixelDataProvider pix = await decoder1.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);
                byte[] pixels = pix.DetachPixelData();

                using (IRandomAccessStream newImgFileStream = await newImgFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    Guid encoderID = Guid.Empty;

                    switch (newImgFile.FileType.ToLower())
                    {
                        case ".png":
                            encoderID = BitmapEncoder.PngEncoderId;
                            break;
                        case ".bmp":
                            encoderID = BitmapEncoder.BmpEncoderId;
                            break;
                        default:
                            encoderID = BitmapEncoder.JpegEncoderId;
                            break;
                    }

                    // Create a bitmap encoder
                    BitmapEncoder bmpEncoder = await BitmapEncoder.CreateAsync(
                        encoderID,
                        newImgFileStream);

                    // Set the pixel data to the cropped image.
                    bmpEncoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Straight,
                        width,
                        height,
                        decoder1.DpiX,
                        decoder1.DpiY,
                        pixels);

                    // Flush the data to file.
                    await bmpEncoder.FlushAsync();
                }
                return newImgFile;
            }
        }

        public static void FindBounderiesOfStroke(InkStroke stroke, ref double smallestX, ref double smallestY, ref double largestX, ref double largestY)
        {
            foreach (var item in stroke.GetInkPoints())
            {
                smallestX = (item.Position.X < smallestX) ? item.Position.X : smallestX;
                smallestY = (item.Position.Y < smallestY) ? item.Position.Y : smallestY;

                largestX = (item.Position.X > largestX) ? item.Position.X : largestX;
                largestY = (item.Position.Y > largestY) ? item.Position.Y : largestY;
            }
        }
    }
}

