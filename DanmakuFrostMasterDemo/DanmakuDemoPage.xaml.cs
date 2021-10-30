using Atelier39;
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class DanmakuDemoPage : Page
    {
        private DanmakuFrostMaster _danmakuController;
        private DispatcherTimer _danmakuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        private uint _timerPassedMs = 0;

        public DanmakuDemoPage()
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
            }

            base.OnNavigatedTo(args);
        }

        private void _btnBack_Click(object sender, RoutedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                rootFrame.GoBack();
            }
        }

        private void _danmakuTimer_Tick(object sender, object arg)
        {
            _danmakuController?.UpdateTime(_timerPassedMs);
            DispatcherTimer timer = (DispatcherTimer)sender;
            _timerPassedMs += (uint)timer.Interval.TotalMilliseconds;
        }

        private void _btnSend_Click(object sender, RoutedEventArgs args)
        {
            string danmakuText = _tboxDanmaku.Text;
            if (!string.IsNullOrWhiteSpace(danmakuText))
            {
                DanmakuMode mode = (DanmakuMode)Enum.Parse(typeof(DanmakuMode), _cbDanmakuMode.SelectedItem as string);
                DanmakuItem danmakuItem = new DanmakuItem
                {
                    StartMs = _timerPassedMs,
                    Mode = mode,
                    Text = danmakuText,
                    TextColor = Colors.White,
                    BaseFontSize = DanmakuItem.DefaultBaseFontSize,
                };
                _danmakuController.AddRealtimeDanmaku(danmakuItem, insertToList: false);
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
