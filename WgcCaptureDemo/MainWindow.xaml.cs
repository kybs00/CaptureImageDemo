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
        [System.Runtime.InteropServices.ComImport]
        [System.Runtime.InteropServices.Guid("3A3DCD6C-3EAB-43DC-BCDE-45671CE800C8")]
        [System.Runtime.InteropServices.InterfaceType(
            System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
        interface IDataTransferManagerInterop
        {
            IntPtr GetForWindow([System.Runtime.InteropServices.In] IntPtr appWindow,
                [System.Runtime.InteropServices.In] ref Guid riid);
            void ShowShareUIForWindow(IntPtr appWindow);
        }

        static readonly Guid _dtm_iid =
            new Guid(0xa5caee9b, 0x8708, 0x49d1, 0x8d, 0x36, 0x67, 0xd2, 0x5a, 0x8d, 0xa0, 0x0c);

        private void CaptureButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Retrieve the window handle (HWND) of the current WinUI 3 window.
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            IDataTransferManagerInterop interop =
                Windows.ApplicationModel.DataTransfer.DataTransferManager.As
                    <IDataTransferManagerInterop>();

            IntPtr result = interop.GetForWindow(hWnd, _dtm_iid);
            var dataTransferManager = WinRT.MarshalInterface
                <Windows.ApplicationModel.DataTransfer.DataTransferManager>.FromAbi(result);

            dataTransferManager.DataRequested += (sender, args) =>
            {
                args.Request.Data.Properties.Title = "In a desktop app...";
                args.Request.Data.SetText("...display WinRT UI objects that depend on CoreWindow.");
                args.Request.Data.RequestedOperation =
                    Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            };

            // Show the Share UI
            interop.ShowShareUIForWindow(hWnd);
        }
    }
}