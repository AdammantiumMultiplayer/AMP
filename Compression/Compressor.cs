namespace AMP.Compression {
    public interface Compressor {

        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] data);

    }
}
