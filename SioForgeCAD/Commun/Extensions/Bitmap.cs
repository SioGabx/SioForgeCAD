using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Commun.Extensions
{
    public static class BitmapExtensions
    {
        public static BitmapSource ToBitmapSource(this System.Drawing.Image Image)
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

        public static Image RotateImage(this Image image, double angleRadians)
        {
            angleRadians = (-angleRadians) % (2 * Math.PI);
            double sin = Math.Abs(Math.Sin(angleRadians));
            double cos = Math.Abs(Math.Cos(angleRadians));
            int newWidth = (int)Math.Round((image.Width * cos) + (image.Height * sin));
            int newHeight = (int)Math.Round((image.Width * sin) + (image.Height * cos));

            Bitmap rotatedImage = new Bitmap(newWidth, newHeight);

            using (Graphics g = Graphics.FromImage(rotatedImage))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.TranslateTransform(newWidth / 2, newHeight / 2);
                g.RotateTransform((float)(angleRadians * (180 / Math.PI)));
                g.DrawImage(image, new Rectangle(-image.Width / 2, -image.Height / 2, image.Width, image.Height));
            }
            return rotatedImage;
        }



    }
}
