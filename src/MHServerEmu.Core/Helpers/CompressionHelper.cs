﻿using Free.Ports.zLib;

namespace MHServerEmu.Core.Helpers
{
    public static class CompressionHelper
    {
        /// <summary>
        /// Compresses the provided buffer using zlib.
        /// </summary>
        public static byte[] ZLibDeflate(byte[] buffer)
        {
            // This is used for compressing the achievement database dump before sending it to clients.
            // We need to compress specifically with a port of zlib rather than various alternatives like SharpZipLib
            // to produce the same result as the original. zlib.compress() in Python also produces the result we need.

            using (MemoryStream ms = new())
            using (ZStreamWriter writer = new(ms))
            {
                writer.Write(buffer);
                writer.Close();
                return ms.ToArray();
            }
        }
    }
}
