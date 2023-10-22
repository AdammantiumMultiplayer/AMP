using System.IO;
using System.IO.Compression;

namespace AMP.Compression {

    public class Deflate : Compressor {

        public byte[] Compress(byte[] data) {
            byte[] compressArray = null;
            try {
                using(MemoryStream memoryStream = new MemoryStream()) {
                    using(DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress)) {
                        deflateStream.Write(data, 0, data.Length);
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
                        using(DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress)) {
                            deflateStream.CopyTo(decompressedStream);
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