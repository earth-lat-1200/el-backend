using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Compression
{
    internal class CompressionHelper
    {
        internal static byte[] CompressBytes(byte[] bytes)
        {
            //Simply write the bytes to memory using the .Net compression stream
            var output = new MemoryStream();
            var gzip = new GZipStream(output, CompressionMode.Compress, true);
            gzip.Write(bytes, 0, bytes.Length);
            gzip.Close();
            return output.ToArray();
        }

        internal static byte[] DecompressBytes(byte[] bytes)
        {
            //Use the .Net decompression stream in memory
            MemoryStream input = new MemoryStream();
            input.Write(bytes, 0, bytes.Length);
            input.Position = 0;

            GZipStream gzip = new GZipStream(input, CompressionMode.Decompress, true);
            MemoryStream output = new MemoryStream();

            byte[] buff = new byte[64]; 
            int read = -1;
            read = gzip.Read(buff, 0, buff.Length);
            while (read > 0)
            {
                output.Write(buff, 0, read);
                read = gzip.Read(buff, 0, buff.Length);
            }

            gzip.Close();

            return output.ToArray();
        }
    }
}
