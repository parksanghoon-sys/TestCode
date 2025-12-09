using System;
using System.IO;
using System.Text;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 바이너리 읽기 헬퍼 (Wrapper Pattern)
    /// </summary>
    public class BinaryReader : IDisposable
    {
        private readonly Stream _stream;
        private readonly Encoding _encoding;
        private readonly bool _leaveOpen;
        private bool _disposed;

        public BinaryReader(Stream stream, Encoding encoding = null, bool leaveOpen = false)
        {
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
            _encoding = encoding ?? Encoding.UTF8;
            _leaveOpen = leaveOpen;
        }

        public long Position => _stream.Position;

        public byte ReadByte()
        {
            var b = _stream.ReadByte();
            if (b == -1)
                throw new EndOfStreamException();
            return (byte)b;
        }

        public short ReadInt16()
        {
            var bytes = new byte[2];
            ReadBytes(bytes, 0, 2);
            return BitConverter.ToInt16(bytes, 0);
        }

        public int ReadInt32()
        {
            var bytes = new byte[4];
            ReadBytes(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        public long ReadInt64()
        {
            var bytes = new byte[8];
            ReadBytes(bytes, 0, 8);
            return BitConverter.ToInt64(bytes, 0);
        }

        public float ReadFloat()
        {
            var bytes = new byte[4];
            ReadBytes(bytes, 0, 4);
            return BitConverter.ToSingle(bytes, 0);
        }

        public double ReadDouble()
        {
            var bytes = new byte[8];
            ReadBytes(bytes, 0, 8);
            return BitConverter.ToDouble(bytes, 0);
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public string ReadString()
        {
            var length = ReadInt32();
            if (length == -1)
                return null;

            if (length == 0)
                return string.Empty;

            var bytes = new byte[length];
            ReadBytes(bytes, 0, length);
            return _encoding.GetString(bytes);
        }

        public byte[] ReadBytes()
        {
            var length = ReadInt32();
            if (length == -1)
                return null;

            if (length == 0)
                return new byte[0];

            var bytes = new byte[length];
            ReadBytes(bytes, 0, length);
            return bytes;
        }

        private void ReadBytes(byte[] buffer, int offset, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var read = _stream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0)
                    throw new EndOfStreamException();
                totalRead += read;
            }
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
