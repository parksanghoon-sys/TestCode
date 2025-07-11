using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace FastMapper.SourceGenerator;

/// <summary>
/// FastMapper Source Generator - 컴파일 타임에 매핑 코드를 생성
/// </summary>
[Generator]
public sealed class FastMapperSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // MapTo 어트리뷰트가 적용된 클래스들을 찾아서 매핑 코드 생성
        var mappingCandidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        context.RegisterSourceOutput(mappingCandidates.Collect(), 
            static (spc, source) => Execute(source!, spc));
    }

    /// <summary>
    /// 생성 대상이 되는 구문인지 확인
    /// </summary>
    private static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration &&
               classDeclaration.AttributeLists
                   .SelectMany(al => al.Attributes)
                   .Any(attr => attr.Name.ToString().Contains("MapTo"));
    }

    /// <summary>
    /// 시맨틱 모델에서 매핑 정보 추출
    /// </summary>
    private static MappingInfo? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
            return null;

        var mappingInfos = new List<MappingTargetInfo>();

        // MapTo 어트리뷰트들을 분석
        foreach (var attributeData in classSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.Name != "MapToAttribute")
                continue;

            if (attributeData.ConstructorArguments.Length > 0)
            {
                var targetType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                if (targetType is null) continue;

                var mappingTarget = new MappingTargetInfo
                {
                    TargetType = targetType,
                    ProfileName = GetNamedArgument(attributeData, "ProfileName") as string,
                    IsBidirectional = GetNamedArgument(attributeData, "IsBidirectional") as bool? ?? false,
                    OptimizationLevel = GetNamedArgument(attributeData, "OptimizationLevel")?.ToString() ?? "Balanced"
                };

                mappingInfos.Add(mappingTarget);
            }
        }

        if (!mappingInfos.Any())
            return null;

        return new MappingInfo
        {
            SourceType = classSymbol,
            TargetMappings = mappingInfos,
            Properties = GetPropertyMappings(classSymbol)
        };
    }

    /// <summary>
    /// 어트리뷰트에서 명명된 인수 값 가져오기
    /// </summary>
    private static object? GetNamedArgument(AttributeData attributeData, string name)
    {
        return attributeData.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == name)
            .Value.Value;
    }

    /// <summary>
    /// 속성 매핑 정보 수집
    /// </summary>
    private static List<PropertyMappingInfo> GetPropertyMappings(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyMappingInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var mappingInfo = new PropertyMappingInfo
            {
                SourceProperty = member,
                TargetPropertyName = member.Name
            };

            // MapProperty 어트리뷰트 분석
            var mapPropertyAttr = member.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "MapPropertyAttribute");

            if (mapPropertyAttr is not null)
            {
                mappingInfo.TargetPropertyName = GetNamedArgument(mapPropertyAttr, "TargetPropertyName") as string ?? member.Name;
                mappingInfo.Ignore = GetNamedArgument(mapPropertyAttr, "Ignore") as bool? ?? false;
                mappingInfo.ConverterMethod = GetNamedArgument(mapPropertyAttr, "ConverterMethod") as string;
                mappingInfo.ConditionMethod = GetNamedArgument(mapPropertyAttr, "ConditionMethod") as string;
                mappingInfo.DefaultValue = GetNamedArgument(mapPropertyAttr, "DefaultValue");
                mappingInfo.ValidatorMethod = GetNamedArgument(mapPropertyAttr, "ValidatorMethod") as string;
            }

            properties.Add(mappingInfo);
        }

        return properties;
    }

    /// <summary>
    /// 매핑 코드 생성 실행
    /// </summary>
    private static void Execute(ImmutableArray<MappingInfo> mappingInfos, SourceProductionContext context)
    {
        if (mappingInfos.IsDefaultOrEmpty)
            return;

        foreach (var mappingInfo in mappingInfos)
        {
            var sourceCode = GenerateMappingCode(mappingInfo);
            var fileName = $"{mappingInfo.SourceType.Name}Mapper.g.cs";
            
            context.AddSource(fileName, SourceText.From(sourceCode, Encoding.UTF8));
        }
    }

    /// <summary>
    /// 실제 매핑 코드 생성
    /// </summary>
    private static string GenerateMappingCode(MappingInfo mappingInfo)
    {
        var sourceType = mappingInfo.SourceType;
        var namespaceName = sourceType.ContainingNamespace.ToDisplayString();
        
        var code = new StringBuilder();
        
        // 파일 헤더
        code.AppendLine("// <auto-generated />");
        code.AppendLine("#nullable enable");
        code.AppendLine("using System;");
        code.AppendLine("using System.Collections.Generic;");
        code.AppendLine("using System.Linq;");
        code.AppendLine("using System.Threading;");
        code.AppendLine("using System.Threading.Tasks;");
        code.AppendLine("using FastMapper.Core.Abstractions;");
        code.AppendLine("using FastMapper.Core.Common;");
        code.AppendLine();

        // 네임스페이스 시작
        code.AppendLine($"namespace {namespaceName}.Generated;");
        code.AppendLine();

        foreach (var targetMapping in mappingInfo.TargetMappings)
        {
            GenerateMapperClass(code, sourceType, targetMapping, mappingInfo.Properties);
        }

        return code.ToString();
    }

    /// <summary>
    /// 매퍼 클래스 생성
    /// </summary>
    private static void GenerateMapperClass(StringBuilder code, INamedTypeSymbol sourceType, 
        MappingTargetInfo targetMapping, List<PropertyMappingInfo> properties)
    {
        var sourceTypeName = sourceType.Name;
        var targetTypeName = targetMapping.TargetType.Name;
        var mapperClassName = $"{sourceTypeName}To{targetTypeName}Mapper";

        // 클래스 선언
        code.AppendLine($"/// <summary>");
        code.AppendLine($"/// {sourceTypeName}에서 {targetTypeName}으로의 고성능 매퍼");
        code.AppendLine($"/// </summary>");
        code.AppendLine($"public sealed class {mapperClassName} : IMapper<{sourceTypeName}, {targetTypeName}>");
        
        if (targetMapping.IsBidirectional)
        {
            code.AppendLine($"    , IBidirectionalMapper<{sourceTypeName}, {targetTypeName}>");
        }
        
        code.AppendLine("{");

        // Map 메서드 구현
        GenerateMapMethod(code, sourceType, targetMapping.TargetType, properties);
        
        // MapCollection 메서드 구현
        GenerateMapCollectionMethod(code, sourceTypeName, targetTypeName);
        
        // MapCollectionAsync 메서드 구현
        GenerateMapCollectionAsyncMethod(code, sourceTypeName, targetTypeName);

        // 양방향 매핑 메서드 (필요한 경우)
        if (targetMapping.IsBidirectional)
        {
            GenerateBidirectionalMethods(code, sourceType, targetMapping.TargetType);
        }

        code.AppendLine("}");
        code.AppendLine();
    }

    /// <summary>
    /// Map 메서드 생성
    /// </summary>
    private static void GenerateMapMethod(StringBuilder code, INamedTypeSymbol sourceType, 
        INamedTypeSymbol targetType, List<PropertyMappingInfo> properties)
    {
        var sourceTypeName = sourceType.Name;
        var targetTypeName = targetType.Name;

        code.AppendLine($"    /// <summary>");
        code.AppendLine($"    /// {sourceTypeName}을 {targetTypeName}으로 매핑");
        code.AppendLine($"    /// </summary>");
        code.AppendLine($"    public {targetTypeName} Map({sourceTypeName} source)");
        code.AppendLine("    {");
        code.AppendLine("        if (source is null)");
        code.AppendLine($"            throw new ArgumentNullException(nameof(source));");
        code.AppendLine();
        code.AppendLine($"        return new {targetTypeName}");
        code.AppendLine("        {");

        // 속성 매핑 코드 생성
        var validProperties = properties.Where(p => !p.Ignore).ToList();
        for (int i = 0; i < validProperties.Count; i++)
        {
            var property = validProperties[i];
            var isLast = i == validProperties.Count - 1;
            
            GeneratePropertyMapping(code, property, isLast);
        }

        code.AppendLine("        };");
        code.AppendLine("    }");
        code.AppendLine();
    }

    /// <summary>
    /// 속성 매핑 코드 생성
    /// </summary>
    private static void GeneratePropertyMapping(StringBuilder code, PropertyMappingInfo property, bool isLast)
    {
        var comma = isLast ? "" : ",";
        
        if (!string.IsNullOrEmpty(property.ConditionMethod))
        {
            code.AppendLine($"            {property.TargetPropertyName} = {property.ConditionMethod}(source) ? ");
            code.AppendLine($"                {GeneratePropertyValue(property)} : default{comma}");
        }
        else
        {
            code.AppendLine($"            {property.TargetPropertyName} = {GeneratePropertyValue(property)}{comma}");
        }
    }

    /// <summary>
    /// 속성 값 매핑 코드 생성
    /// </summary>
    private static string GeneratePropertyValue(PropertyMappingInfo property)
    {
        if (!string.IsNullOrEmpty(property.ConverterMethod))
        {
            return $"{property.ConverterMethod}(source.{property.SourceProperty.Name})";
        }
        
        if (property.DefaultValue is not null)
        {
            return $"source.{property.SourceProperty.Name} ?? {property.DefaultValue}";
        }
        
        return $"source.{property.SourceProperty.Name}";
    }

    /// <summary>
    /// MapCollection 메서드 생성
    /// </summary>
    private static void GenerateMapCollectionMethod(StringBuilder code, string sourceTypeName, string targetTypeName)
    {
        code.AppendLine($"    /// <summary>");
        code.AppendLine($"    /// 컬렉션 매핑 - 고성능 배치 처리");
        code.AppendLine($"    /// </summary>");
        code.AppendLine($"    public IEnumerable<{targetTypeName}> MapCollection(IEnumerable<{sourceTypeName}> sources)");
        code.AppendLine("    {");
        code.AppendLine("        if (sources is null)");
        code.AppendLine("            throw new ArgumentNullException(nameof(sources));");
        code.AppendLine();
        code.AppendLine("        return sources.Select(Map);");
        code.AppendLine("    }");
        code.AppendLine();
    }

    /// <summary>
    /// MapCollectionAsync 메서드 생성
    /// </summary>
    private static void GenerateMapCollectionAsyncMethod(StringBuilder code, string sourceTypeName, string targetTypeName)
    {
        code.AppendLine($"    /// <summary>");
        code.AppendLine($"    /// 비동기 컬렉션 매핑");
        code.AppendLine($"    /// </summary>");
        code.AppendLine($"    public async Task<IReadOnlyList<{targetTypeName}>> MapCollectionAsync(");
        code.AppendLine($"        IEnumerable<{sourceTypeName}> sources,");
        code.AppendLine("        CancellationToken cancellationToken = default)");
        code.AppendLine("    {");
        code.AppendLine("        if (sources is null)");
        code.AppendLine("            throw new ArgumentNullException(nameof(sources));");
        code.AppendLine();
        code.AppendLine("        var tasks = sources.Select(source => Task.Run(() => Map(source), cancellationToken));");
        code.AppendLine("        var results = await Task.WhenAll(tasks);");
        code.AppendLine("        return results;");
        code.AppendLine("    }");
        code.AppendLine();
    }

    /// <summary>
    /// 양방향 매핑 메서드 생성
    /// </summary>
    private static void GenerateBidirectionalMethods(StringBuilder code, INamedTypeSymbol sourceType, INamedTypeSymbol targetType)
    {
        var sourceTypeName = sourceType.Name;
        var targetTypeName = targetType.Name;

        code.AppendLine($"    /// <summary>");
        code.AppendLine($"    /// {sourceTypeName}에서 {targetTypeName}으로 매핑");
        code.AppendLine($"    /// </summary>");
        code.AppendLine($"    public {targetTypeName} MapTo({sourceTypeName} source) => Map(source);");
        code.AppendLine();
        
        code.AppendLine($"    /// <summary>");
        code.AppendLine($"    /// {targetTypeName}에서 {sourceTypeName}으로 매핑");
        code.AppendLine($"    /// </summary>");
        code.AppendLine($"    public {sourceTypeName} MapFrom({targetTypeName} source)");
        code.AppendLine("    {");
        code.AppendLine("        if (source is null)");
        code.AppendLine($"            throw new ArgumentNullException(nameof(source));");
        code.AppendLine();
        code.AppendLine("        // TODO: 역방향 매핑 구현 필요");
        code.AppendLine($"        throw new NotImplementedException(\"역방향 매핑이 구현되지 않았습니다.\");");
        code.AppendLine("    }");
        code.AppendLine();
    }
}

/// <summary>
/// 매핑 정보 모델
/// </summary>
public sealed class MappingInfo
{
    public INamedTypeSymbol SourceType { get; set; } = null!;
    public List<MappingTargetInfo> TargetMappings { get; set; } = new();
    public List<PropertyMappingInfo> Properties { get; set; } = new();
}

/// <summary>
/// 매핑 대상 정보
/// </summary>
public sealed class MappingTargetInfo
{
    public INamedTypeSymbol TargetType { get; set; } = null!;
    public string? ProfileName { get; set; }
    public bool IsBidirectional { get; set; }
    public string OptimizationLevel { get; set; } = "Balanced";
}

/// <summary>
/// 속성 매핑 정보
/// </summary>
public sealed class PropertyMappingInfo
{
    public IPropertySymbol SourceProperty { get; set; } = null!;
    public string TargetPropertyName { get; set; } = string.Empty;
    public bool Ignore { get; set; }
    public string? ConverterMethod { get; set; }
    public string? ConditionMethod { get; set; }
    public object? DefaultValue { get; set; }
    public string? ValidatorMethod { get; set; }
}
