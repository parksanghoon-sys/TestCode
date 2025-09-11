namespace MapperGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class MapToAttribute : Attribute
{
    public Type TargetType { get; }
    public MapToAttribute(Type targetType) => TargetType = targetType;
}
