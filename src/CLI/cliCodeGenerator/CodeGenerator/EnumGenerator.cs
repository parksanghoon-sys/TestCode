
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace CodeGenerator
{
    [Generator]
    public class EnumGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 마커 특성을 컴파일 과정에 추가한다.
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource("EnumExtensionAttribute.g.cs", SourceText.From(SourceGeneratorationHelper.Attribute, Encoding.UTF8)));

            // TODO: 소스생성기의 나머지 구현      

            // 열거형에 대한 간단한 필터 수행
            IncrementalValuesProvider<EnumDeclarationSyntax> enumDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsSyntaxTargetForGeneration(s), // select enums with attributes
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)) // sect the enum with the [EnumExtensions] attribute
                .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

            // 선택한 열거형을 `Compilation`과 결합
            IncrementalValueProvider<(Compilation, ImmutableArray<EnumDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(enumDeclarations.Collect());

            // Compilation 및 열거형을 사용하여 소스 생성
            context.RegisterSourceOutput(compilationAndEnums, static(spc, source) => Excute(source.Item1, source.Item2, spc));
        }
        static bool IsSyntaxTargetForGeneration(SyntaxNode node)
                => node is EnumDeclarationSyntax m && m.AttributeLists.Count > 0;
        private const string EnumExtensionsAttribute = "CodeGenerator.EnumExtensionsAttribute";

        static EnumDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            // IsSyntaxTargetForGeneration에서 이미 확인했으므로 EnumDeclarationSyntax로 캐스팅할 수 있습니다.
            var enumDeclarationSyntax = (EnumDeclarationSyntax)context.Node;

            // 메서드의 모든 특성을 반복
            foreach (var attributeList in enumDeclarationSyntax.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        // 이상함, 기호를 가져올 수 없다 무시
                        continue;
                    }
                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if(fullName == EnumExtensionsAttribute)
                        return enumDeclarationSyntax;
                }
            }
            return null;
        }
        static void Excute(Compilation compilation, ImmutableArray<EnumDeclarationSyntax> enumDeclarations, SourceProductionContext context)
        {

        }
    }
}