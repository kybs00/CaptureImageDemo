using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CaptureImageDemo
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

        private void GdiCaptureButton_OnClick(object sender, RoutedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var image = CaptureScreen();
            Debug.WriteLine(stopwatch.ElapsedMilliseconds);
            CaptureImage.Source = ConvertBitmapToBitmapSource(image);
            Debug.WriteLine(stopwatch.ElapsedMilliseconds);
        }
        /// <summary>
        /// 截图屏幕
        /// </summary>
        /// <returns></returns>
        public static Bitmap CaptureScreen()
        {
            IntPtr desktopWindow = GetDesktopWindow();
            //获取窗口位置大小
            GetWindowRect(desktopWindow, out var lpRect);
            return CaptureByGdi(desktopWindow, 0d, 0d, lpRect.Width, lpRect.Height);
        }
        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            using MemoryStream memoryStream = new MemoryStream();
            // 将 System.Drawing.Bitmap 保存到内存流中
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            // 重置内存流的指针到开头
            memoryStream.Seek(0, SeekOrigin.Begin);

            // 创建 BitmapImage 对象并从内存流中加载图像
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            // 确保内存流不会被回收
            bitmapImage.Freeze();
            return bitmapImage;
        }
        /// <summary>
        /// 截图窗口/屏幕
        /// </summary>
        /// <param name="windowIntPtr">窗口句柄(窗口或者桌面)</param>
        /// <param name="left">水平坐标</param>
        /// <param name="top">竖直坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns></returns>
        private static Bitmap CaptureByGdi(IntPtr windowIntPtr, double left, double top, double width, double height)
        {
            IntPtr windowDc = GetWindowDC(windowIntPtr);
            IntPtr compatibleDc = CreateCompatibleDC(windowDc);
            IntPtr compatibleBitmap = CreateCompatibleBitmap(windowDc, (int)width, (int)height);
            IntPtr bitmapObj = SelectObject(compatibleDc, compatibleBitmap);
            BitBlt(compatibleDc, 0, 0, (int)width, (int)height, windowDc, (int)left, (int)top, CopyPixelOperation.SourceCopy);
            Bitmap bitmap = System.Drawing.Image.FromHbitmap(compatibleBitmap);
            //释放
            SelectObject(compatibleDc, bitmapObj);
            DeleteObject(compatibleBitmap);
            DeleteDC(compatibleDc);
            ReleaseDC(windowIntPtr, windowDc);
            return bitmap;
        }
    /// <summary>
    /// 获取桌面窗口
    /// </summary>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();
    /// <summary>
    /// 获取整个窗口的矩形区域
    /// </summary>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
    /// <summary>
    /// 检索整个窗口的设备上下文
    /// </summary>
    /// <param name="hWnd">具有要检索的设备上下文的窗口的句柄</param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);
    /// <summary>
    /// 创建与指定设备兼容的内存设备上下文
    /// </summary>
    /// <param name="hdc">现有 DC 的句柄</param>
    /// <returns>如果函数成功，则返回值是内存 DC 的句柄，否则返回Null</returns>
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);
    /// <summary>
    /// 将对象选择到指定的设备上下文中
    /// </summary>
    /// <param name="hdc">DC 的句柄</param>
    /// <param name="gdiObj">要选择的对象句柄</param>
    /// <returns>如果函数成功，则返回值是兼容位图 (DDB) 的句柄，否则返回Null</returns>
    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr gdiObj);
    /// <summary>
    /// 创建与与指定设备上下文关联的设备的位图
    /// </summary>
    /// <param name="hdc">设备上下文的句柄</param>
    /// <param name="nWidth">位图宽度（以像素为单位）</param>
    /// <param name="nHeight">位图高度（以像素为单位）</param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);
    /// <summary>
    /// 执行与从指定源设备上下文到目标设备上下文中的像素矩形对应的颜色数据的位块传输
    /// </summary>
    /// <param name="hdcDest">目标设备上下文的句柄</param>
    /// <param name="xDest">目标矩形左上角的 x 坐标（逻辑单位）</param>
    /// <param name="yDest">目标矩形左上角的 y 坐标（逻辑单位）</param>
    /// <param name="wDest">源矩形和目标矩形的宽度（逻辑单位）</param>
    /// <param name="hDest">源矩形和目标矩形的高度（逻辑单位）</param>
    /// <param name="hdcSource">源设备上下文的句柄</param>
    /// <param name="xSrc">源矩形左上角的 x 坐标（逻辑单位）</param>
    /// <param name="ySrc">源矩形左上角的 y 坐标（逻辑单位）</param>
    /// <param name="rop">定义如何将源矩形的颜色数据与目标矩形的颜色数据相结合</param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdcDest,
        int xDest, int yDest, int wDest, int hDest, IntPtr hdcSource,
        int xSrc, int ySrc, CopyPixelOperation rop);
    /// <summary>
    /// 删除逻辑笔、画笔、字体、位图、区域或调色板，释放与对象关联的所有系统资源。
    /// 删除对象后，指定的句柄将不再有效。
    /// </summary>
    /// <param name="hObject"></param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
    /// <summary>
    /// 删除指定的设备上下文
    /// </summary>
    /// <param name="hdc">设备上下文的句设备上下文的句</param>
    /// <returns></returns>
    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC([In] IntPtr hdc);
    /// <summary>
    /// 释放设备上下文 （DC），释放它以供其他应用程序使用
    /// </summary>
    /// <param name="hWnd"></param>
    /// <param name="hdc"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ReleaseDC(IntPtr hWnd, IntPtr hdc);

    /// <summary>
    /// 定义一个矩形区域。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        /// <summary>
        /// 矩形左侧的X坐标。
        /// </summary>
        public int Left;

        /// <summary>
        /// 矩形顶部的Y坐标。
        /// </summary>
        public int Top;

        /// <summary>
        /// 矩形右侧的X坐标。
        /// </summary>
        public int Right;

        /// <summary>
        /// 矩形底部的Y坐标。
        /// </summary>
        public int Bottom;

        /// <summary>
        /// 获取矩形的宽度。
        /// </summary>
        public int Width => Right - Left;

        /// <summary>
        /// 获取矩形的高度。
        /// </summary>
        public int Height => Bottom - Top;

        /// <summary>
        /// 初始化一个新的矩形。
        /// </summary>
        /// <param name="left">矩形左侧的X坐标。</param>
        /// <param name="top">矩形顶部的Y坐标。</param>
        /// <param name="right">矩形右侧的X坐标。</param>
        /// <param name="bottom">矩形底部的Y坐标。</param>
        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

        private void GraphicsCaptureButton_OnClick(object sender, RoutedEventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var image = CaptureScreen1();
            Debug.WriteLine(stopwatch.ElapsedMilliseconds);
            CaptureImage.Source = ConvertBitmapToBitmapSource(image);
            Debug.WriteLine(stopwatch.ElapsedMilliseconds);
        }
        /// <summary>
        /// 截图屏幕
        /// </summary>
        /// <returns></returns>
        public static Bitmap CaptureScreen1()
        {
            IntPtr desktopWindow = GetDesktopWindow();
            //获取窗口位置大小
            GetWindowRect(desktopWindow, out var lpRect);
            return CaptureScreenByGraphics(0, 0, lpRect.Width, lpRect.Height);
        }
        /// <summary>
        /// 截图屏幕
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <param name="width">截取的宽度</param>
        /// <param name="height">截取的高度</param>
        /// <returns></returns>
        public static Bitmap CaptureScreenByGraphics(int x, int y, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            return bitmap;
        }
    }
}