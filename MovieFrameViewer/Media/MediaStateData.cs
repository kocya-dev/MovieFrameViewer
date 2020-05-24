using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MovieFrameViewer.Media
{
    public class MediaStateData
    {
        public event EventHandler<ValueChangedEventArgs<MediaPlayState>> StateChanged;
        
        private MediaPlayState _state = MediaPlayState.None;
        private Dispatcher _dispatcher;
        private WriteableBitmap _canvas;
        public WriteableBitmap Canvas { get => _canvas; set => _canvas = value; }

        public bool IsPlaying => State == MediaPlayState.Play;
        public MediaPlayState State
        {
            get { return _state; }
            set
            {
                if (_state == value) return;
                var prev = _state;
                _state = value;
                _dispatcher.Invoke(()=>StateChanged?.Invoke(this, new ValueChangedEventArgs<MediaPlayState>(prev, value)));
            }
        }

        public MediaStateData()
        {
            Canvas = new WriteableBitmap(720, 480, 96, 96, PixelFormats.Bgr24, null);
        }
        public void Initialize(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void CreateImage(int width, int height)
        {
            Canvas?.Freeze();
            Canvas = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr24, null);
        }
        public void UpdateImage(Mat image)
        {
            _dispatcher.Invoke(() => { BitmapSourceUtil.CopyFromMat(Canvas, image); });
        }
        public void UpdateImage(MemoryStream ms, int width, int height)
        {
            _dispatcher.Invoke(() => { BitmapSourceUtil.CopyFromMs(Canvas, ms, width, height); });
        }
    }
}
