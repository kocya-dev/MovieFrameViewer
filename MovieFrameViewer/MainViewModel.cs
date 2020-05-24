using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MovieFrameViewer.Media;
using System.Windows.Input;
using System.Windows.Controls;

namespace MovieFrameViewer
{
    public class MainViewModel  : INotifyPropertyChanged
    {
        #region コマンド

        public ICommand PlayCommand = new DelegateCommand(
            ()=> MediaAccessor.Inst.TogglePlay(),
            ()=> MediaAccessor.Inst.IsOpened);

        #endregion

        #region イベント
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region プロパティ
        private string _title = "MovieViewer";
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged(); }
        }
        public BitmapSource _playActionImage = BitmapSourceUtil.FromBitmap(Properties.Resources.appbar_control_play);
        public BitmapSource PlayActionImage
        {
            get { return _playActionImage; }
            set { if (_playActionImage?.GetHashCode() == value.GetHashCode()) return; _playActionImage = value; NotifyPropertyChanged(); }
        }
        public double _frameNo = 1;
        public double FrameNo
        {
            get { return _frameNo; }
            set
            {
                if (_frameNo == value) return; 
                _frameNo = value; 
                NotifyPropertyChanged();
                FrameText = string.Format($"{_frameNo}/{FrameMax}");
            }
        }
        public double _frameMax = 0;
        public double FrameMax
        {
            get { return _frameMax; }
            set 
            {
                if (_frameMax == value) return; 
                _frameMax = value; 
                NotifyPropertyChanged();
                FrameText = string.Format($"{FrameNo}/{_frameMax}");
            }
        }
        public string _frameText = "0/0";
        public string FrameText
        {
            get { return _frameText; }
            set { if (_frameText == value) return; _frameText = value; NotifyPropertyChanged(); }
        }
        private string _timeText = "00:00:00";
        public string TimeText
        {
            get { return _timeText; }
            set { if (_timeText == value) return; _timeText = value; NotifyPropertyChanged(); }
        }
        List<MediaFrameImage> _neighboringFrameInfo = new List<MediaFrameImage>();
        public List<MediaFrameImage> NeighboringFrameInfo
        {
            get { return _neighboringFrameInfo; }
            private set
            {
                _neighboringFrameInfo.Clear();
                _neighboringFrameInfo = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(CurrentFrameInfo));
            }
        }
        public MediaFrameImage CurrentFrameInfo
        {
            get 
            {
                try
                {
                    return (NeighboringFrameInfo.Count() == 0)
                           ? new MediaFrameImage(1, null)
                           : NeighboringFrameInfo.First(x => x.FrameNo == FrameNo);
                }
                catch
                {
                    return NeighboringFrameInfo[0];
                }
            }
        }

        private string _toolTipText = "No Media";
        public string ToolTipText
        {
            get { return _toolTipText; }
            set { _toolTipText = value; NotifyPropertyChanged(); }
        }
        #endregion

        #region フィールド
        private System.Windows.Controls.Image _image;
        private DispatcherTimer _dispatcherTimer = new DispatcherTimer();
        #endregion

        public WriteableBitmap BmpSource { get { return MediaAccessor.Inst.BmpSource; } }


        public MainViewModel(Dispatcher dispatcher, System.Windows.Controls.Image image) 
        {
            _image = image;
            MediaAccessor media = MediaAccessor.Inst;
            media.Initialize(dispatcher);
            media.Opened += Inst_Opened;
            media.StateChanged += Inst_StateChanged;
            media.FramePosChanged += Inst_FramePosChanged;
            _dispatcherTimer.Interval = TimeSpan.FromMilliseconds(50);
            _dispatcherTimer.Tick += _dispatcherTimer_Tick;
        }

        private async void Inst_Opened(object sender, EventArgs e)
        {
            MediaAccessor media = MediaAccessor.Inst;
            var time = media.PlayTime;
            TimeText = string.Format($"{new TimeSpan(time.Hours, time.Minutes, time.Seconds)}.{time.Milliseconds}");
            FrameMax = media.TotalFrame;
            FrameNo = media.CurrentFrameIndex + 1;

            Size resolution = MediaAccessor.Inst.Resolution;
            string fpsText = (MediaAccessor.Inst.Fps < 1) ? "--" : MediaAccessor.Inst.Fps.ToString("F2");
            Title = string.Format($"MovieViewer ({MediaAccessor.Inst.FilePath})");
            ToolTipText = string.Format($"{resolution.Width}x{resolution.Height} Fps{fpsText}");

            NeighboringFrameInfo = await media.CreateNeighborinFrames();
            _image.Source = media.BmpSource;
        }
        private async void Inst_StateChanged(object sender, ValueChangedEventArgs<MediaPlayState> e)
        {
            if (e.OldValue == e.NewValue) return;

            switch (e.NewValue)
            {
                case MediaPlayState.Play:
                    PlayActionImage = BitmapSourceUtil.FromBitmap(Properties.Resources.appbar_control_pause);
                    break;
                case MediaPlayState.Pause:
                    PlayActionImage = BitmapSourceUtil.FromBitmap(Properties.Resources.appbar_control_resume);
                    break;
                case MediaPlayState.Stop:
                    PlayActionImage = BitmapSourceUtil.FromBitmap(Properties.Resources.appbar_control_play);
                    break;
                default:
                    break;
            }

            if (e.OldValue != MediaPlayState.Play && e.NewValue == MediaPlayState.Play)
            {
                _dispatcherTimer.Start();
            }
            else if (e.OldValue == MediaPlayState.Play && e.NewValue != MediaPlayState.Play)
            {
                _dispatcherTimer.Stop();
                FrameNo = MediaAccessor.Inst.CurrentFrameIndex + 1;
                if (!NeighboringFrameInfo.Any(x => x.FrameNo == FrameNo))
                {
                    NeighboringFrameInfo = await MediaAccessor.Inst.CreateNeighborinFrames();
                }
            }
        }
        private async void Inst_FramePosChanged(object sender, EventArgs e)
        {
            FrameNo = MediaAccessor.Inst.CurrentFrameIndex + 1;
            if (!NeighboringFrameInfo.Any(x => x.FrameNo == FrameNo))
            {
                NeighboringFrameInfo = await MediaAccessor.Inst.CreateNeighborinFrames();
            }
        }

        private void _dispatcherTimer_Tick(object sender, EventArgs e)
        {
            FrameNo = MediaAccessor.Inst.CurrentFrameIndex + 1;
        }
    }
}

