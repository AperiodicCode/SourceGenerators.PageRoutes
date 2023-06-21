using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace AperiodicCode.SourceGenerators.PageRoutes.Tests;

[UsesVerify]
public class PageRouteGeneratorTests
{
    [Fact]
    public Task GeneratesTheThingCorrectly()
    {
        var project = CreateTestProject(
            new()
            {
                ["Pages/TestNoParameters.razor"] = """
            @page "/test-no-parameters"
            <h1>Test</h1>
            """,
                ["Pages/TestWithParamatersPage.razor"] = """
            @page "/test-with-parameters/{id:int}/{name}"
            <h1>Test</h1>
            """
            }
        );

        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult();

        var driverTask = GetDriverAsync(project);

        while (driverTask.IsCompleted == false)
        {
            Thread.Sleep(100);
        }

        var driver = driverTask.Result;

        return TestHelper.Verify(compilation, ref driver);
    }

    private static Project _baseProject = CreateBaseProject();

    private static Project CreateBaseProject()
    {
        var projectId = ProjectId.CreateNewId("TestProject");

        var solution = new AdhocWorkspace().CurrentSolution.AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp);

        var project = solution.Projects
            .Single()
            .WithCompilationOptions(
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable)
            );

        project = project.WithParseOptions(((CSharpParseOptions)project.ParseOptions!).WithLanguageVersion(LanguageVersion.Preview));

        foreach (var defaultCompileLibrary in DependencyContext.Load(typeof(PageRouteGeneratorTests).Assembly)!.CompileLibraries)
        {
            foreach (var resolveReferencePath in defaultCompileLibrary.ResolveReferencePaths(new AppLocalResolver()))
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(resolveReferencePath));
            }
        }

        // The deps file in the project is incorrect and does not contain "compile" nodes for some references.
        // However these binaries are always present in the bin output. As a "temporary" workaround, we'll add
        // every dll file that's present in the test's build output as a metadatareference.
        foreach (var assembly in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            if (
                !project.MetadataReferences.Any(
                    c =>
                        string.Equals(
                            Path.GetFileNameWithoutExtension(c.Display),
                            Path.GetFileNameWithoutExtension(assembly),
                            StringComparison.OrdinalIgnoreCase
                        )
                )
            )
            {
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(assembly));
            }
        }

        return project;
    }

    private static Project CreateTestProject(Dictionary<string, string> additionalSources, Dictionary<string, string>? sources = null)
    {
        var project = _baseProject;

        if (sources is not null)
        {
            foreach (var (name, source) in sources)
            {
                project = project.AddDocument(name, source).Project;
            }
        }

        foreach (var (name, source) in additionalSources)
        {
            project = project.AddAdditionalDocument(name, source).Project;
        }

        return project;
    }

    private static async ValueTask<GeneratorDriver> GetDriverAsync(
        Project project,
        Action<TestAnalyzerConfigOptionsProvider>? configureGlobalOptions = null
    )
    {
        var (driver, _) = await GetDriverWithAdditionalTextAsync(project, configureGlobalOptions);
        return driver;
    }

    private static async ValueTask<(GeneratorDriver, ImmutableArray<AdditionalText>)> GetDriverWithAdditionalTextAsync(
        Project project,
        Action<TestAnalyzerConfigOptionsProvider>? configureGlobalOptions = null
    )
    {
        var result = await GetDriverWithAdditionalTextAndProviderAsync(project, configureGlobalOptions);
        return (result.Item1, result.Item2);
    }

    private static async ValueTask<(
        GeneratorDriver,
        ImmutableArray<AdditionalText>,
        TestAnalyzerConfigOptionsProvider
    )> GetDriverWithAdditionalTextAndProviderAsync(
        Project project,
        Action<TestAnalyzerConfigOptionsProvider>? configureGlobalOptions = null
    )
    {
        var sourceGenerator = new PageRouteGenerator().AsSourceGenerator();

        var driver = (GeneratorDriver)
            CSharpGeneratorDriver.Create(
                new[] { sourceGenerator },
                parseOptions: (CSharpParseOptions)project.ParseOptions!,
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true)
            );

        var optionsProvider = new TestAnalyzerConfigOptionsProvider
        {
            TestGlobalOptions =
            {
                ["build_property.RazorConfiguration"] = "Default",
                ["build_property.RootNamespace"] = "MyApp",
                ["build_property.RazorLangVersion"] = "Latest",
                ["build_property.GenerateRazorMetadataSourceChecksumAttributes"] = "false"
            }
        };

        configureGlobalOptions?.Invoke(optionsProvider);

        var additionalTexts = ImmutableArray<AdditionalText>.Empty;

        foreach (var document in project.AdditionalDocuments)
        {
            var additionalText = new TestAdditionalText(document.Name, await document.GetTextAsync());
            additionalTexts = additionalTexts.Add(additionalText);

            var additionalTextOptions = new TestAnalyzerConfigOptions
            {
                ["build_metadata.AdditionalFiles.TargetPath"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(additionalText.Path))
            };

            optionsProvider.AdditionalTextOptions[additionalText.Path] = additionalTextOptions;
        }

        driver = driver.AddAdditionalTexts(additionalTexts).WithUpdatedAnalyzerConfigOptions(optionsProvider);

        return (driver, additionalTexts, optionsProvider);
    }

    private class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => TestGlobalOptions;

        public TestAnalyzerConfigOptions TestGlobalOptions { get; } = new();

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();

        public Dictionary<string, TestAnalyzerConfigOptions> AdditionalTextOptions { get; } = new();

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            AdditionalTextOptions.TryGetValue(textFile.Path, out var options) ? options : new TestAnalyzerConfigOptions();
    }

    private class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public Dictionary<string, string> Options { get; } = new();

        public string this[string name]
        {
            get => Options[name];
            set => Options[name] = value;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => Options.TryGetValue(key, out value);
    }
}

public class AppLocalResolver : ICompilationAssemblyResolver
{
    public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
    {
        foreach (var assembly in library.Assemblies)
        {
            var dll = Path.Combine(Directory.GetCurrentDirectory(), "refs", Path.GetFileName(assembly));

            if (File.Exists(dll))
            {
                assemblies!.Add(dll);

                return true;
            }

            dll = Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(assembly));

            if (File.Exists(dll))
            {
                assemblies!.Add(dll);

                return true;
            }
        }

        return false;
    }
}

public sealed class TestAdditionalText : AdditionalText
{
    private readonly SourceText _text;

    public TestAdditionalText(string path, SourceText text)
    {
        Path = path;
        _text = text;
    }

    public TestAdditionalText(string text = "", Encoding? encoding = null, string path = "dummy")
        : this(path, SourceText.From(text, encoding)) { }

    public override string Path { get; }

    public override SourceText GetText(CancellationToken cancellationToken = default) => _text;
}
