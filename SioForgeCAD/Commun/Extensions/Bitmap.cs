using SioForgeCAD.Commun.Mist;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Commun.Extensions
{
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this Image Image)
        {
            using (var ms = new MemoryStream())
            {
                Image.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        public static Image RotateImage(this Image image, double angleRadians, System.Drawing.Color BackgroundColor)
        {
            angleRadians = (-angleRadians) % (2 * Math.PI);
            double sin = Math.Abs(Math.Sin(angleRadians));
            double cos = Math.Abs(Math.Cos(angleRadians));
            int newWidth = (int)Math.Round((image.Width * cos) + (image.Height * sin));
            int newHeight = (int)Math.Round((image.Width * sin) + (image.Height * cos));

            Bitmap rotatedImage = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.Clear(BackgroundColor);
                g.TranslateTransform(newWidth / 2, newHeight / 2);
                g.RotateTransform((float)(angleRadians * (180 / Math.PI)));
                g.DrawImage(image, new Rectangle(-image.Width / 2, -image.Height / 2, image.Width, image.Height));
            }
            return rotatedImage;
        }


        public static string GetImageFileSize(this Image image)
        {
            long jpegByteSize;
            using (var ms = new MemoryStream()) // estimatedLength can be original fileLength
            {
                image.Save(ms, ImageFormat.Jpeg); // save image to stream in Jpeg format
                jpegByteSize = ms.Length;
            }
            return Files.FormatFileSizeFromByte(jpegByteSize, 2);
        }
    }
}
