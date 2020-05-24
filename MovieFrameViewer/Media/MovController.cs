using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MovieFrameViewer.Media
{
    public class MovController : IMediaController
    {
        private VideoCapture _video;
        private Task _captureTask;
        private CancellationTokenSource _tokenSource;
        private MediaStateData _stateData;
        private object _syncObject = new object();

        private bool IsOpened { get => _video != null; }

        public TimeSpan PlayTime
        {
            get
            {
                if (!IsOpened) return new TimeSpan();
                int sec = (int)((double)_video.FrameCount / _video.Fps);
                int msec_frame = _video.FrameCount - (int)(sec * _video.Fps);
                int msec = (int)(1000 * msec_frame / _video.Fps);
                return new TimeSpan(0, 0, 0, sec, msec);
            }
        }

        public int CurrentFrameIndex { get { return (!IsOpened) ? 0 : Math.Min(_video.PosFrames, _video.FrameCount - 1); } }
        public int TotalFrame { get { return (!IsOpened) ? 0 : _video.FrameCount; } }

        public System.Drawing.Size Resolution => new System.Drawing.Size(_video.FrameWidth, _video.FrameHeight);

        public double Fps => _video.Fps;

        public void Initialize(MediaStateData stateData)
        {
            _stateData = stateData;
        }
        public void Dispose()
        {
            _video?.Dispose();
            _captureTask?.Dispose();
            _tokenSource?.Dispose();
        }
        public void Load(string path)
        {
            _video = new VideoCapture(path);
            _video.PosFrames = 0;

            _stateData.CreateImage(_video.FrameWidth, _video.FrameHeight);
            GetFrame(0);
        }
        public void Close()
        {
            if (!IsOpened)
            {
                return;
            }
            _video.Dispose();
            _video = null;
        }
        public void Play()
        {
            if (_stateData.IsPlaying) return;
            _stateData.State = MediaPlayState.Play;
            int startFrame = ((TotalFrame - 1) <= CurrentFrameIndex) ? 0 : CurrentFrameIndex;
            CaptureAsync(startFrame);
        }
        public async void Stop()
        {
            if (!_stateData.IsPlaying) return;

            if (_captureTask != null)
            {
                _tokenSource.Cancel();
                await _captureTask;
            }
            _stateData.State = MediaPlayState.Stop;
        }
        public async void Pause()
        {
            if (_captureTask != null)
            {
                _tokenSource.Cancel();
                await _captureTask;
            }
            _stateData.State = MediaPlayState.Pause;
        }

        public bool GetFrame(int frameIndex)
        {
            lock (_syncObject)
            {
                Mat image = new Mat();
                _video.PosFrames = frameIndex;
                if (!_video.Read(image)) return false;
                if (image.Empty()) return false;

                _stateData.UpdateImage(image);
                image.Dispose();
                _video.PosFrames = frameIndex;   // 1フレーム取得すると現在のフレーム位置が進むので再設定しておく
            }
            return true;
        }
        public List<MediaFrameImage> CreateNeighborinFrames(Range<int> frameRange)
        {
            List<MediaFrameImage> frames = new List<MediaFrameImage>();

            lock (_syncObject)
            {
                int prevFrame = _video.PosFrames;
                _video.PosFrames = frameRange.Min;
                for (int i = frameRange.Min; i <= frameRange.Max; ++i)
                {
                    using (var image = _video.RetrieveMat())
                    {
                        if (image.Empty()) break;
                        frames.Add(new MediaFrameImage(i + 1, BitmapSourceUtil.FromMat(image, System.Windows.Media.PixelFormats.Bgr24)));   // frameNo = frameIndex + 1
                    }
                }
                _video.PosFrames = prevFrame;
            }
            return frames;
        }

        private void CaptureAsync(int frame)
        {
            Debug.Assert(_video != null);

            _video.PosFrames = frame;
            int interval = (int)(1000 / _video.Fps);

            _tokenSource?.Dispose();
            _tokenSource = new CancellationTokenSource();
            _captureTask = new Task(() =>
            {
                var watch = new Stopwatch();
                watch.Reset();
                watch.Start();

                lock (_syncObject)
                {
                    var prevMs = watch.ElapsedMilliseconds;
                    Mat image;
                    while ((image = _video.RetrieveMat()) != null && !image.Empty() && !_tokenSource.IsCancellationRequested)
                    {
                        _stateData.UpdateImage(image);
                        image.Dispose();
                        var curMs = watch.ElapsedMilliseconds;
                        var diff = (int)Math.Min(curMs - prevMs, 100);
                        Debug.WriteLine(diff);
                        Task.Delay(Math.Max(0, interval - diff));
                        prevMs = curMs;
                    }
                }
                if (TotalFrame <= CurrentFrameIndex + 1)
                {
                    _stateData.State = MediaPlayState.Stop;// 最後まで再生した
                }
            },
            _tokenSource.Token);
            _captureTask.Start();
        }

    }
}
