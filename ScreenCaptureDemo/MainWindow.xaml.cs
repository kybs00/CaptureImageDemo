using OpenCvSharp;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace DxgiScreenCaptureDemo
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
        var monitorDxgiCapture = new MonitorDxgiCapture();
        monitorDxgiCapture.FrameArrived += WgcCapture_FrameArrived;
        monitorDxgiCapture.StartCapture();
    }

    private void WgcCapture_FrameArrived(object? sender, CaptureFrame e)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            var stride = e.Size.Width * 4; // 4 bytes per pixel in BGRA format
            var bitmap = BitmapSource.Create(e.Size.Width, e.Size.Height, 96, 96, PixelFormats.Bgra32, null, e.Data, stride);

            bitmap.Freeze();
            CaptureImage.Source = bitmap;
        });
    }
    }
}