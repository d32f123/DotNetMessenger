using System;
using System.Text;

namespace DotNetMessenger.WebApi.Extensions
{
    public static class StringExtensions
    {
        public static string FromBase64ToString(this string convertString)
        {
            byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(convertString);
            }
            catch (FormatException)
            {
                return null;
            }

            var encoding = Encoding.ASCII;
            // Make a writable copy of the encoding to enable setting a decoder fallback.
            encoding = (Encoding)encoding.Clone();
            // Fail on invalid bytes rather than silently replacing and continuing.
            encoding.DecoderFallback = DecoderFallback.ExceptionFallback;

            try
            {
                return encoding.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                return null;
            }
        }
    }
}