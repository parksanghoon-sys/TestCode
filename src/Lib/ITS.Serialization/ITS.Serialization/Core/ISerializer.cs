using System;
using System.IO;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 직렬화 인터페이스 (Strategy Pattern)
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// 객체를 바이트 배열로 직렬화
        /// </summary>
        byte[] Serialize<T>(T obj) where T : class;

        /// <summary>
        /// 객체를 스트림에 직렬화
        /// </summary>
        void Serialize<T>(Stream stream, T obj) where T : class;

        /// <summary>
        /// 바이트 배열을 객체로 역직렬화
        /// </summary>
        T Deserialize<T>(byte[] data) where T : class;

        /// <summary>
        /// 스트림에서 객체를 역직렬화
        /// </summary>
        T Deserialize<T>(Stream stream) where T : class;
    }
}
