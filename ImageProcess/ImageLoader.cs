using ImageMagick;
using System;
using System.IO;

namespace ImageProcess
{
    public struct ImageInfo
    {
        public int Width;
        public int Height;

        public int DataSize => Width * 3 * Height;
    }
    public class ImageLoader
    {
        public static ImageInfo LoadInfo(string filePath)
        {
            MagickFormat loadFormat = GetFormat(filePath);
            ImageInfo info = new ImageInfo();
            try
            {
                using (MagickImage image = new MagickImage())
                {
                    var magicReadSettings = new MagickReadSettings
                    {
                        Format = loadFormat,
                        ColorSpace = ColorSpace.sRGB
                    };

                    image.Read(filePath, magicReadSettings);
                    info.Width = image.Width;
                    info.Height = image.Height;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return info;
        }

        public static void Load(string filePath, MemoryStream ms)
        {
            MagickFormat loadFormat = GetFormat(filePath); ;
            try
            {
                using (MagickImage image = new MagickImage())
                {
                    var magicReadSettings = new MagickReadSettings
                    {
                        Format = loadFormat,
                        ColorSpace = ColorSpace.sRGB
                    };

                    image.Read(filePath, magicReadSettings);
                    //image.Quality = 100;
                    //image.TransformColorSpace(ColorProfile.SRGB, ColorProfile.AdobeRGB1998);
                    //image.FilterType = FilterType.Sinc;
                    image.Depth = 8;
                    image.Write(ms, MagickFormat.Bgr);
                    ms.Position = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static MagickFormat GetFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToUpper();
            switch (ext)
            {
                case ".DNG": return MagickFormat.Dng;
                default: return MagickFormat.Jpg;
            }
        }
    }
}
