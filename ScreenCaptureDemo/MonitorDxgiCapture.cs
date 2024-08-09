using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Media;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Size = System.Drawing.Size;

namespace DxgiScreenCaptureDemo
{
    /// <summary>
    /// 提供对特定桌面（即一台显示器）逐帧更新的访问，以及图像和光标信息
    /// </summary>
    internal class MonitorDxgiCapture : IDisposable
    {
       
        /// <summary>
        /// 复制指定监视器的输出
        /// </summary>
        /// <param name="whichMonitor">要复制的输出设备（即监视器）。以零开始，应于主监视器.</param>
        public MonitorDxgiCapture(int whichMonitor = 0) : this(0, whichMonitor) { }

        /// <summary>
        /// 在指定的显卡适配器上复制指定监视器的输出
        /// </summary>
        /// <remarks>1获取适配器；2获取输出；3创建设备；4创建复制输出； 5创建图像纹理</remarks>
        /// <param name="whichGraphicsCardAdapter">显卡适配器</param>
        /// <param name="whichOutputDevice">要复制的输出设备（即监视器）。以零开始，应于主监视器.</param>
        public MonitorDxgiCapture(int whichGraphicsCardAdapter, int whichOutputDevice)
        {
            //根据显卡编号获取适配器
            var primaryAdapter = new Factory1().GetAdapter1(whichGraphicsCardAdapter);
            // 根据显示适配器和输出设备获取输出
            Output primaryOutput = primaryAdapter.GetOutput(whichOutputDevice);
            // 获取输出对象的描述信息
            var mOutputDesc = primaryOutput.Description;
            _screenSize = new Size
            {
                Width = Math.Abs(mOutputDesc.DesktopBounds.Right - mOutputDesc.DesktopBounds.Left),
                Height = Math.Abs(mOutputDesc.DesktopBounds.Bottom - mOutputDesc.DesktopBounds.Top)
            };
            SourceSize = _screenSize;
            CaptureSize = SourceSize;
            //根据显卡适配器(视频卡)创建Direct3D设备
            _mDevice = new Device(primaryAdapter);
            // 根据输出和设备创建输出Duplication
            _mDeskDupl = primaryOutput.QueryInterface<Output1>().DuplicateOutput(_mDevice);
            // 创建共享资源
            _desktopImageTexture = CreateTexture2D(_mDevice, _screenSize);
        }

        /// <summary>
        /// 创建2D纹理
        /// </summary>
        /// <remarks>共享资源</remarks>
        /// <param name="mDevice"></param>
        /// <param name="screenSize"></param>
        /// <returns></returns>
        private Texture2D CreateTexture2D(Device mDevice, Size screenSize)
        {
            return new Texture2D(mDevice, new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = screenSize.Width,
                Height = screenSize.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });
        }

        #region 私有方法

    [HandleProcessCorruptedStateExceptions]
    private CaptureFrame CaptureFrame()
    {
        try
        {
            var data = new byte[CaptureSize.Width * CaptureSize.Height * 4];
            var result = _mDeskDupl.TryAcquireNextFrame(TimeOut, out _, out var desktopResource);
            if (result.Failure) return null;

            using var tempTexture = desktopResource?.QueryInterface<Texture2D>();
            //拷贝图像纹理：GPU硬件加速的纹理复制
            _mDevice.ImmediateContext.CopyResource(tempTexture, _desktopImageTexture);
            desktopResource?.Dispose();

            var desktopSource = _mDevice.ImmediateContext.MapSubresource(_desktopImageTexture, 0, MapMode.Read, MapFlags.None);
            using var inputRgbaMat = new Mat(_screenSize.Height, _screenSize.Width, MatType.CV_8UC4, desktopSource.DataPointer);
            if (CaptureSize.Width != _screenSize.Width || CaptureSize.Height != _screenSize.Height)
            {
                var size = new OpenCvSharp.Size(CaptureSize.Width, CaptureSize.Height);
                Cv2.Resize(inputRgbaMat, inputRgbaMat, size, interpolation: InterpolationFlags.Linear);
            }
            Marshal.Copy(inputRgbaMat.Data, data, 0, data.Length);

            var captureFrame = new CaptureFrame(CaptureSize, data);
            _mDevice.ImmediateContext.UnmapSubresource(_desktopImageTexture, 0);
            //释放帧
            _mDeskDupl.ReleaseFrame();
            return captureFrame;
        }
        catch (AccessViolationException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }


        #endregion

        #region 公共接口实现

        /// <summary>
        /// 新帧到达事件
        /// </summary>
        public event EventHandler<CaptureFrame> FrameArrived;

        /// <summary>
        /// 开始捕获
        /// </summary>
        public void StartCapture(bool startMonitor = true)
        {
            _isManualCaptureStop = false;
            _cancellationTokenSource = new CancellationTokenSource();
            if (!startMonitor) return;
            Task.Run(() =>
            {
                try
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        var captureFrame = CaptureFrame();
                        if (captureFrame != null)
                        {
                            FrameArrived?.Invoke(this, captureFrame);
                        }
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// 停止捕获
        /// </summary>
        public void StopCapture()
        {
            _cancellationTokenSource?.Cancel();
            Dispose();
            _isManualCaptureStop = true;
        }

        /// <summary>
        /// 获取下一帧图像
        /// </summary>
        /// <param name="captureFrame"></param>
        /// <returns></returns>
        public bool TryGetNextFrame(out CaptureFrame captureFrame)
        {
            captureFrame = null;

            if (_isManualCaptureStop) return false;
            _cancellationTokenSource?.Cancel();

            captureFrame = CaptureFrame();
            return captureFrame != null;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _mDevice?.Dispose();
            _mDeskDupl?.Dispose();
            _desktopImageTexture?.Dispose();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 捕获图像大小,默认为捕获源大小,建议按原始比例设置
        /// </summary>
        public Size CaptureSize { get; set; }

        /// <summary>
        /// 捕获超时时间
        /// </summary>
        public int TimeOut { get; set; } = 100;

        /// <summary>
        /// 捕获源大小
        /// </summary>
        public Size SourceSize { get; }

        #endregion

        #region 私有字段

        // 设备接口表示一个虚拟适配器
        private readonly Device _mDevice;

        // 复制输出设备
        private readonly OutputDuplication _mDeskDupl;

        //图像纹理
        private readonly Texture2D _desktopImageTexture;

        //屏幕大小
        private readonly Size _screenSize;

        private CancellationTokenSource _cancellationTokenSource;

        private bool _isManualCaptureStop;

        #endregion
    }
}
