namespace BytePacketSupport.BytePacketSupport.Services
{
    public abstract class ErrorDetection
    {
        public abstract ReadOnlySpan<byte> Compute(ReadOnlySpan<byte> data);
        public abstract string GetDetectionType();
    }
}
