using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WgcCaptureDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void CaptureButton_OnClick(object sender, RoutedEventArgs e)
        {
            var hmon = MonitorHelper.GetMonitors().First().Hmon;
            var wgcCapture = new WgcCapture(hmon, CaptureType.Screen);
            wgcCapture.FrameArrived += WgcCapture_FrameArrived;
            wgcCapture.StartCapture();
        }

        private void WgcCapture_FrameArrived(object? sender, CaptureFrame e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                return CaptureImage.Source = ByteArrayToBitmapImage(e.Data, e.Size.Width, e.Size.Height);
            });
        }
        public BitmapSource ByteArrayToBitmapImage(byte[] data, int width, int height)
        {
            var stride = width * 4; // 4 bytes per pixel in BGRA format
            var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, data, stride);
            bitmap.Freeze();
            return bitmap;
        }
    }
}