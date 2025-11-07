# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FastMapper is a high-performance compile-time object mapping library for C# that uses Source Generators to eliminate reflection overhead. The library generates strongly-typed mapping code at compile time through Roslyn analyzers, achieving 10-100x performance improvements over reflection-based mappers.

## Build and Development Commands

### Building the Solution
```bash
# Full build with tests (recommended for development)
dotnet build

# Build specific configuration
dotnet build --configuration Release
dotnet build --configuration Debug

# Using build scripts (includes restore, build, test, and pack)
./build.sh           # Linux/macOS - builds Release by default
./build.sh Debug     # Build Debug configuration
build.bat            # Windows - builds Release by default
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests without rebuilding
dotnet test --no-build --configuration Release

# Run specific test file
dotnet test --filter "FullyQualifiedName~IntegrationTests"
```

### Running Benchmarks
```bash
# Performance benchmarks (compare against AutoMapper and Mapster)
dotnet run --project tests/FastMapper.Benchmarks --configuration Release

# Note: Always run benchmarks in Release mode for accurate results
```

### Restoring Dependencies
```bash
dotnet restore
```

### Creating NuGet Packages
```bash
# Build scripts automatically create packages in Release mode
./build.sh           # Creates packages in ./artifacts directory

# Manual package creation
dotnet pack --configuration Release --output ./artifacts
```

## Architecture Overview

### Three-Layer Design

**1. FastMapper.Core** (`src/FastMapper.Core/`)
- **Purpose**: Attribute definitions and interface contracts
- **Target**: netstandard2.0 (zero dependencies)
- **Key Components**:
  - `Attributes/MappingAttributes.cs`: `[MapTo]`, `[MapProperty]`, `[MapCollection]`, `[MapComplex]` attributes
  - `Abstractions/IMapper.cs`: `IMapper<TSource, TDestination>`, `IBidirectionalMapper<,>`, `IDynamicMapper`
  - `Common/MappingModels.cs`: `MappingResult<T>`, `MappingContext`, `MappingOptions`

**2. FastMapper.SourceGenerator** (`src/FastMapper.SourceGenerator/`)
- **Purpose**: Roslyn-based code generation at compile time
- **Target**: netstandard2.0 (compile-time only, not deployed)
- **Key File**: `FastMapperSourceGenerator.cs`
- **Process**:
  1. Implements `IIncrementalGenerator` for optimal performance
  2. Filters classes decorated with `[MapTo]` attribute
  3. Extracts mapping metadata (property mappings, converters, conditions)
  4. Generates strongly-typed mapper classes in `.Generated` namespace
  5. Emits `*.g.cs` files with implementations of `IMapper<,>`

**3. FastMapper.Extensions** (`src/FastMapper.Extensions/`)
- **Purpose**: Dependency Injection integration for ASP.NET Core
- **Target**: net8.0
- **Key File**: `ServiceCollectionExtensions.cs`
- **Features**:
  - `AddFastMapper()`: Auto-discovers generated mappers in assemblies
  - `IMapperFactory`: Runtime mapper creation
  - `IMappingPerformanceMonitor`: Optional performance tracking
  - Thread-safe registration of mappers in DI container

### Code Generation Flow

```
Developer Code (User.cs)
  ↓ [MapTo(typeof(UserDto))]
Source Generator (Compile Time)
  ↓ Analyzes attributes and properties
Generated Code (UserToUserDtoMapper.g.cs)
  ↓ Implements IMapper<User, UserDto>
DI Container (Runtime)
  ↓ Auto-discovers and registers
Application Code
  ↓ Injects IMapper<User, UserDto>
Direct Method Call (No Reflection)
```

### Key Design Patterns

- **Source Generator Pattern**: Zero-runtime-cost abstraction through compile-time code generation
- **Factory Pattern**: `IMapperFactory` for dynamic mapper creation
- **Result Pattern**: `MappingResult<T>` for functional error handling without exceptions
- **Strategy Pattern**: Multiple optimization levels (Safe/Balanced/Aggressive)
- **Decorator Pattern**: Attributes compose to define complex mapping scenarios

### Generated Code Structure

When you decorate a class with `[MapTo(typeof(Target))]`, the generator creates:

```csharp
namespace YourNamespace.Generated;

public sealed class SourceToTargetMapper : IMapper<Source, Target>
{
    public Target Map(Source source)
    {
        // Null checks
        // Property-by-property assignment
        // Custom converter calls
        // Conditional logic
    }

    public IEnumerable<Target> MapCollection(IEnumerable<Source> sources)
    {
        return sources.Select(Map);
    }

    public async Task<IReadOnlyList<Target>> MapCollectionAsync(...)
    {
        // Parallel processing using Task.WhenAll
    }
}
```

## Important Implementation Details

### Source Generator Reference Pattern

When referencing the Source Generator in project files, always use:
```xml
<ProjectReference Include="..\..\src\FastMapper.SourceGenerator\FastMapper.SourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```
- `OutputItemType="Analyzer"`: Treats generator as Roslyn analyzer
- `ReferenceOutputAssembly="false"`: Generator only runs at compile time

