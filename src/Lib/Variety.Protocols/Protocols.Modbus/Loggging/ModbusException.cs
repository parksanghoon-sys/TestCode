using Protocols.Abstractions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus.Loggging
{
    public class ModbusException : ErrorCodeException<ModbusExceptionCode>
    {
        public ModbusException(ModbusExceptionCode code)
            :base(code)
        {
            
        }
        public ModbusException(ModbusExceptionCode code, Exception innerException)
            : base(code, innerException)
        {

        }
    }
}
