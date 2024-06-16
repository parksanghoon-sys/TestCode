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
                    }
                }
            }
            return null;
        }

        public static T Cast<T>(this object obj) => (T) obj;                            
    }
}
