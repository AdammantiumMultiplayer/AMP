using System.IO;
using System.IO.Compression;

namespace AMP.Compression {

    /// <summary>
    /// Currently not usable cause of the GZip Overhead
    /// </summary>
    public class GZipCompression : Compressor {

        public byte[] Compress(byte[] data) {
            byte[] compressArray = null;
            try {
                using(MemoryStream memoryStream = new MemoryStream()) {
                    using(GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
                        gzipStream.Write(data, 0, data.Length);
                    }
                    compressArray = memoryStream.ToArray();
                }
            } catch {
                return data;
            }
            return compressArray;
        }

        public byte[] Decompress(byte[] data) {
            byte[] decompressedArray = null;
            try {
                using(MemoryStream decompressedStream = new MemoryStream()) {
                    using(MemoryStream compressStream = new MemoryStream(data)) {
                        using(GZipStream gzipStream = new GZipStream(compressStream, CompressionMode.Decompress)) {
                            gzipStream.CopyTo(decompressedStream);
                        }
                    }
                    decompressedArray = decompressedStream.ToArray();
                }
            } catch {
                // do something !
            }

            return decompressedArray;
        }
    }
}
