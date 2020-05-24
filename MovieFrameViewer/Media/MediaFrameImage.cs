using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace MovieFrameViewer.Media
{
    public class MediaFrameImage
    {
        public int FrameNo { get; private set; }
        public BitmapSource Image { get; private set; }
        public string FileName { get; private set; }

        public MediaFrameImage(int frameNo, BitmapSource bmp, string fileName = null)
        {
            FrameNo = frameNo;
            Image = bmp;
            Image?.Freeze();
            FileName = fileName ?? frameNo.ToString();
        }
    }
}
