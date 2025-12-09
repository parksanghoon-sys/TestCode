using System;
using System.IO;
using System.Text;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 바이너리 쓰기 헬퍼 (Wrapper Pattern)
    /// </summary>
    public class BinaryWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly bool _leaveOpen;
        private bool _disposed;

        public BinaryWriter(Stream stream, Encoding encoding = null, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _encoding = encoding ?? Encoding.UTF8;
            _leaveOpen = leaveOpen;
        }

        public long Position => _stream.Position;

        public void WriteByte(byte value)
        {
            _stream.WriteByte(value);
        }

        public void WriteInt16(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteInt32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteInt64(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBool(bool value)
        {
            _stream.WriteByte(value ? (byte)1 : (byte)0);
        }

        public void WriteString(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            var bytes = _encoding.GetBytes(value);
            WriteInt32(bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteBytes(byte[] value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            WriteInt32(value.Length);
            _stream.Write(value, 0, value.Length);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (!_leaveOpen)
                    _stream?.Dispose();
                _disposed = true;
            }
        }
    }
}
