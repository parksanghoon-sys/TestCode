using Protocols.Abstractions.Channels;
using Protocols.Abstractions.Logging;
using Protocols.Modbus.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus
{
    public class ModbusException : ErrorCodeException<ModbusExceptionCode>
    {
        public ModbusException(ModbusExceptionCode code)
            : base(code)
        {

        }
        public ModbusException(ModbusExceptionCode code, Exception innerException)
            : base(code, innerException)
        {

        }
    }
   
}
