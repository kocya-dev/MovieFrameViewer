using ImageProcess;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MovieFrameViewer.Media
{
    public class BitmapSourceUtil
    {
        [System.Security.SuppressUnmanagedCodeSecurity]
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);

        public static BitmapSource FromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(handle);
            }
        }
        public static BitmapSource FromMat(Mat image, System.Windows.Media.PixelFormat format)
        {
            return BitmapSource.Create(image.Width, image.Height, 96, 96, format, null, image.Data, image.Width * 3 * image.Height, image.Width * 3);
        }
        public static BitmapSource FromMs(MemoryStream ms, int width, int height, System.Windows.Media.PixelFormat format)
        {
            using (var pinner = new ScopedPinner(ms.GetBuffer()))
            {
                return BitmapSource.Create(width, height, 96, 96, format, null, pinner.GetPtr(), width * 3 * height, width * 3);
            }
        }
        public static void CopyFromBitmap(WriteableBitmap source, Bitmap bmp)
        {
            BitmapData data = bmp.LockBits(new Rectangle(new System.Drawing.Point(0, 0), bmp.Size), ImageLockMode.ReadOnly, bmp.PixelFormat);
            source.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), data.Scan0, data.Stride * data.Height, data.Stride);
            bmp.UnlockBits(data);
        }
        public static void CopyFromMat(WriteableBitmap source, Mat image)
        {
            source.WritePixels(new Int32Rect(0, 0, image.Width, image.Height), image.Data, image.Width * 3 * image.Height, image.Width * 3);
        }
        public static void CopyFromMs(WriteableBitmap source, MemoryStream ms, int width, int height)
        {
            using (var pinner = new ScopedPinner(ms.GetBuffer()))
            {
                source.WritePixels(new Int32Rect(0, 0, width, height), pinner.GetPtr(), width * 3 * height, width * 3);
            }
        }
    }
}
