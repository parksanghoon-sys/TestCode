using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;

var argsList = args.ToList();
if (argsList.Count == 0)
{
    Console.Error.WriteLine("Usage: WpfDevPack.HookRunner <pre-commit|pre-push> [--repo-root PATH] [--config PATH] [--files path1;path2]");
    return 2;
}

var command = argsList[0].ToLowerInvariant();
var options = ParseOptions(argsList.Skip(1).ToArray());
var repoRoot = options.TryGetValue("repo-root", out var rr) && !string.IsNullOrWhiteSpace(rr)
    ? Path.GetFullPath(rr)
    : Directory.GetCurrentDirectory();
var configPath = options.TryGetValue("config", out var cp) && !string.IsNullOrWhiteSpace(cp)
    ? Path.GetFullPath(cp)
    : Path.Combine(repoRoot, "wpf-dev-pack", "scripts", "codex-hook.config.json");
var config = HookConfig.Load(configPath);

return command switch
{
    "pre-commit" => await PreCommitAsync(repoRoot, config, options),
    "pre-push" => await PrePushAsync(repoRoot, config, options),
    _ => 2
};

static Dictionary<string, string> ParseOptions(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var current = args[i];
        if (!current.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = current[2..];
        var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
            ? args[++i]
            : "true";
        result[key] = value;
    }

    return result;
}

static async Task<int> PreCommitAsync(string repoRoot, HookConfig config, Dictionary<string, string> options)
{
    var files = ResolveFiles(repoRoot, options);
    if (files.Count == 0)
    {
        Console.WriteLine("[codex-hooks] No staged files. Skipping HookRunner pre-commit checks.");
        return 0;
    }

    if (config.PreCommit.CheckMcpDependencies)
    {
        McpDependencyChecker.Check(repoRoot);
    }

    if (config.PreCommit.ValidateXaml)
    {
        foreach (var file in files.Where(HasExtension(".xaml")))
        {
            foreach (var issue in XamlValidator.ValidateFile(Path.Combine(repoRoot, file)))
            {
                Console.WriteLine($"[WPF Dev Pack] XAML warning in {file}: {issue}");
            }
        }
    }

    if (config.PreCommit.DetectMvvmViolations)
    {
        foreach (var file in files.Where(HasExtension(".cs")))
        {
            foreach (var issue in MvvmViolationDetector.ValidateFile(Path.Combine(repoRoot, file)))
            {
                Console.WriteLine($"[WPF Dev Pack] MVVM warning in {file}: {issue}");
            }
        }
    }

    if (config.PreCommit.DetectWpfKeywords)
    {
        var suggestions = WpfKeywordDetector.Detect(files.Select(file => new FileScanInput(file, SafeReadAllText(Path.Combine(repoRoot, file)))));
        foreach (var suggestion in suggestions)
        {
            Console.WriteLine(suggestion);
        }
    }

    if (config.PreCommit.ValidateDotnetTools)
    {
        ToolAvailability.ReportPreCommitTools();
    }

    await Task.CompletedTask;
    return 0;
}

static async Task<int> PrePushAsync(string repoRoot, HookConfig config, Dictionary<string, string> options)
{
    if (config.PrePush.ValidateDotnetTools)
    {
        ToolAvailability.ReportPrePushTools();
    }

    var diagnostics = new List<string>();
    var configuration = string.IsNullOrWhiteSpace(config.PrePush.Configuration) ? "Debug" : config.PrePush.Configuration;

    if (config.PrePush.RunBuildErrorDiagnoser)
    {
        diagnostics.Add(BuildErrorDiagnoser.GetEnvironmentHint());
    }

    if (config.PrePush.CheckMcpDependencies)
    {
        McpDependencyChecker.Check(repoRoot);
    }

    foreach (var line in diagnostics.Where(static x => !string.IsNullOrWhiteSpace(x)))
    {
        Console.WriteLine(line);
    }

    await Task.CompletedTask;
    return 0;
}

