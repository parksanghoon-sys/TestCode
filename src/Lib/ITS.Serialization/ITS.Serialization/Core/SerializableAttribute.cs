using System;

namespace ITS.Serialization.Core
{
    /// <summary>
    /// 직렬화 가능한 클래스를 표시하는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class SerializableAttribute : Attribute
    {
    }

    /// <summary>
    /// 직렬화할 멤버를 표시하는 Attribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SerializableMemberAttribute : Attribute
    {
        /// <summary>
        /// 직렬화 순서 (1부터 시작)
        /// </summary>
        public int Order { get; set; }

        public SerializableMemberAttribute(int order)
        {
            Order = order;
        }
    }
}
