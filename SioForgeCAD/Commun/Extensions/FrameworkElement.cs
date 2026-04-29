using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SioForgeCAD.Commun.Extensions
{
    public static class FrameworkElementExtensions
    {
        public static BitmapSource CreateElementSnapshot(this FrameworkElement element)
        {
            if (element == null || element.ActualWidth <= 0 || element.ActualHeight <= 0)
            {
                return null;
            }

            element.UpdateLayout();
            int width = (int)Math.Ceiling(element.ActualWidth);
            int height = (int)Math.Ceiling(element.ActualHeight);
            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Pbgra32);
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                VisualBrush visualBrush = new VisualBrush(element);
                drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(0, 0), new Size(width, height)));
            }
            rtb.Render(drawingVisual);
            rtb.Freeze();

            return rtb;
        }

        public static void ResetAllDataContexts(this DependencyObject target)
        {
            if (target is FrameworkElement fe)
            {
                fe.DataContext = null;
            }

            // Parcourir les enfants dans l'arbre visuel
            int count = VisualTreeHelper.GetChildrenCount(target);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(target, i);
                ResetAllDataContexts(child);
            }
        }

        public static IEnumerable<T> GetVisualChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                // Search deeper in the tree
                foreach (T childOfChild in GetVisualChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        public static T GetVisualChild<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                // Recursive call to search deeper
                T result = GetVisualChild<T>(child);
                if (result != null) return result;
            }

            return null;
        }

        public static void SetAllDataContexts(this DependencyObject target, object Context)
        {
            if (target is FrameworkElement fe)
            {
                fe.DataContext = Context;
            }

            // Parcourir les enfants dans l'arbre visuel
            int count = VisualTreeHelper.GetChildrenCount(target);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(target, i);
                SetAllDataContexts(child, Context);
            }
        }


        public static Cursor CreateCursorFromElement(this FrameworkElement element, Point hotspot)
        {
            element.UpdateLayout();

            // Les curseurs Windows ne peuvent pas dépasser 255 pixels
            double scale = 1.0;
            if (element.ActualWidth > 255 || element.ActualHeight > 255)
            {
                scale = 255.0 / Math.Max(element.ActualWidth, element.ActualHeight);
            }

            int width = (int)Math.Min(Math.Max(1, element.ActualWidth * scale), 255);
            int height = (int)Math.Min(Math.Max(1, element.ActualHeight * scale), 255);

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.PushOpacity(0.70);
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.DrawRectangle(new VisualBrush(element), null, new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            }
            rtb.Render(dv);

            // encode en PNG
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using (MemoryStream msPng = new MemoryStream())
            {
                encoder.Save(msPng);
                byte[] pngBytes = msPng.ToArray();

                //On fabrique le curseur avec le HOTSPOT !
                using (MemoryStream msCursor = new MemoryStream())
                {
                    BinaryWriter bw = new BinaryWriter(msCursor);

                    // Header ICONDIR
                    bw.Write((short)0); // Réservé
                    bw.Write((short)2); // Type: 2 = CUR
                    bw.Write((short)1); // Nombre d'images

                    // ICONDIRENTRY
                    bw.Write((byte)width);
                    bw.Write((byte)height);
                    bw.Write((byte)0);  // Couleurs
                    bw.Write((byte)0);  // Réservé

                    // On définit le point de clic (Hotspot) en fonction de là où la souris a cliqué
                    bw.Write((short)Math.Round(hotspot.X * scale)); // Hotspot X
                    bw.Write((short)Math.Round(hotspot.Y * scale)); // Hotspot Y
                    bw.Write(pngBytes.Length); // Taille des données
                    bw.Write(22); // Décalage vers les données de l'image
                    // Les pixels
                    bw.Write(pngBytes);

                    msCursor.Position = 0;
                    return new Cursor(msCursor);
                }
            }
        }
    }
}
