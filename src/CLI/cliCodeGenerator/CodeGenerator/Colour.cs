using System;

namespace CodeGenerator
{   
    [EnumExtensions]
    public enum Colour
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