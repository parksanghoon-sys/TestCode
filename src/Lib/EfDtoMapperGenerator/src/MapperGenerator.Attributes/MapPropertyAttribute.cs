namespace MapperGenerator.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public sealed class MapPropertyAttribute : Attribute
{
    public string TargetName { get; }

    public MapPropertyAttribute(string targetName)
    {
        TargetName = targetName;
    }
}
