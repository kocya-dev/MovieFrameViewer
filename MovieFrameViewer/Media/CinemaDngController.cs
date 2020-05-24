using ImageProcess;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MovieFrameViewer.Media
{
    public class CinemaDngController : IMediaController
    {
        private MediaStateData _stateData;
        private List<string> _fileList = new List<string>();
        private ImageInfo _info = new ImageInfo();
        private Task _captureTask;
        private CancellationTokenSource _tokenSource;

        public bool IsOpened => 0 < TotalFrame;
        public TimeSpan PlayTime { get; private set; } = TimeSpan.Zero;
        public int CurrentFrameIndex { get; private set; }
        public int TotalFrame => _fileList.Count;

        public Size Resolution => new Size(_info.Width, _info.Height);

        public double Fps => 0.0f;

        public void Initialize(MediaStateData stateData)
        {
            _stateData = stateData;
        }
        public void Dispose()
        {
            if (_fileList != null)
            {
                _fileList.Clear();
                _fileList = null;
            }
            _captureTask?.Dispose();
            _tokenSource?.Dispose();
        }
        public void Load(string path)
        {
            _fileList.Clear();
            foreach (var filePath in Directory.GetFiles(path, "*.DNG")){
                _fileList.Add(filePath);
            }
            _info = ImageLoader.LoadInfo(_fileList[0]);
            _stateData.CreateImage(_info.Width, _info.Height);
            GetFrame(0);
        }
        public void Close()
        {
            _fileList.Clear();
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
            using (var ms = new MemoryStream(_info.DataSize))
            {
                ImageLoader.Load(_fileList[frameIndex], ms);
                /*
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.None;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                */
                _stateData.UpdateImage(ms, _info.Width, _info.Height);
                CurrentFrameIndex = frameIndex;
            }
            return true;
        }

        public List<MediaFrameImage> CreateNeighborinFrames(Range<int> frameRange)
        {
            List<MediaFrameImage> frames = new List<MediaFrameImage>();
            Parallel.For(frameRange.Min, frameRange.Max + 1, (i) =>
            {
                using (var ms = new MemoryStream(_info.DataSize))
                {
                    ImageLoader.Load(_fileList[i], ms);
                    ms.Position = 0;
                    frames.Add(new MediaFrameImage(
                        i + 1,    // frameNo = frameIndex + 1
                        BitmapSourceUtil.FromMs(ms, _info.Width, _info.Height, System.Windows.Media.PixelFormats.Bgr24),
                        Path.GetFileNameWithoutExtension(_fileList[i])));
                }
            });
            frames.Sort((MediaFrameImage lhs, MediaFrameImage rhs)=>lhs.FrameNo.CompareTo(rhs.FrameNo));
            return frames;
        }
        private void CaptureAsync(int frame)
        {
            CurrentFrameIndex = frame;
            int interval = (int)(1000 / 30);    // 30fps固定

            _tokenSource?.Dispose();
            _tokenSource = new CancellationTokenSource();
            _captureTask = new Task(() =>
            {
                var watch = new Stopwatch();
                watch.Reset();
                watch.Start();

                var prevMs = watch.ElapsedMilliseconds;
                using (var ms = new MemoryStream(_info.DataSize))
                {
                    for (int i = frame; i < TotalFrame; ++i)
                    {
                        if (_tokenSource.IsCancellationRequested)
                        {
                            break;
                        }
                        ms.Position = 0;
                        ImageLoader.Load(_fileList[i], ms);
                        _stateData.UpdateImage(ms, _info.Width, _info.Height);
                        CurrentFrameIndex = i;

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
