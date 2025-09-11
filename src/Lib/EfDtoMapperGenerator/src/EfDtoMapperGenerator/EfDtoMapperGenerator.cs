using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Text;

namespace MapperGenerator;

// Source Generator 선언
// Incremental Generator는 빌드 시점에 Roslyn 컴파일러가 실행해서
// 추가적인 C# 코드를 생성해줍니다.
[Generator]
public class EfDtoMapperGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. [MapTo] Attribute가 붙은 클래스만 필터링
        //    SyntaxProvider를 통해 SyntaxNode(클래스 선언) → SemanticModel(타입 심볼)로 변환

        var candidate = context.SyntaxProvider
                                .CreateSyntaxProvider(predicate: static (node, _) =>
                                    node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0, // 속성(Attribute)이 붙은 클래스만 추출
                                    transform: static (ctx, _) =>
                                    {
                                        // SyntaxNode → Symbol 변환
                                        var classDec1 = ctx.Node as ClassDeclarationSyntax;
                                        var model = ctx.SemanticModel.GetDeclaredSymbol(classDec1) as INamedTypeSymbol;
                                        return model;
                                    }).Where(static symbol => symbol is not null);
        // 2. 실제 코드 생성 작업 등록
        //    후보 클래스(Symbol)를 받아서 매퍼 메서드를 자동 생성
        context.RegisterSourceOutput(candidate, (spc, sourceSymbol) =>
        {
            // [MapTo(typeof(TargetType))] Attribute 추출
            var mapAttr = sourceSymbol.GetAttributes()
                                        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MapperGenerator.Attributes.MapToAttribute");

            if (mapAttr == null) return; // Attribute 없으면 무시
            if (mapAttr.ConstructorArguments.Length == 0) return;

            // Attribute에 지정된 대상 타입 가져오기
            var targetType = mapAttr.ConstructorArguments[0].Value as INamedTypeSymbol;
            if (targetType == null) return;

            // 매퍼 코드 생성
            var sourceCode = GenerateMapper(sourceSymbol, targetType);

            // 최종 생성 파일 추가
            spc.AddSource($"{sourceSymbol.Name}_Mapper.g.cs", sourceCode);
        });
    }
    /// <summary>
    /// 주어진 Source 클래스와 Target 클래스 사이의 매퍼 메서드를 생성합니다.
    /// </summary>
    private string GenerateMapper(INamedTypeSymbol source, INamedTypeSymbol target)
    {        
        var ignoreAttrName = "MapperGenerator.Attributes.MapIgnoreAttribute";

        // Source 클래스의 프로퍼티 중 [MapIgnore]가 없는 것만 추출
        var sourceProps = source.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null &&
                        !p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ignoreAttrName));

        // Target 클래스의 프로퍼티 중 [MapIgnore]가 없는 것만 추출
        var targetProps = target.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null &&
                        !p.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == ignoreAttrName));

        // 양쪽 클래스 모두에 존재하고 타입이 동일한 프로퍼티만 매핑 대상으로 선택
        var commonProps = sourceProps
                          .Select(sp =>
                          {
                              (IPropertySymbol Source, IPropertySymbol Target)? result = null;

                              var mapAttr = sp.GetAttributes()
                                  .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "MapperGenerator.Attributes.MapPropertyAttribute");

                              if (mapAttr != null && mapAttr.ConstructorArguments.Length > 0)
                              {
                                  var targetName = mapAttr.ConstructorArguments[0].Value as string;
                                  var targetProp = targetProps.FirstOrDefault(tp => tp.Name == targetName);
                                  if (targetProp != null && SymbolEqualityComparer.Default.Equals(targetProp.Type, sp.Type))
                                      result = (sp, targetProp);
                              }

                              if (result == null)
                              {
                                  var defaultTarget = targetProps.FirstOrDefault(tp => tp.Name == sp.Name &&
                                      SymbolEqualityComparer.Default.Equals(tp.Type, sp.Type));
                                  if (defaultTarget != null)
                                      result = (sp, defaultTarget);
                              }

                              return result;
                          })
                          .Where(x => x != null)
                          .Select(x => x!.Value)  // nullable 제거
                          .ToList();



        var namespaceName = source.ContainingNamespace?.ToDisplayString() ?? "Generated";
        // 🔹 코드 빌더 시작
        var sb = new StringBuilder($@"
using System;
using System.Collections.Generic;
using System.Linq;

namespace {namespaceName}
{{
    // {source.Name} ↔ {target.Name} 매퍼
    public static class {source.Name}Mapper
    {{
        // Entity → DTO 변환
        public static {target.ToDisplayString()} To{target.Name}(this {source.ToDisplayString()} source)
        {{
            if (source == null) return null;
            return new {target.ToDisplayString()}()
            {{
");

        // 공통 프로퍼티 매핑 (source → target)
        foreach (var (sourceProp, targetProp) in commonProps)
            sb.AppendLine($"                {targetProp.Name} = source.{sourceProp.Name},");

        sb.Append(@"
            };
        }

        // DTO → Entity 변환
        public static " + source.ToDisplayString() + @" To" + source.Name + @"(this " + target.ToDisplayString() + @" source)
        {
            if (source == null) return null;
            return new " + source.ToDisplayString() + @"()
            {
");

        // 공통 프로퍼티 매핑 (target → source)
        foreach (var (sourceProp, targetProp) in commonProps)
            sb.AppendLine($"                {sourceProp.Name} = source.{targetProp.Name},");

        sb.Append(@"
            };
        }

        // List<Entity> → List<DTO>
        public static IEnumerable<" + target.ToDisplayString() + @"> To" + target.Name + @"List(this IEnumerable<" + source.ToDisplayString() + @"> list)
        {
            return list?.Select(x => x.To" + target.Name + @"()) ?? Enumerable.Empty<" + target.ToDisplayString() + @">();
        }

        // List<DTO> → List<Entity>
        public static IEnumerable<" + source.ToDisplayString() + @"> To" + source.Name + @"List(this IEnumerable<" + target.ToDisplayString() + @"> list)
        {
            return list?.Select(x => x.To" + source.Name + @"()) ?? Enumerable.Empty<" + source.ToDisplayString() + @">();
        }
    }
}");
        return sb.ToString();
    }
}
