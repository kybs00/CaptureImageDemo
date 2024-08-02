using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WgcCaptureDemo
{
    /// <summary>
    /// 捕获图像帧信息
    /// </summary>
    public class CaptureFrame
    {
        /// <summary>
        /// 创建一帧图像信息
        /// </summary>
        /// <param name="size"></param>
        /// <param name="data"></param>
        public CaptureFrame(Size size, byte[] data)
        {
            Size = size;
            Data = data;
        }

        /// <summary>
        /// 原始像素数据
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// 捕获图像大小
        /// </summary>
        public Size Size { get; set; }
    }
}
