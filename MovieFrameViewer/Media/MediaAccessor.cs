using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpenCvSharp.Extensions;
using OpenCvSharp;
using System.Windows.Interop;
using System.Windows;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;

namespace MovieFrameViewer.Media
{
    internal class MediaAccessor
    {
        #region イベント
        public event EventHandler Opened;
        public event EventHandler<ValueChangedEventArgs<MediaPlayState>> StateChanged
        {
            add { _stateData.StateChanged += value; }
            remove { _stateData.StateChanged -= value; }
        }
        public event EventHandler FramePosChanged;
        #endregion

        public static MediaAccessor Inst { get; } = new MediaAccessor();
        public string Ext { get; private set; } = string.Empty;
        public string FilePath { get; private set; } = string.Empty;

        private MediaStateData _stateData = new MediaStateData();
        private IMediaController _controller = new MovController();
        private Dispatcher _dispatcher;

        public bool IsPlaying => _stateData.IsPlaying;
        public bool IsOpened => _stateData.State != MediaPlayState.None;
        public TimeSpan PlayTime => _controller.PlayTime;
        public int CurrentFrameIndex => _controller.CurrentFrameIndex;
        public int TotalFrame => _controller.TotalFrame;
        public System.Drawing.Size Resolution => _controller.Resolution;
        public double Fps => _controller.Fps;

        public WriteableBitmap BmpSource => _stateData.Canvas;

        internal void Initialize(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _stateData.Initialize(dispatcher);
            _controller.Initialize(_stateData);
        }

        internal void Load(string path)
        {
            Ext = File.GetAttributes(path).HasFlag(FileAttributes.Directory)
                    ? ".DNG"
                    : Path.GetExtension(path);
            FilePath = path;

            _controller?.Dispose();
            _controller = (Ext == ".DNG")
                    ? new CinemaDngController()
                    : (IMediaController)new MovController();
            _controller.Initialize(_stateData);
            _controller.Load(path);

            _dispatcher.Invoke(() =>
            {
                _stateData.State = MediaPlayState.Stop;
                Opened?.Invoke(this, EventArgs.Empty);
            });
        }
        internal void Close()
        {
            _controller.Close();
        }

        public bool GetFrameProcessing { get; private set; }
        internal bool GetFrame(int frameIndex)
        {
            if (!IsOpened) return false;

            if (GetFrameProcessing) return false;
            GetFrameProcessing = true;
            bool isPlaying = IsPlaying;
            if (isPlaying) Pause();

            try
            {
                _controller.GetFrame(frameIndex);
            }
            finally
            {
                if (isPlaying) Play();
                FramePosChanged?.Invoke(this, EventArgs.Empty);
                GetFrameProcessing = false;
            }

            return true;
        }
        internal Task<bool> GetFrameAsync(int frameIndex)
        {
            return Task.Run(() =>GetFrame(frameIndex));
        }
        public async Task<List<MediaFrameImage>> CreateNeighborinFrames()
        {
            if (!IsOpened) return new List<MediaFrameImage>();
            if (IsPlaying) return new List<MediaFrameImage>();

            var result = await Task.Run(() =>
            {
                var range = GetNearFrameIndexRange();
                return _controller.CreateNeighborinFrames(range);
            });
            return result;
    }

        public Range<int> GetNearFrameIndexRange()
        {
            const int Distance = 2;
            int min = CurrentFrameIndex - Distance;
            int max = CurrentFrameIndex + Distance;
            int slide_min = (min < 0) ? -min : 0;
            int slide_max = (TotalFrame <= max) ? max + 1 - TotalFrame : 0;
            if (0 < slide_min)
            {
                min += slide_min;
                max += slide_min;
            }
            if (0 < slide_max)
            {
                max -= slide_max;
                min -= slide_max;
            }
            min = Math.Max(0, min);
            max = Math.Min(TotalFrame - 1, max);
            return new Range<int>(min, max, CurrentFrameIndex);
        }

        internal void Play()
        {
            _controller.Play();
        }
        internal void Stop()
        {
            _controller.Stop();
        }
        internal void Pause()
        {
            _controller.Pause();
        }

        internal void TogglePlay()
        {
            switch (_stateData.State)
            {
                case MediaPlayState.Play: Pause();break;
                case MediaPlayState.Stop: Play(); break;
                case MediaPlayState.Pause: Play(); break;
                default: break;
            }
        }
    }
}