using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DanmakuFrostMasterDemo
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void _btnDemo1_Click(object sender, RoutedEventArgs args)
        {
            Frame.Navigate(typeof(DanmakuDemoPage));
        }

        private void _btnDemo2_Click(object sender, RoutedEventArgs args)
        {
            Frame.Navigate(typeof(BilibiliDemo1Page));
        }

        private void _btnDemo3_Click(object sender, RoutedEventArgs args)
        {
            Frame.Navigate(typeof(BilibiliDemo2Page));
        }

        private void _btnDemo4_Click(object sender, RoutedEventArgs args)
        {
            Frame.Navigate(typeof(AssDemoPage));
        }
    }
}