static List<string> ResolveFiles(string repoRoot, Dictionary<string, string> options)
{
    if (options.TryGetValue("files", out var filesValue) && !string.IsNullOrWhiteSpace(filesValue))
    {
        return filesValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeRelativePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    return new List<string>();

    string NormalizeRelativePath(string value)
    {
        var full = Path.GetFullPath(Path.Combine(repoRoot, value));
        return Path.GetRelativePath(repoRoot, full);
    }
}

static Func<string, bool> HasExtension(string extension) =>
    path => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);

static string SafeReadAllText(string path)
{
    try
    {
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }
    catch
    {
        return string.Empty;
    }
}

sealed record FileScanInput(string RelativePath, string Content);

sealed class HookConfig
{
    public PreCommitConfig PreCommit { get; init; } = new();
    public PrePushConfig PrePush { get; init; } = new();

    public static HookConfig Load(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return new HookConfig();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HookConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new HookConfig();
        }
        catch
        {
            return new HookConfig();
        }
    }
}

sealed class PreCommitConfig
{
    public bool FormatCSharp { get; init; } = true;
    public bool ValidateXaml { get; init; } = true;
    public bool RestageFiles { get; init; } = true;
    public bool DetectMvvmViolations { get; init; } = true;
    public bool DetectWpfKeywords { get; init; } = true;
    public bool CheckMcpDependencies { get; init; } = true;
    public bool ValidateDotnetTools { get; init; } = true;
}

sealed class PrePushConfig
{
    public bool Build { get; init; } = true;
    public bool Test { get; init; } = true;
    public string Configuration { get; init; } = "Debug";
    public bool RunBuildErrorDiagnoser { get; init; } = true;
    public bool CheckMcpDependencies { get; init; } = true;
    public bool ValidateDotnetTools { get; init; } = true;
}

static class ToolAvailability
{
    public static void ReportPreCommitTools()
    {
        if (!CommandExists("dotnet"))
        {
            Console.WriteLine("[WPF Dev Pack] dotnet SDK not found. C# formatting and XAML tool execution may be limited.");
        }
    }

    public static void ReportPrePushTools()
    {
        if (!CommandExists("dotnet"))
        {
            Console.WriteLine("[WPF Dev Pack] dotnet SDK not found. Build/test hooks require dotnet on PATH.");
        }
    }

    private static bool CommandExists(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path)) return false;
        var candidates = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? new[] { string.Empty, ".exe", ".cmd", ".bat" }
            : new[] { string.Empty };

        foreach (var folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var ext in candidates)
            {
                var full = Path.Combine(folder.Trim(), command + ext);
                if (File.Exists(full)) return true;
            }
        }
        return false;
    }
}

static class McpDependencyChecker
{
    public static void Check(string repoRoot)
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var cacheFile = Path.Combine(Path.GetTempPath(), $"wpf-dev-pack-mcp-check-{today}.txt");
        if (File.Exists(cacheFile))
        {
            return;
        }

        File.WriteAllText(cacheFile, DateTime.UtcNow.ToString("O"));

        var requiredMcps = new[] { "context7", "serena", "microsoft-learn", "csharp-lsp" };
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var possiblePaths = new[]
        {
            Path.Combine(home, ".codex", "config.toml"),
            Path.Combine(home, ".mcp.json"),
            Path.Combine(repoRoot, ".mcp.json"),
            Path.Combine(repoRoot, ".codex", "config.toml")
        };

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in possiblePaths.Where(File.Exists))
        {
            var text = SafeReadAllText(path);
            foreach (var mcp in requiredMcps)
            {
                if (text.Contains(mcp, StringComparison.OrdinalIgnoreCase))
                {
                    found.Add(mcp);
                }
            }
        }

        var missing = requiredMcps.Where(mcp => !found.Contains(mcp)).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        Console.WriteLine($"[WPF Dev Pack] Missing MCP references: {string.Join(", ", missing)}");
    }
}

