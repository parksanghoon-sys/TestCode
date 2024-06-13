using System.Buffers;

namespace BytePacketSupport.Extentions
{
    public ref struct ReservedSpan
    {
        private int _length;
        private ArrayBufferWriter<byte> _list;
        public ReservedSpan(ArrayBufferWriter<byte> list, int length)
        {
            _length = length;
            _list = list;
            Span = list.GetSpan(_length);
        }
        public Span<byte> Span { get; private set; }
        public static implicit operator Span<byte> (ReservedSpan span) => span.Span;
        public void Dispose() => _list.Advance(_length);
    }
}
