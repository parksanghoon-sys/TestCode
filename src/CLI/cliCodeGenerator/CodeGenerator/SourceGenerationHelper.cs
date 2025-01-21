using System.Collections.Immutable;
using System.Drawing;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeGenerator
{
}
public static class Constants
    {
        public const string Version = "1.0.0-beta11";

        public const string EnabledPropertyName = "EnableEnumGeneratorInterceptor";
    }
    public static class SourceGenerationHelper
    {
        public const string Attribute = @"

namespace CodeGenerator
{
    [System.AttributeUsage(System.AttributeTargets.Enum)]
    public class EnumExtensionsAttribute : System.Attribute
    {
    }
}";

        public static string GenerateExtensionClass(List<EnumToGenerate> enumsToGenerate)
        {
            var sb = new StringBuilder();
            sb.Append(@"
namespace NetEscapades.EnumGenerators
{
    public static partial class EnumExtensions
    {");
            foreach (var enumToGenerate in enumsToGenerate)
            {
                sb.Append(@"
                public static string ToStringFast(this ").Append(enumToGenerate.Name).Append(@" value)
                    => value switch
                    {");
                foreach (var member in enumToGenerate.Values)
                {
                    sb.Append(@"
                ").Append(enumToGenerate.Name).Append('.').Append(member)
                        .Append(" => nameof(")
                        .Append(enumToGenerate.Name).Append('.').Append(member).Append("),");
                }

                sb.Append(@"
                    _ => value.ToString(),
                };
");
            }

            sb.Append(@"
    }
}");

            return sb.ToString();
        }
    }
}
