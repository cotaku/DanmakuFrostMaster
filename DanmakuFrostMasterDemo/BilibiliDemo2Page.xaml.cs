using Atelier39;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class BilibiliDemo2Page : Page
    {
        private DanmakuFrostMaster _danmakuController;
        private DispatcherTimer _danmakuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };

        private List<DanmakuItem> _subtitleDanmakuListJaJp = null;
        private List<DanmakuItem> _subtitleDanmakuListEnUs = null;
        private List<DanmakuItem> _subtitleDanmakuListZhT = null;

        public BilibiliDemo2Page()
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

                _danmakuController.SetAutoControlDensity(false);
                _danmakuController.SetRollingDensity(-1);
                _danmakuController.SetIsTextBold(true);
                _danmakuController.SetBorderColor(Colors.Blue);

                StorageFile danmakuFile = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DemoFiles/demo_3_danmaku.xml")).AsTask().Result;
                using (Stream danmakuStream = danmakuFile.OpenStreamForReadAsync().Result)
                {
                    using (StreamReader danmakuReader = new StreamReader(danmakuStream))
                    {
                        string danmakuXml = danmakuReader.ReadToEnd();
                        List<DanmakuItem> danmakuList = BilibiliDanmakuXmlParser.GetDanmakuList(danmakuXml, null, true, out _, out _, out _);
                        _danmakuController.SetDanmakuList(danmakuList);
                    }
                }

                StorageFile subtitleFile1 = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DemoFiles/demo_3_ja-jp.sub")).AsTask().Result;
                using (Stream subtitleStream = subtitleFile1.OpenStreamForReadAsync().Result)
                {
                    using (StreamReader subtitleReader = new StreamReader(subtitleStream))
                    {
                        string subtitleJson = subtitleReader.ReadToEnd();
                        _subtitleDanmakuListJaJp = BilibiliDanmakuXmlParser.GetSubtitleList(subtitleJson);
                    }
                }
                StorageFile subtitleFile2 = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DemoFiles/demo_3_en-us.sub")).AsTask().Result;
                using (Stream subtitleStream = subtitleFile2.OpenStreamForReadAsync().Result)
                {
                    using (StreamReader subtitleReader = new StreamReader(subtitleStream))
                    {
                        string subtitleJson = subtitleReader.ReadToEnd();
                        _subtitleDanmakuListEnUs = BilibiliDanmakuXmlParser.GetSubtitleList(subtitleJson);
                    }
                }
                StorageFile subtitleFile3 = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///DemoFiles/demo_3_zh-t.sub")).AsTask().Result;
                using (Stream subtitleStream = subtitleFile3.OpenStreamForReadAsync().Result)
                {
                    using (StreamReader subtitleReader = new StreamReader(subtitleStream))
                    {
                        string subtitleJson = subtitleReader.ReadToEnd();
                        _subtitleDanmakuListZhT = BilibiliDanmakuXmlParser.GetSubtitleList(subtitleJson);
                    }
                }
            }

            base.OnNavigatedTo(args);
        }

        private void _btnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                Pause();
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

        private void _btnStartPause_Click(object sender, RoutedEventArgs args)
        {
            Button button = (Button)sender;
            if (!_danmakuTimer.IsEnabled)
            {
                Resume();

                _btnBackward.IsEnabled = true;
                _btnForward.IsEnabled = true;

                button.Content = "Pause";
            }
            else
            {
                Pause();

                _btnBackward.IsEnabled = false;
                _btnForward.IsEnabled = false;

                button.Content = "Resume";
            }
        }

        private void _btnBackward_Click(object sender, RoutedEventArgs args)
        {
            TimeSpan currentPlayPosition = _mpe.MediaPlayer.PlaybackSession.Position;
            if (currentPlayPosition.TotalSeconds > 5)
            {
                Seek(TimeSpan.FromSeconds(currentPlayPosition.TotalSeconds - 5));
            }
        }

        private void _btnForward_Click(object sender, RoutedEventArgs args)
        {
            TimeSpan currentPlayPosition = _mpe.MediaPlayer.PlaybackSession.Position;
            if (currentPlayPosition.TotalSeconds < _mpe.MediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds - 5)
            {
                Seek(TimeSpan.FromSeconds(currentPlayPosition.TotalSeconds + 5));
            }
        }

        private void _cbSubLang_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedIndex == 0)
            {
                _danmakuController?.SetSubtitleList(_subtitleDanmakuListJaJp);
            }
            else if (comboBox.SelectedIndex == 1)
            {
                _danmakuController?.SetSubtitleList(_subtitleDanmakuListEnUs);
            }
            else if (comboBox.SelectedIndex == 2)
            {
                _danmakuController?.SetSubtitleList(_subtitleDanmakuListZhT);
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

        private void Pause()
        {
            _cbSubLang.IsEnabled = false;

            _mpe.MediaPlayer.Pause();

            _danmakuTimer.Stop();
            _danmakuController?.Pause();
        }

        private void Resume()
        {
            _cbSubLang.IsEnabled = true;

            _mpe.MediaPlayer.Play();

            _danmakuController?.Resume();
            _danmakuTimer.Start();
        }

        private void Seek(TimeSpan newTime)
        {
            _mpe.MediaPlayer.PlaybackSession.Position = newTime;
            _danmakuController?.Seek((uint)newTime.TotalMilliseconds);
        }
    }
}
