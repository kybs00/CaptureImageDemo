using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace WgcCaptureDemo
{
    /// <summary>
    /// Capture辅助类
    /// </summary>
    public static class CaptureUtils
    {
        static readonly Guid GraphicsCaptureItemGuid = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr window, [In] ref Guid iid);

            IntPtr CreateForMonitor([In] IntPtr monitor, [In] ref Guid iid);
        }


        [Guid("00000035-0000-0000-C000-000000000046")]
        internal unsafe struct IActivationFactoryVftbl
        {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
            public readonly IInspectable.Vftbl IInspectableVftbl;
            private readonly void* _ActivateInstance;
#pragma warning restore

            public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ActivateInstance => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_ActivateInstance;
        }

        internal class Platform
        {
            [DllImport("api-ms-win-core-com-l1-1-0.dll")]
            internal static extern int CoDecrementMTAUsage(IntPtr cookie);

            [DllImport("api-ms-win-core-com-l1-1-0.dll")]
            internal static extern unsafe int CoIncrementMTAUsage(IntPtr* cookie);

            [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
            internal static extern unsafe int RoGetActivationFactory(IntPtr runtimeClassId, ref Guid iid, IntPtr* factory);
        }

        private static class WinRtModule
        {
            private static readonly Dictionary<string, ObjectReference<IActivationFactoryVftbl>> Cache = new Dictionary<string, ObjectReference<IActivationFactoryVftbl>>();

            public static ObjectReference<IActivationFactoryVftbl> GetActivationFactory(string runtimeClassId)
            {
                lock (Cache)
                {
                    if (Cache.TryGetValue(runtimeClassId, out var factory))
                        return factory;

                    var m = MarshalString.CreateMarshaler(runtimeClassId);

                    try
                    {
                        var instancePtr = GetActivationFactory(MarshalString.GetAbi(m));

                        factory = ObjectReference<IActivationFactoryVftbl>.Attach(ref instancePtr);
                        Cache.Add(runtimeClassId, factory);

                        return factory;
                    }
                    finally
                    {
                        m.Dispose();
                    }
                }
            }

            private static unsafe IntPtr GetActivationFactory(IntPtr hstrRuntimeClassId)
            {
                if (s_cookie == IntPtr.Zero)
                {
                    lock (s_lock)
                    {
                        if (s_cookie == IntPtr.Zero)
                        {
                            IntPtr cookie;
                            Marshal.ThrowExceptionForHR(Platform.CoIncrementMTAUsage(&cookie));

                            s_cookie = cookie;
                        }
                    }
                }

                Guid iid = typeof(IActivationFactoryVftbl).GUID;
                IntPtr instancePtr;
                int hr = Platform.RoGetActivationFactory(hstrRuntimeClassId, ref iid, &instancePtr);

                if (hr == 0)
                    return instancePtr;

                throw new Win32Exception(hr);
            }

            public static bool ResurrectObjectReference(IObjectReference objRef)
            {
                var disposedField = objRef.GetType().GetField("disposed", BindingFlags.NonPublic | BindingFlags.Instance)!;
                if (!(bool)disposedField.GetValue(objRef)!)
                    return false;
                disposedField.SetValue(objRef, false);
                GC.ReRegisterForFinalize(objRef);
                return true;
            }

            private static IntPtr s_cookie;
            private static readonly object s_lock = new object();
        }
        
        /// <summary>
        /// 根据窗口句柄创建 GraphicsCaptureItem 实例。
        /// </summary>
        /// <param name="hWnd">窗口句柄，指定要捕获的窗口。</param>
        /// <returns>返回一个 GraphicsCaptureItem 实例，表示捕获的窗口。</returns>
        /// <exception cref="Exception">当窗口不存在时抛出异常</exception>
        public static GraphicsCaptureItem CreateItemForWindow(IntPtr hWnd)
        {
            var factory = WinRtModule.GetActivationFactory("Windows.Graphics.Capture.GraphicsCaptureItem");
            var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForWindow(hWnd, GraphicsCaptureItemGuid);
            var item = GraphicsCaptureItem.FromAbi(itemPointer);
            return item;
        }

        /// <summary>
        /// 根据显示器句柄创建 GraphicsCaptureItem 实例。
        /// </summary>
        /// <param name="hmon">显示器句柄，指定要捕获的显示器。</param>
        /// <returns>显示器句柄，指定要捕获的显示器。</returns>
        /// <exception cref="Exception">当显示器句柄错误时抛出异常。</exception>
        public static GraphicsCaptureItem CreateItemForMonitor(IntPtr hmon)
        {
            var factory = WinRtModule.GetActivationFactory("Windows.Graphics.Capture.GraphicsCaptureItem");
            var interop = factory.AsInterface<IGraphicsCaptureItemInterop>();
            var itemPointer = interop.CreateForMonitor(hmon, GraphicsCaptureItemGuid);
            var item = GraphicsCaptureItem.FromAbi(itemPointer);
            return item;
        }
    }
}
