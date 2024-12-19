using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus
{
    /// <summary>
    /// Modbus 슬레이브 서비스
    /// </summary>
    public class ModbusSlaveService : IDisposable, IEnumerable<KeyValuePair<byte, ModbusSlave>>
    {
    }
}
