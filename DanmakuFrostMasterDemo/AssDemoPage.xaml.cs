using Atelier39;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Navigation;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class AssDemoPage : Page
    {
        private DanmakuFrostMaster _danmakuController;
        private DispatcherTimer _danmakuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

        public AssDemoPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Disabled;

            _danmakuTimer.Tick += _danmakuTimer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs args)
        {
            if (_danmakuController == null)
            {
                _danmakuController = new DanmakuFrostMaster(_canvasDanmaku);
                _danmakuController.SetSubtitleLayer(DanmakuDefaultLayerDef.SubtitleLayerId);

                _danmakuController.SetAutoControlDensity(false);
                _danmakuController.SetRollingDensity(-1);
                _danmakuController.SetIsTextBold(true);
                _danmakuController.SetBorderColor(Colors.Blue);
            }

            base.OnNavigatedTo(args);
        }

        private void _btnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                _mpe.MediaPlayer.Pause();

                _danmakuTimer.Stop();
                _danmakuController?.Stop();

                rootFrame.GoBack();
            }
        }

        private void _danmakuTimer_Tick(object sender, object arg)
        {
            if (_mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                TimeSpan playTime = _mpe.MediaPlayer.PlaybackSession.Position;
                _danmakuController?.UpdateTime((uint)playTime.TotalMilliseconds);
            }
        }

        private void _sliderPos_ValueChanged(object sender, RangeBaseValueChangedEventArgs args)
        {
            Slider slider = (Slider)sender;
            if (slider.IsEnabled)
            {
                if (_mpe.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    TimeSpan videoDuration = _mpe.MediaPlayer.PlaybackSession.NaturalDuration;
                    TimeSpan newTime = TimeSpan.FromMilliseconds(videoDuration.TotalMilliseconds * slider.Value / slider.Maximum);
                    _mpe.MediaPlayer.PlaybackSession.Position = newTime;
                    _danmakuController?.Seek((uint)newTime.TotalMilliseconds);
                }
                else
                {
                    slider.Value = 0;
                }
            }
        }

        private async void _btnOpen_Click(object sender, RoutedEventArgs args)
        {
            FileOpenPicker videoOpenPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
            };
            videoOpenPicker.FileTypeFilter.Add(".mp4");
            StorageFile videoFile = await videoOpenPicker.PickSingleFileAsync();

            FileOpenPicker assOpenPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.VideosLibrary,
            };
            assOpenPicker.FileTypeFilter.Add(".ass");
            StorageFile assFile = await assOpenPicker.PickSingleFileAsync();

            if (videoFile != null && assFile != null)
            {
                using (Stream assStream = await assFile.OpenStreamForReadAsync())
                {
                    using (StreamReader assReader = new StreamReader(assStream))
                    {
                        string assStr = assReader.ReadToEnd();
                        List<DanmakuItem> danmakuList = AssParser.GetDanmakuList(assStr);
                        if (danmakuList != null && danmakuList.Count > 0)
                        {
                            _danmakuController?.SetDanmakuList(danmakuList);
                            //_danmakuController?.Restart();
                        }
                    }
                }

                Stream videoStream = await videoFile.OpenStreamForReadAsync();
                _mpe.MediaPlayer.Source = MediaSource.CreateFromStream(videoStream.AsRandomAccessStream(), string.Empty);
                _mpe.MediaPlayer.Play();

                _sliderPos.Value = 0;
                _sliderPos.IsEnabled = true;

                _danmakuTimer.Start();
            }
            else
            {
                MessageDialog md = new MessageDialog($"Failed to open {(videoFile == null ? "video" : "ass")} file");
                await md.ShowAsync();
            }
        }

        private void _cbDebugMode_Checked(object sender, RoutedEventArgs args)
        {
            if (_danmakuController != null)
            {
                _danmakuController.DebugMode = true;
            }
        }

        private void _cbDebugMode_Unchecked(object sender, RoutedEventArgs args)
        {
            if (_danmakuController != null)
            {
                _danmakuController.DebugMode = false;
            }
        }
    }
}
