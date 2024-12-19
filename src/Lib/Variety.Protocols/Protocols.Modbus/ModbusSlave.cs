using Protocols.Modbus.Datas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Protocols.Modbus
{
    /// <summary>
    /// Modbus Slave
    /// </summary>
    public sealed class ModbusSlave : INotifyPropertyChanged
    {
        private ModbusBits coils = new();
        private ModbusBits discreteInputs = new();


        /// <summary>
        /// Coils
        /// </summary>
        public ModbusBits Coils { get => coils; set => this.Set(ref coils, value, PropertyChanged); }
        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