static class XamlValidator
{
    public static IReadOnlyList<string> ValidateFile(string path)
    {
        var issues = new List<string>();
        if (!File.Exists(path)) return issues;

        string content;
        try
        {
            content = File.ReadAllText(path);
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            using var reader = XmlReader.Create(new StringReader(content), settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            issues.Add($"Invalid XAML/XML: {ex.Message}");
            return issues;
        }

        if (Regex.IsMatch(content, @"<Style\s+(?!.*x:Key)(?!.*TargetType)", RegexOptions.IgnoreCase))
            issues.Add("Style should have either x:Key or TargetType.");
        if (Path.GetFileName(path).Equals("Generic.xaml", StringComparison.OrdinalIgnoreCase) &&
            Regex.IsMatch(content, @"<Style\s+[^>]*TargetType=", RegexOptions.IgnoreCase))
            issues.Add("Generic.xaml contains direct Style entries; verify merged dictionary guidance.");
        if (Regex.IsMatch(content, @"<TextBox[^>]*Text\s*=\s*\"\{Binding\s+(?!.*UpdateSourceTrigger)[^}]*\}\"", RegexOptions.IgnoreCase))
            issues.Add("TextBox Text binding is missing UpdateSourceTrigger=PropertyChanged.");
        if (Regex.IsMatch(content, @"ElementName\s*=\s*(?:self|this)\b", RegexOptions.IgnoreCase))
            issues.Add("ElementName=self/this detected; use RelativeSource Self when appropriate.");
        return issues;
    }
}

static class MvvmViolationDetector
{
    public static IReadOnlyList<string> ValidateFile(string path)
    {
        var issues = new List<string>();
        if (!File.Exists(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) return issues;
        if (!IsViewModelFile(path) || IsExcluded(path)) return issues;

        var content = SafeReadAllText(path);
        if (Regex.IsMatch(content, @"^\s*using\s+System\.Windows(?!\.Input\s*;)(\.\w+)*\s*;", RegexOptions.Multiline))
            issues.Add("using System.Windows.* detected in ViewModel.");
        if (Regex.IsMatch(content, @"\b(Visibility\s*\.|Thickness\b|CornerRadius\b|SolidColorBrush\b|Brush\b(?!\s*=))"))
            issues.Add("PresentationFramework-facing type detected in ViewModel.");
        if (Regex.IsMatch(content, @"\b(ICollectionView\b|CollectionViewSource\b|Dispatcher\b(?!Priority)|DependencyObject\b|FrameworkElement\b|UIElement\b)"))
            issues.Add("WPF-specific type detected in ViewModel.");
        return issues;
    }

    private static bool IsViewModelFile(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        return fileName.Contains("ViewModel", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith("Vm", StringComparison.OrdinalIgnoreCase)
            || directory.Contains("ViewModels", StringComparison.OrdinalIgnoreCase)
            || directory.Contains("ViewModel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcluded(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.Contains(".UI/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Converters/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/Services/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("Converter.cs", StringComparison.OrdinalIgnoreCase);
    }
}

static class WpfKeywordDetector
{
    private static readonly Dictionary<string, string[]> KeywordMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["controltemplate"] = new[] { "customizing-controltemplate" },
        ["control template"] = new[] { "customizing-controltemplate" },
        ["templatebinding"] = new[] { "customizing-controltemplate" },
        ["customcontrol"] = new[] { "authoring-wpf-controls", "developing-wpf-customcontrols" },
        ["custom control"] = new[] { "authoring-wpf-controls", "developing-wpf-customcontrols" },
        ["usercontrol"] = new[] { "make-wpf-usercontrol" },
        ["dependencyproperty"] = new[] { "defining-wpf-dependencyproperty" },
        ["dependency property"] = new[] { "defining-wpf-dependencyproperty" },
        ["behavior"] = new[] { "using-wpf-behaviors-triggers", "make-wpf-behavior" },
        ["eventtrigger"] = new[] { "using-wpf-behaviors-triggers" },
        ["triggeraction"] = new[] { "using-wpf-behaviors-triggers" },
        ["converter"] = new[] { "using-converter-markup-extension", "make-wpf-converter" },
        ["ivalueconverter"] = new[] { "using-converter-markup-extension", "make-wpf-converter" },
        ["imultivalueconverter"] = new[] { "using-converter-markup-extension", "make-wpf-converter" },
        ["markupextension"] = new[] { "using-converter-markup-extension" },
        ["property element"] = new[] { "using-xaml-property-element-syntax" },
        ["mvvm"] = new[] { "implementing-communitytoolkit-mvvm", "make-wpf-viewmodel" },
        ["viewmodel"] = new[] { "implementing-communitytoolkit-mvvm", "make-wpf-viewmodel" },
        ["observableproperty"] = new[] { "implementing-communitytoolkit-mvvm" },
        ["relaycommand"] = new[] { "implementing-communitytoolkit-mvvm" },
        ["inotifypropertychanged"] = new[] { "implementing-communitytoolkit-mvvm" },
        ["communitytoolkit"] = new[] { "implementing-communitytoolkit-mvvm" },
        ["prism"] = new[] { "implementing-pubsub-pattern", "creating-wpf-dialogs" },
        ["binding"] = new[] { "advanced-data-binding" },
        ["multibinding"] = new[] { "advanced-data-binding" },
        ["prioritybinding"] = new[] { "advanced-data-binding" },
        ["relativesource"] = new[] { "advanced-data-binding" },
        ["elementname"] = new[] { "advanced-data-binding" },
        ["collectionview"] = new[] { "managing-wpf-collectionview-mvvm" },
        ["collectionviewsource"] = new[] { "managing-wpf-collectionview-mvvm" },
        ["icollectionview"] = new[] { "managing-wpf-collectionview-mvvm" },
        ["validation"] = new[] { "implementing-wpf-validation", "validating-with-fluentvalidation" },
        ["validationrule"] = new[] { "implementing-wpf-validation" },
        ["inotifydataerrorinfo"] = new[] { "implementing-wpf-validation" },
        ["idataerrorinfo"] = new[] { "implementing-wpf-validation" },
        ["fluentvalidation"] = new[] { "validating-with-fluentvalidation" },
        ["resourcedictionary"] = new[] { "managing-styles-resourcedictionary" },
        ["resource dictionary"] = new[] { "managing-styles-resourcedictionary" },
        ["generic.xaml"] = new[] { "designing-wpf-customcontrol-architecture", "configuring-wpf-themeinfo" },
        ["themeinfo"] = new[] { "configuring-wpf-themeinfo" },
        ["storyboard"] = new[] { "creating-wpf-animations" },
        ["animation"] = new[] { "creating-wpf-animations" },
        ["doubleanimation"] = new[] { "creating-wpf-animations" },
        ["coloranimation"] = new[] { "creating-wpf-animations" },
        ["brush"] = new[] { "creating-wpf-brushes" },
        ["solidcolorbrush"] = new[] { "creating-wpf-brushes" },
        ["lineargradientbrush"] = new[] { "creating-wpf-brushes" },
        ["radialgradientbrush"] = new[] { "creating-wpf-brushes" },
        ["vector icon"] = new[] { "creating-wpf-vector-icons" },
        ["pathgeometry"] = new[] { "implementing-2d-graphics", "creating-wpf-vector-icons" },
        ["icon font"] = new[] { "resolving-icon-font-inheritance" },
        ["drawingcontext"] = new[] { "rendering-with-drawingcontext" },
        ["onrender"] = new[] { "rendering-with-drawingcontext" },
        ["drawingvisual"] = new[] { "rendering-with-drawingvisual" },
        ["render pipeline"] = new[] { "rendering-wpf-architecture" },
        ["bitmapcache"] = new[] { "rendering-wpf-high-performance" },
        ["cachemode"] = new[] { "rendering-wpf-high-performance" },
        ["virtualization"] = new[] { "virtualizing-wpf-ui" },
        ["virtualizingstackpanel"] = new[] { "virtualizing-wpf-ui" },
        ["freezable"] = new[] { "optimizing-wpf-memory" },
        ["freeze"] = new[] { "optimizing-wpf-memory" },
        ["visualtreehelper"] = new[] { "navigating-visual-logical-tree" },
        ["logicaltreehelper"] = new[] { "navigating-visual-logical-tree" },
        ["transformtoancestor"] = new[] { "checking-image-bounds-transform" },
        ["rendertransform"] = new[] { "checking-image-bounds-transform" },
        ["transform"] = new[] { "checking-image-bounds-transform" },
        ["dragdrop"] = new[] { "implementing-wpf-dragdrop" },
        ["drag drop"] = new[] { "implementing-wpf-dragdrop" },
        ["adorner"] = new[] { "implementing-wpf-adorners" },
        ["hittest"] = new[] { "implementing-hit-testing" },
        ["hit test"] = new[] { "implementing-hit-testing" },
        ["commandbinding"] = new[] { "handling-wpf-input-commands" },
        ["inputbinding"] = new[] { "handling-wpf-input-commands" },
        ["routedevent"] = new[] { "routing-wpf-events" },
        ["routed event"] = new[] { "routing-wpf-events" },
        ["dialog"] = new[] { "creating-wpf-dialogs" },
        ["messagebox"] = new[] { "creating-wpf-dialogs" },
        ["flowdocument"] = new[] { "creating-wpf-flowdocument" },
        ["clipboard"] = new[] { "using-wpf-clipboard" },
        ["localization"] = new[] { "localizing-wpf-applications" },
        ["baml"] = new[] { "localizing-wpf-with-baml" },
        ["automationpeer"] = new[] { "implementing-wpf-automation" },
        ["uiautomation"] = new[] { "implementing-wpf-automation" },
        ["dispatcher"] = new[] { "threading-wpf-dispatcher", "handling-async-operations" },
        ["async"] = new[] { "handling-async-operations" },
        ["await"] = new[] { "handling-async-operations" },
        ["servicecollection"] = new[] { "configuring-dependency-injection", "make-wpf-service" },
        ["generichost"] = new[] { "configuring-dependency-injection" },
        ["hostbuilder"] = new[] { "configuring-dependency-injection" },
        ["repository"] = new[] { "implementing-repository-pattern" },
        ["erroror"] = new[] { "handling-errors-with-erroror" },
        ["pipelines"] = new[] { "implementing-io-pipelines" },
        ["pipewriter"] = new[] { "implementing-io-pipelines" },
        ["generatedregex"] = new[] { "using-generated-regex" },
        ["regexgenerator"] = new[] { "using-generated-regex" },
        ["span<"] = new[] { "optimizing-memory-allocation" },
        ["memory<"] = new[] { "optimizing-memory-allocation" },
        ["arraypool"] = new[] { "optimizing-memory-allocation" },
        ["pooled"] = new[] { "optimizing-memory-allocation" },
        ["parallel"] = new[] { "processing-parallel-tasks" },
        ["parallel.foreach"] = new[] { "processing-parallel-tasks" },
        ["task.whenall"] = new[] { "processing-parallel-tasks" },
        ["consoleapp"] = new[] { "configuring-console-app-di" },
        ["livecharts"] = new[] { "integrating-livecharts2" },
        ["scottplot"] = new[] { "scottplot-syncing-modifier-keys-for-mousewheel" },
        ["nodify"] = new[] { "integrating-nodify" },
        ["wpfui"] = new[] { "integrating-wpfui-fluent" },
        ["mediaelement"] = new[] { "integrating-wpf-media" },
        ["dragdelta"] = new[] { "implementing-wpf-dragdrop" },

        ["컨버터"] = new[] { "using-converter-markup-extension", "make-wpf-converter" },
        ["바인딩"] = new[] { "advanced-data-binding" },
        ["멀티 바인딩"] = new[] { "advanced-data-binding" },
        ["뷰모델"] = new[] { "implementing-communitytoolkit-mvvm", "make-wpf-viewmodel" },
        ["유효성 검사"] = new[] { "implementing-wpf-validation", "validating-with-fluentvalidation" },
        ["리소스 딕셔너리"] = new[] { "managing-styles-resourcedictionary" },
        ["스토리보드"] = new[] { "creating-wpf-animations" },
        ["애니메이션"] = new[] { "creating-wpf-animations" },
        ["브러시"] = new[] { "creating-wpf-brushes" },
        ["대화상자"] = new[] { "creating-wpf-dialogs" },
        ["다이얼로그"] = new[] { "creating-wpf-dialogs" },
        ["비헤이비어"] = new[] { "using-wpf-behaviors-triggers", "make-wpf-behavior" },
        ["커스텀 컨트롤"] = new[] { "authoring-wpf-controls", "developing-wpf-customcontrols" },
        ["의존성 프로퍼티"] = new[] { "defining-wpf-dependencyproperty" },
        ["의존성 주입"] = new[] { "configuring-dependency-injection", "make-wpf-service" },
        ["가상화"] = new[] { "virtualizing-wpf-ui" },
        ["렌더링"] = new[] { "rendering-wpf-architecture", "rendering-wpf-high-performance" },
        ["히트 테스트"] = new[] { "implementing-hit-testing" },
        ["드래그드롭"] = new[] { "implementing-wpf-dragdrop" },
        ["드래그 앤 드롭"] = new[] { "implementing-wpf-dragdrop" },
        ["어도너"] = new[] { "implementing-wpf-adorners" },
        ["클립보드"] = new[] { "using-wpf-clipboard" },
        ["지역화"] = new[] { "localizing-wpf-applications" },
        ["디스패처"] = new[] { "threading-wpf-dispatcher", "handling-async-operations" },
        ["성능"] = new[] { "rendering-wpf-high-performance", "optimizing-wpf-memory" }
    };

    private static readonly Dictionary<string, string[]> PathMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/viewmodels/"] = new[] { "make-wpf-viewmodel", "implementing-communitytoolkit-mvvm" },
        ["/views/"] = new[] { "make-wpf-usercontrol", "advanced-data-binding" },
        ["/controls/"] = new[] { "authoring-wpf-controls", "developing-wpf-customcontrols" },
        ["/behaviors/"] = new[] { "using-wpf-behaviors-triggers", "make-wpf-behavior" },
        ["/converters/"] = new[] { "using-converter-markup-extension", "make-wpf-converter" },
        ["/services/"] = new[] { "make-wpf-service", "configuring-dependency-injection" },
        ["/themes/"] = new[] { "managing-styles-resourcedictionary", "configuring-wpf-themeinfo" },
        ["/animations/"] = new[] { "creating-wpf-animations" },
        ["/dialogs/"] = new[] { "creating-wpf-dialogs" },
        ["/validation/"] = new[] { "implementing-wpf-validation", "validating-with-fluentvalidation" }
    };

    public static IReadOnlyList<string> Detect(IEnumerable<FileScanInput> files)
    {
        var hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var matched = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var normalizedPath = file.RelativePath.Replace('\\', '/');
            var corpus = $"{normalizedPath}\n{file.Content}";
            var lowerCorpus = corpus.ToLowerInvariant();

            foreach (var entry in KeywordMap)
            {
                if (!lowerCorpus.Contains(entry.Key.ToLowerInvariant(), StringComparison.Ordinal)) continue;
                foreach (var skill in entry.Value)
                {
                    hits.Add(skill);
                    AddMatch(skill, entry.Key);
                }
            }

            foreach (var entry in PathMap)
            {
                if (!normalizedPath.Contains(entry.Key, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var skill in entry.Value)
                {
                    hits.Add(skill);
                    AddMatch(skill, $"path:{entry.Key}");
                }
            }
        }

        if (hits.Count == 0) return Array.Empty<string>();

        var lines = new List<string>
        {
            "[WPF Dev Pack] Relevant skills detected from staged changes:"
        };

        foreach (var skill in hits.OrderBy(static x => x))
        {
            var reasons = matched.TryGetValue(skill, out var reasonSet)
                ? string.Join(", ", reasonSet.OrderBy(static x => x).Take(3))
                : "heuristic";
            lines.Add($"  -> .agents/skills/{skill}/SKILL.md    [matched: {reasons}]");
        }

        return lines;

        static void AddMatch(string skill, string reason)
        {
            if (!matched.TryGetValue(skill, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                matched[skill] = set;
            }
            set.Add(reason);
        }
    }
}

static class BuildErrorDiagnoser
{
    public static string GetEnvironmentHint() =>
        "[WPF Dev Pack] Pre-push diagnostics active. On build/test failure, inspect CS/NU/XAML errors and cross-check related skills in .agents/skills/.";
}