### Attribute Configuration

**Class-Level (`[MapTo]`)**:
- `IsBidirectional`: Enables reverse mapping (Target → Source)
- `ProfileName`: Groups related mappings
- `OptimizationLevel`: Safe/Balanced/Aggressive performance tuning

**Property-Level (`[MapProperty]`)**:
- `TargetPropertyName`: Maps to different property name
- `ConverterMethod`: Custom transformation (must be static method)
- `ConditionMethod`: Conditional mapping (must return bool)
- `ValidatorMethod`: Runtime validation
- `Ignore`: Exclude property from mapping
- `DefaultValue`: Fallback for null sources

### Custom Converters and Validators

Converter and validator methods must be static and follow specific signatures:

```csharp
// Converter: transforms property value
public static string GetFullName(string firstName) => $"{firstName} LastName";

// Condition: determines if property should be mapped
public static bool ShouldMapEmail(User user) => !string.IsNullOrEmpty(user.Email);

// Validator: validates after mapping
public static bool ValidateAge(int age) => age >= 0 && age <= 150;
```

### DI Registration Discovery

`AddFastMapper()` scans assemblies for classes matching:
- Namespace contains `.Generated`
- Class name ends with `Mapper`
- Implements `IMapper<,>` interface
- Is concrete (not abstract/interface)

### Performance Characteristics

- **No Reflection**: All mapping code is statically compiled
- **Inlining**: JIT can inline simple property assignments
- **Zero Allocation**: Direct object construction
- **Parallel Processing**: Built-in async support via `MapCollectionAsync()`
- **Memory**: Typically <1KB per mapped object

## Testing

The test suite uses xUnit, FluentAssertions, and Moq:

- **Unit Tests**: `tests/FastMapper.Tests/Unit/CoreComponentsTests.cs`
- **Integration Tests**: `tests/FastMapper.Tests/Integration/IntegrationTests.cs`
- **Benchmarks**: `tests/FastMapper.Benchmarks/` (compares against AutoMapper and Mapster)

### Running Specific Tests
```bash
# Run single test class
dotnet test --filter "FullyQualifiedName~CoreComponentsTests"

# Run tests by name pattern
dotnet test --filter "DisplayName~Mapper"
```

## Common Development Scenarios

### Adding New Mapping Attributes

1. Add attribute to `src/FastMapper.Core/Attributes/MappingAttributes.cs`
2. Update `FastMapperSourceGenerator.cs` to recognize and process new attribute
3. Modify code generation logic in `GeneratePropertyMapping()` or equivalent
4. Add tests to verify generated code

### Debugging Generated Code

Generated files are created in `obj/{Configuration}/generated/` during build. To inspect:
```bash
# Build to generate code
dotnet build

# View generated files
ls obj/Debug/net8.0/generated/FastMapper.SourceGenerator/FastMapper.SourceGenerator.FastMapperSourceGenerator/
```

In Visual Studio, enable "Show All Files" in Solution Explorer to see generated files under project dependencies.

### Extending with Custom Mappers

If auto-generation doesn't suit your needs, manually implement `IMapper<TSource, TTarget>`:

```csharp
public class CustomUserMapper : IMapper<User, UserDto>
{
    public UserDto Map(User source)
    {
        // Your custom logic
    }

    // Implement other interface members
}

// Register manually
services.AddScoped<IMapper<User, UserDto>, CustomUserMapper>();
```

## Project Configuration

### SDK Requirements
- **.NET SDK**: 8.0.411 or later (specified in `global.json`)
- **Language**: C# latest (configured in `Directory.Build.props`)
- **Nullable Reference Types**: Enabled across all projects
- **Warnings**: Treated as errors (except XML documentation CS1591)

### Central Package Management

Version management is centralized in `Directory.Packages.props`. When adding dependencies:
1. Add `<PackageReference Include="PackageName" />` in project files (no version)
2. Define version in `Directory.Packages.props`

### Build Configuration

`Directory.Build.props` applies to all projects:
- **Debug**: Full debug symbols, no optimization, includes DEBUG constant
- **Release**: Portable PDB, optimization enabled, trimming configured

## Known Limitations

1. **Bidirectional Mapping**: Not fully implemented (see TODO at line 359-360 in `FastMapperSourceGenerator.cs`)
2. **Rebuild Required**: Changes to `[MapTo]` attributes require rebuild to regenerate code
3. **Complex Scenarios**: Deep nested objects or circular references need careful configuration
4. **Static Converters Only**: Converter methods must be static (no instance methods)

## Sample Code Reference

The `samples/FastMapper.Sample/` directory contains working examples:
- Basic mapping with `[MapTo]`
- Custom converters with `[MapProperty(ConverterMethod = "...")]`
- Conditional mapping with `ConditionMethod`
- Collection mapping
- DI integration in `Program.cs`
- Service layer usage patterns

Review sample code for real-world usage patterns before implementing new features.
