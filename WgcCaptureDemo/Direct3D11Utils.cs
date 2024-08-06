using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using WinRT;
using Device = SharpDX.Direct3D11.Device;
using Device3 = SharpDX.DXGI.Device3;

namespace WgcCaptureDemo
{
    /// <summary>
    ///     D3D辅助类
    /// </summary>
    internal static class Direct3D11Utils
    {
        private static Guid IInspectable = new("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90");
        private static Guid ID3D11Resource = new("dc8e63f3-d12b-4952-b47b-5e45026a862d");
        private static Guid IDXGIAdapter3 = new("645967A4-1392-4310-A798-8053CE3E93FD");
        private static readonly Guid ID3D11Device = new("db6f6ddb-ac77-4e88-8253-819df9bbf140");
        private static readonly Guid ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true,
            CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface", SetLastError = true,
            CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern uint CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);


        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IDirect3DDxgiInterfaceAccess
        {
            IntPtr GetInterface([In] ref Guid iid);
        }

        /// <summary>
        /// 创建硬件驱动类型的IDirect3DDevice实例
        /// </summary>
        /// <returns></returns>
        public static IDirect3DDevice CreateDevice()
        {
            return CreateDevice(false);
        }

        /// <summary>
        /// 创建一个使用指定设备类型（硬件或软件）的IDirect3DDevice实例。
        /// </summary>
        /// <param name="useWarp">创建一个使用指定设备类型（硬件或软件）的IDirect3DDevice实例。</param>
        /// <returns>返回使用指定驱动类型创建的IDirect3DDevice实例</returns>
        public static IDirect3DDevice CreateDevice(bool useWarp)
        {
            var d3dDevice = new Device(useWarp ? DriverType.Software : DriverType.Hardware, DeviceCreationFlags.BgraSupport);
            // Acquire the DXGI interface for the Direct3D device.
            using var dxgiDevice = d3dDevice.QueryInterface<Device3>();
            // Wrap the native device using a WinRT interop object.
            var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);
            if (hr == 0)
            {
                return MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);
            }
            return null;
        }

        /// <summary>
        /// 从 SharpDX.Direct3D11.Device 创建 IDirect3DDevice 实例。
        /// </summary>
        /// <param name="d3dDevice">SharpDX.Direct3D11.Device 对象。</param>
        /// <returns>返回一个 IDirect3DDevice 实例，表示从 SharpDX.Direct3D11.Device 创建的 Direct3D 设备。</returns>
        public static IDirect3DDevice CreateDirect3DDeviceFromSharpDxDevice(Device d3dDevice)
        {
            IDirect3DDevice device = null;

            // Acquire the DXGI interface for the Direct3D device.
            using var dxgiDevice = d3dDevice.QueryInterface<Device3>();
            // Wrap the native device using a WinRT interop object.
            var hr = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out var pUnknown);
            if (hr == 0)
            {
                device = MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);
            }

            return device;
        }

        /// <summary>
        /// 从 SharpDX.Direct3D11.Texture2D 创建 IDirect3DSurface 实例。
        /// </summary>
        /// <param name="texture">SharpDX.Direct3D11.Texture2D 对象。</param>
        /// <returns>返回一个 IDirect3DSurface 实例，表示从 SharpDX.Direct3D11.Texture2D 创建的 Direct3D 表面。</returns>
        public static IDirect3DSurface CreateDirect3DSurfaceFromSharpDxTexture(Texture2D texture)
        {
            // Acquire the DXGI interface for the Direct3D surface.
            using var dxgiSurface = texture.QueryInterface<Surface>();
            // Wrap the native device using a WinRT interop object.
            var hr = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface.NativePointer, out var pUnknown);

            if (hr != 0) return null;

#if NET5_0_OR_GREATER
            var surface = MarshalInterface<IDirect3DSurface>.FromAbi(pUnknown);
#else
            var surface = Marshal.GetObjectForIUnknown(pUnknown) as IDirect3DSurface;
            Marshal.Release(pUnknown);
#endif
            return surface;
        }

        /// <summary>
        /// 从 IDirect3DDevice 创建 SharpDX.Direct3D11.Device 实例。
        /// </summary>
        /// <param name="device">IDirect3DDevice 对象。</param>
        /// <returns>返回一个 SharpDX.Direct3D11.Device 实例，表示从 IDirect3DDevice 创建的 SharpDX Direct3D 设备。</returns>
        public static Device CreateSharpDxDevice(IDirect3DDevice device)
        {
#if NET5_0_OR_GREATER
            var access = device.As<IDirect3DDxgiInterfaceAccess>();
#else
            var access = (IDirect3DDxgiInterfaceAccess)device;
#endif
            var d3dPointer = access.GetInterface(ID3D11Device);
            var d3dDevice = new Device(d3dPointer);
            return d3dDevice;
        }

        /// <summary>
        /// 从 IDirect3DSurface 创建 SharpDX.Direct3D11.Texture2D 实例。
        /// </summary>
        /// <param name="surface">IDirect3DSurface 对象。</param>
        /// <returns>返回一个 SharpDX.Direct3D11.Texture2D 实例，表示从 IDirect3DSurface 创建的 SharpDX Direct3D Texture2D。</returns>
        public static Texture2D CreateSharpDxTexture2D(IDirect3DSurface surface)
        {
#if NET5_0_OR_GREATER
            var access = surface.As<IDirect3DDxgiInterfaceAccess>();
#else
            var access = (IDirect3DDxgiInterfaceAccess)surface;
#endif
            var d3dPointer = access.GetInterface(ID3D11Texture2D);
            var d3dSurface = new Texture2D(d3dPointer);
            return d3dSurface;
        }
    }
}