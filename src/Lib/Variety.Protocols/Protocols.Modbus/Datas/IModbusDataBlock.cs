namespace Protocols.Modbus.Datas
{
    /// <summary>
    /// Modbus Data 블록 인터페이스
    /// </summary>
    /// <typeparam name="TData">데이터 형식</typeparam>
    /// <typeparam name="TRawData">Raw 데이터 형식</typeparam>
    public interface IModbusDataBlock<TData, TRawData> : IEnumerable<TData>
    {        
        ushort StartAddress { get; }
        ushort EndAddress { get; }
        ushort Count { get; }
        /// <summary>
        /// Raw 데이터
        /// </summary>
        IReadOnlyList<TRawData> RawData { get; }
        /// <summary>
        /// 단위 데이터당 Raw 데이터 갯수
        /// </summary>
        int NumberOfUnit { get; }
        /// <summary>
        /// 특정 주소의 데이터 가져오기
        /// </summary>
        /// <param name="address">주소</param>
        /// <returns>데이터</returns>
        TData this[ushort address] { get; }
    }
}
