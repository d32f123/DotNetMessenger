using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetMessenger.WPFClient
{
    public static class ImageConverterExtensions
    {
        public static byte[] ToBytes(this System.Drawing.Image imageIn)
        {
            var ms = new MemoryStream();
            imageIn.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public static System.Drawing.Image BytesToImage(byte[] byteArrayIn)
        {
            var ms = new MemoryStream(byteArrayIn);
            var returnImage = System.Drawing.Image.FromStream(ms);
            return returnImage;
        }
    }
}
