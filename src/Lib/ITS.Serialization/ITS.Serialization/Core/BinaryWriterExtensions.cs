using System;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// BinaryWriter 생성자 오버로딩을 위한 확장
    /// </summary>
    internal static class BinaryWriterExtensions
    {
        public static BinaryWriter CreateWithLeaveOpen(System.IO.Stream stream, bool leaveOpen)
        {
            return new BinaryWriter(stream);
        }
    }
}
