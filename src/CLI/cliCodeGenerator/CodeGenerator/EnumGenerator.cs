
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
        static void Excute(Compilation compilation, ImmutableArray<EnumDeclarationSyntax> enums, SourceProductionContext context)
        {
            if (enums.IsDefaultOrEmpty)
                return;
            // 이것이 실제로 필요한지 확실하지 않지만 `[LoggerMessage]`가 수행하므로 좋은 생각인 것 같습니다!
            IEnumerable<EnumDeclarationSyntax> distinctEnums = enums.Distinct();

            // 각 EnumDeclarationSyntax를 EnumToGenerate로 변환
            List<EnumToGenerate> enumsToGenerate = GetTypesToGenerate(compilation, distinctEnums, context.CancellationToken);

            // EnumDeclarationSyntax에 오류가 있는 경우 EnumToGenerate를 생성하지 않으므로 생성할 항목이 있는지 확인합니다. 
            if (enumsToGenerate.Count > 0)
            {
                // 소스 코드를 생성하고 출력에 추가
                string result = SourceGenerationHelper.GenerateExtensionClass(enumsToGenerate);
                context.AddSource("EnumExtensions.g.cs", SourceText.From(result, Encoding.UTF8));
            }

        }
        static List<EnumToGenerate> GetTypesToGenerate(Compilation compilation, IEnumerable<EnumDeclarationSyntax> enums, CancellationToken ct)
        {
            // 출력을 저장할 목록을 만듭니다.
            var enumsToGenerate = new List<EnumToGenerate>();
            // 마커 특성의 의미론적 표현을 얻습니다.
            INamedTypeSymbol? enumAttribute = compilation.GetTypeByMetadataName("NetEscapades.EnumGenerators.EnumExtensionsAttribute");

            if (enumAttribute == null)
            {
                // 이것이 null이면 Compilation에서 마커 특성 유형을 찾을 수 없습니다.
                // 이는 무언가 매우 잘못되었음을 나타냅니다. 구제..
                return enumsToGenerate;
            }

            foreach (EnumDeclarationSyntax enumDeclarationSyntax in enums)
            {
                // 우리가 요청하면 중지
                ct.ThrowIfCancellationRequested();

                // 열거형 구문의 의미론적 표현 얻기 
                SemanticModel semanticModel = compilation.GetSemanticModel(enumDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(enumDeclarationSyntax) is not INamedTypeSymbol enumSymbol)
                {
                    // 뭔가 잘못되었습니다, 구제  
                    continue;
                }

                // 열거형의 전체 유형 이름을 가져옵니다. e.g. Colour,
                // 또는 OuterClass<T>.Colour가 제네릭 형식에 중첩된 경우(예)
                string enumName = enumSymbol.ToString();

                // 열거형의 모든 멤버 가져오기 
                ImmutableArray<ISymbol> enumMembers = enumSymbol.GetMembers();
                var members = new List<string>(enumMembers.Length);

                // 열거형에서 모든 필드를 가져오고 해당 이름을 목록에 추가합니다. 
                foreach (ISymbol member in enumMembers)
                {
                    if (member is IFieldSymbol field && field.ConstantValue is not null)
                    {
                        members.Add(member.Name);
                    }
                }

                // 생성 단계에서 사용할 EnumToGenerate 생성 
                enumsToGenerate.Add(new EnumToGenerate(enumName, members));
            }

            return enumsToGenerate;
        }
    }

}