using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MovieFrameViewer.Properties;
using MovieFrameViewer.Media;
using System.Reactive.Linq;
using Reactive.Bindings.Extensions;

namespace MovieFrameViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _mainViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _mainViewModel = new MainViewModel(this.Dispatcher, ImageView);
            DataContext = _mainViewModel;
            MediaAccessor.Inst.Initialize(this.Dispatcher);

            // スライドバー操作時イベントの監視登録 (操作方法によっては大量発行されるので少し時間をおいて実行する)
            var movieSliderValueChanged = Observable.FromEvent<RoutedPropertyChangedEventHandler<double>, RoutedPropertyChangedEventArgs<double>>(
                h => (s, e) => h(e),    // RoutedPropertyChangedEventHandler<double> で受けたイベントを Action<RoutedPropertyChangedEventArgs<double>> に変換して実行する
                h => MovieSlider.ValueChanged += h,
                h => MovieSlider.ValueChanged -= h
                );
            movieSliderValueChanged.Throttle(TimeSpan.FromMilliseconds(200))        // 操作後200ms何もなければ処理を開始して、
                .Where(e => (e.NewValue != _mainViewModel.FrameNo) && !MediaAccessor.Inst.GetFrameProcessing)                   // 現在のフレーム位置と異なる場合のみ
                .Subscribe(e => MediaAccessor.Inst.GetFrameAsync((int)e.NewValue - 1));  // フレーム情報を取得する
        }


        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                MediaAccessor.Inst.Load(files[0]);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
           MediaAccessor.Inst.TogglePlay();
        }
    }
}
