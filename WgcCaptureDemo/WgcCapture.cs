using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using System.Drawing;
using OpenCvSharp;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Security.AccessControl;
using Size = System.Drawing.Size;

namespace WgcCaptureDemo
{
    /// <summary>
    /// Windows Graphics Capture类（屏幕、窗口截图）
    /// </summary>
    internal class WgcCapture
    {
        public WgcCapture(IntPtr hWnd, CaptureType captureType)
        {
            if (!GraphicsCaptureSession.IsSupported())
            {
                throw new Exception("不支Windows Graphics Capture API");
            }
            var item = captureType == CaptureType.Screen ? CaptureHelper.CreateItemForMonitor(hWnd) : CaptureHelper.CreateItemForWindow(hWnd);
            CaptureSize = new Size(item.Size.Width, item.Size.Height);

            var d3dDevice = Direct3D11Helper.CreateDevice(false);
            _device = Direct3D11Helper.CreateSharpDxDevice(d3dDevice);
            _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(d3dDevice, pixelFormat: DirectXPixelFormat.B8G8R8A8UIntNormalized, numberOfBuffers: 1, item.Size);
            _desktopImageTexture = CreateTexture2D(_device, item.Size);
            _framePool.FrameArrived += OnFrameArrived;
            item.Closed += (i, _) =>
            {
                _framePool.FrameArrived -= OnFrameArrived;
                StopCapture();
                ItemClosed?.Invoke(this, i);
            };
            _session = _framePool.CreateCaptureSession(item);
        }

        private Texture2D CreateTexture2D(Device device, SizeInt32 size)
        {
            return new Texture2D(device, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });
        }

        /// <summary>
        /// 开始捕获
        /// </summary>
        public void StartCapture()
        {
            _session.StartCapture();
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        public void StopCapture()
        {
            Dispose();
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            try
            {
                using var frame = _framePool.TryGetNextFrame();
                if (frame == null) return;
                var data = CopyFrameToBytes(frame);
                var captureFrame = new CaptureFrame(CaptureSize, data);
                FrameArrived?.Invoke(this, captureFrame);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private byte[] CopyFrameToBytes(Direct3D11CaptureFrame frame)
        {
            using var bitmap = Direct3D11Helper.CreateSharpDxTexture2D(frame.Surface);
            _device.ImmediateContext.CopyResource(bitmap, _desktopImageTexture);
            // 将Texture2D资源映射到CPU内存
            var mappedResource = _device.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read, MapFlags.None);
            //Bgra32
            var bytesPerPixel = 4;
            var width = _desktopImageTexture.Description.Width;
            var height = _desktopImageTexture.Description.Height;
            using var inputRgbaMat = new Mat(height, width, MatType.CV_8UC4, mappedResource.DataPointer, mappedResource.RowPitch);

            var data = new byte[CaptureSize.Width * CaptureSize.Height * bytesPerPixel];
            if (CaptureSize.Width != width || CaptureSize.Height != height)
            {
                var size = new OpenCvSharp.Size(CaptureSize.Width, CaptureSize.Height);
                Cv2.Resize(inputRgbaMat, inputRgbaMat, size, interpolation: InterpolationFlags.Linear);
            }
            var sourceSize = new Size(frame.ContentSize.Width, frame.ContentSize.Height);
            if (CaptureSize == sourceSize)
            {
                var rowPitch = mappedResource.RowPitch;
                for (var y = 0; y < height; y++)
                {
                    var srcRow = inputRgbaMat.Data + y * rowPitch;
                    var destRowOffset = y * width * bytesPerPixel;
                    Marshal.Copy(srcRow, data, destRowOffset, width * bytesPerPixel);
                }
            }
            else
            {
                Marshal.Copy(inputRgbaMat.Data, data, 0, data.Length);
            }

            _device.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
            return data;
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _desktopImageTexture?.Dispose();
            _device?.Dispose();
        }

        /// <summary>
        ///  捕获图像大小,默认为捕获源大小
        /// </summary>
        public Size CaptureSize { get; set; }

        /// <summary>
        /// 新帧到达事件
        /// </summary>
        public event EventHandler<CaptureFrame>? FrameArrived;

        /// <summary>
        /// 捕获项关闭事件
        /// </summary>
        public event EventHandler<GraphicsCaptureItem>? ItemClosed;

        #region 私有字段

        private readonly Device _device;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;
        private readonly Texture2D _desktopImageTexture;

        #endregion
    }
}