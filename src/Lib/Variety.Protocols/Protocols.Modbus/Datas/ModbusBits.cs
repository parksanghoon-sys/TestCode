﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Protocols.Modbus.Datas
{
    /// <summary>
    /// Modbus Bit(Coil, Discrete Input) 데이터 셋
    /// </summary>
    public class ModbusBits : ModbusDataSet<bool, bool>
    {
        /// <summary>
        /// 데이터셋 열거자 가져요기
        /// </summary>
        /// <returns>데이터셋 열거</returns>
        public override IEnumerator<KeyValuePair<ushort, bool>> GetEnumerator()
        {
            foreach (ModbusBitDataBlock dataBlock in DataBlocks)
            {
                ushort address = dataBlock.StartAddress;
                foreach (var value in dataBlock)
                    yield return new KeyValuePair<ushort, bool>(address++, value);
            }
        }
        /// <summary>
        /// 주소 할당
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="data">데이터 배열</param>
        public void Allocate(ushort startAddress, bool[] data)
        {
            AllocateCore(new ModbusBitDataBlock(startAddress, data));
        }
        /// <summary>
        /// 반복 할당
        /// </summary>
        /// <param name="startAddress">시작 주소</param>
        /// <param name="count">개수</param>
        /// <param name="value">반복할 값</param>
        public void AllocateRepeat(ushort startAddress, ushort count, bool value) => Allocate(startAddress, Enumerable.Repeat(value, count).ToArray());
        /// <summary>
        /// 전체 주소 할당
        /// </summary>
        /// <param name="value">전체 주소에 할당할 값</param>
        public void AllocateAll(bool value) => Allocate(0, Enumerable.Repeat(value, ushort.MaxValue).ToArray());
        internal override ModbusDataBlock<bool, bool> CreateDataBlock(ushort startAddress, bool[] values)
                        => new ModbusBitDataBlock(startAddress, values);
        
        class ModbusBitDataBlock : ModbusDataBlock<bool, bool>
        {
            private ushort startAddress = 0;

            public override ushort StartAddress
            {
                get => startAddress;
                set
                {
                    if (value > EndAddress)
                        value = EndAddress;

                    if(startAddress != value)
                    {
                        if(startAddress > value)
                        {
                            if(startAddress > value)
                            {
                                rawData = Enumerable.Repeat(false, startAddress - value).Concat(rawData).ToArray();
                            }
                            else
                            {
                                rawData = rawData.Skip(value - startAddress).ToArray();
                            }
                            startAddress = value;
                        }
                    }
                }
            }
            public override ushort EndAddress { get => (ushort)(StartAddress + Count - 1); set => Array.Resize(ref rawData, Math.Max(value - StartAddress + 1, 0)); }
            public override ushort Count => (ushort)rawData.Length;
            public override int NumberOfUnit => 1;
            public override bool this[ushort address]
            {
                get
                {
                    if(address >= StartAddress && address <= EndAddress)
                    {
                        return rawData[(address - StartAddress) * NumberOfUnit];
                    }
                    else
                    {
                        throw new ModbusException(ModbusExceptionCode.IllegalDataAddress);
                    }
                }
                set
                {
                    rawData[(address - StartAddress) * NumberOfUnit] = value;
                }
            }
            public ModbusBitDataBlock(ushort startAddress, bool[] value)
            {
                this.startAddress = startAddress;
                rawData = value;
            }
            public override IEnumerator<bool> GetEnumerator()
            {
                foreach (var value in rawData)
                {
                    yield return value;
                }
            }
        }
    }
}
