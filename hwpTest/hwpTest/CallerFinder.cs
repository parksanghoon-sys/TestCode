using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;


namespace hwpTest
{
    public class CallerFinder
    {
        private readonly List<string> _excludePaths;
        private readonly string _solutionPath;

        public CallerFinder(string solutionPath, List<string> excludePaths)
        {
            this._solutionPath = solutionPath;
            this._excludePaths = excludePaths;
        }

        public string Find(string fileName, string methodName, int parameterCount)
        {
            MSBuildLocator.RegisterDefaults();
            Solution result1 = MSBuildWorkspace.Create().OpenSolutionAsync(this._solutionPath).Result;
            ImmutableHashSet<Document> immutableHashSet = this.GetDocumentsExcludeList(result1).ToImmutableHashSet<Document>();
            ISymbol symbol = (ISymbol)null;
            foreach (Document document in immutableHashSet.Where<Document>((Func<Document, bool>)(document => string.Equals(document.Name, fileName, StringComparison.CurrentCultureIgnoreCase))))
            {
                SemanticModel result2 = document.GetSemanticModelAsync().Result;
                SyntaxNode result3 = document.GetSyntaxRootAsync().Result;
                MethodDeclarationSyntax node = (MethodDeclarationSyntax)null;
                try
                {
                    if (result3 != null)
                        node = result3.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault<MethodDeclarationSyntax>((Func<MethodDeclarationSyntax, bool>)(syntax => syntax.Identifier.ToString() == methodName && syntax.ParameterList.Parameters.Count == parameterCount));
                    if (node == null)
                        continue;
                }
                catch (Exception ex)
                {
                    continue;
                }
                if (result2 != null)
                {
                    symbol = result2.GetSymbolInfo((SyntaxNode)node).Symbol ?? (ISymbol)result2.GetDeclaredSymbol((BaseMethodDeclarationSyntax)node, new CancellationToken());
                    break;
                }
                break;
            }
            return symbol == null ? string.Empty : string.Join("\r\n", (IEnumerable<string>)SymbolFinder.FindCallersAsync(symbol, result1, (IImmutableSet<Document>)immutableHashSet).Result.Select<SymbolCallerInfo, string>((Func<SymbolCallerInfo, string>)(symbolCallerInfo => Regex.Replace(symbolCallerInfo.CallingSymbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), "\\([^)]*\\)", ""))).Distinct<string>().ToList<string>());
        }

        private IEnumerable<Document> GetDocumentsExcludeList(Solution solution)
        {
            List<Document> documentsExcludeList = new List<Document>();
            foreach (Project project in solution.Projects)
            {
                foreach (Document document in project.Documents)
                {
                    Document projectDocument = document;
                    string solutionDirectory = Path.GetDirectoryName(this._solutionPath);
                    if (!this._excludePaths.Any<string>((Func<string, bool>)(excludePath => projectDocument.FilePath != null && solutionDirectory != null && projectDocument.FilePath.Replace(solutionDirectory, string.Empty).Contains(excludePath))))
                        documentsExcludeList.Add(projectDocument);
                }
            }
            return (IEnumerable<Document>)documentsExcludeList;
        }
    }
}
