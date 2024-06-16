using BytePacketSupport.Attributes;
using BytePacketSupport.BytePacketSupport.Converter;
using System.Reflection;

namespace BytePacketSupport
{
    public static class PacketParse
    {
        public static byte[] Serialize<TSource>(TSource source) where TSource : class
        {
            FieldInfo[] fields =  typeof(TSource).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            List<byte> result = new List<byte>();

            foreach(FieldInfo field in fields)
            {
                object value = field.GetValue(source);

                if (field.FieldType == typeof(string))
                {
                    result.AddRange(ByteConverter.GetBytes(value.Cast<string>()));
                }
                else if (field.FieldType == typeof(short))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));
                    if(attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((short)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((short)value, attribute.Endian));
                }
                else if (field.FieldType == typeof(int))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));

                    if (attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((int)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((int)value, attribute.Endian));
                }
                else if (field.FieldType == typeof(long))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));

                    if (attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((long)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((long)value, attribute.Endian));
                }
                else if (field.FieldType == typeof(ushort))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));

                    if (attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((ushort)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((ushort)value, attribute.Endian));
                }
                else if (field.FieldType == typeof(uint))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));

                    if (attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((uint)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((uint)value, attribute.Endian));
                }
                else if (field.FieldType == typeof(ulong))
                {
                    var attribute = ((EndianAttribute)Attribute.GetCustomAttribute(field, typeof(EndianAttribute)));

                    if (attribute == null)
                    {
                        result.AddRange(ByteConverter.GetBytes((ulong)value));
                        continue;
                    }
                    result.AddRange(ByteConverter.GetBytes((ulong)value, attribute.Endian));
                }
                else if(field.FieldType == typeof(byte))
                {
                    result.Add(value.Cast<byte>());
                }
                else if(field.FieldType == typeof(byte[]))
                {
                    result.AddRange(value.Cast<byte[]>());
                }
                else if(field.FieldType == typeof(List<byte>))
                {
                    result.AddRange(value.Cast<List<byte>>());
                }
            }
            return result.ToArray();
        }

        public static T Cast<T>(this object obj) => (T) obj;                            
    }
}
