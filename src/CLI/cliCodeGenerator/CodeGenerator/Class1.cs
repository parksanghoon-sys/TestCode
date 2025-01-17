using System;

namespace CodeGenerator
{
    public readonly struct EnumToGenerator
    {
        public readonly string Name;
        public readonly List<string> Values;

        public EnumToGenerator(string name, List<string> values)
        {
            Name = name;
            Values = values;
        }
    }
    [EnumExtensions]
    public enum Color
    {
        Red = 0,
        Blue = 1
    }
    public static class SourceGeneratorationHelper
    {
        public const string Attribute = @"
    namespace CodeGenerator
    {
        [AttributeUsage(AttributeTargets.Enum)]
        public class EnumExtensionsAttribute : Attribute
        {
        }
    }";
    }
}