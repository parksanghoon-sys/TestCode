using TcpSimulator.Domain.Interfaces;

namespace TcpSimulator.Infrastructure.Buffers;

public sealed class StreamBufferAssembler : IDisposable
{
    private readonly IProtocolSerializer _serializer;
    private readonly IBufferManager _bufferManager;
    private RentedBuffer<byte>? _currentBuffer;
    private int _currentPosition;

    public StreamBufferAssembler(IProtocolSerializer serializer, IBufferManager bufferManager)
    {
        _serializer = serializer;
        _bufferManager = bufferManager;
    }

    public IMessage? TryAssembleMessage(ReadOnlySpan<byte> newData)
    {
        // Allocate initial buffer if needed
        if (_currentBuffer == null)
        {
            _currentBuffer = _bufferManager.Rent(8192);
            _currentPosition = 0;
        }

        // Add new data
        if (newData.Length > 0)
        {
            if (_currentPosition + newData.Length > _currentBuffer.Value.Length)
            {
                // Need to expand buffer
                var newBuffer = _bufferManager.Rent(_currentPosition + newData.Length);
                _currentBuffer.Value.Span.Slice(0, _currentPosition).CopyTo(newBuffer.Span);
                _bufferManager.Return(_currentBuffer.Value);
                _currentBuffer = newBuffer;
            }

            newData.CopyTo(_currentBuffer.Value.Span.Slice(_currentPosition));
            _currentPosition += newData.Length;
        }

        // Check if we have a complete message
        if (!_serializer.IsCompleteMessage(
            _currentBuffer.Value.Span.Slice(0, _currentPosition), out int messageLength))
        {
            return null; // Not yet complete
        }

        // Deserialize the message
        var message = _serializer.Deserialize(
            _currentBuffer.Value.Span.Slice(0, messageLength));

        // Remove used data
        if (_currentPosition > messageLength)
        {
            // Move remaining data to front
            _currentBuffer.Value.Span.Slice(messageLength, _currentPosition - messageLength)
                .CopyTo(_currentBuffer.Value.Span);
            _currentPosition -= messageLength;
        }
        else
        {
            // All data used
            _currentPosition = 0;
        }

        return message;
    }

    public void Dispose()
    {
        if (_currentBuffer != null)
        {
            _bufferManager.Return(_currentBuffer.Value);
            _currentBuffer = null;
        }
    }
}
