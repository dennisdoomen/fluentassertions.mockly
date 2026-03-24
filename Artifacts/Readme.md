## About

### What's this?

**Mockly** is a powerful and flexible HTTP mocking library for .NET that makes it easy to test code that depends on `HttpClient`. It provides a fluent API for configuring HTTP request mocks, capturing request details, and asserting on HTTP interactions in your tests.

The library supports:
* **.NET Framework 4.7.2** and higher
* **.NET 8.0** and higher
* **FluentAssertions 7.x and 8.x** integration for expressive test assertions

### What's so special about that?

Unlike other HTTP mocking libraries, Mockly offers:

* **Fluent, intuitive API** - Chain method calls to build complex mocking scenarios with ease
* **Wildcard pattern matching** - Match URLs using wildcards (`*`) in paths and query strings
* **Custom matchers** - Use predicates for advanced request matching logic
* **Request capture & inspection** - Automatically capture all requests with full metadata (headers, body, timestamp)
* **Powerful assertions** - Built-in FluentAssertions extensions for verifying HTTP behavior
* **Diagnostic support** - Detailed error messages when unexpected requests occur
* **Extensibility** - Design allows for custom response generators and matchers
* **Zero configuration** - Works out of the box with sensible defaults
* **Performance optimized** - Regex patterns are cached for efficient matching
* **Invocation limits** - Restrict how many times a mock can respond using `Once()`, `Twice()`, or `Times(n)`

### Who created this?

Mockly is created and maintained by [Dennis Doomen](https://github.com/dennisdoomen), also the creator of [FluentAssertions](https://fluentassertions.com/), [PackageGuard](https://github.com/dennisdoomen/packageguard), [Reflectify](https://github.com/dennisdoomen/reflectify), [Pathy](https://github.com/dennisdoomen/pathy) and the [.NET Library Starter Kit](https://github.com/dennisdoomen/dotnet-library-starter-kit). It's designed to work seamlessly with modern .NET testing practices and integrates naturally with FluentAssertions for expressive test assertions.

## Key Features

### 🎯 Fluent Request Matching

```csharp
mock.ForGet().WithPath("/api/users/*").RespondsWithJsonContent(user);
mock.ForPost().WithPath("/api/data").WithQuery("?filter=*").RespondsWithStatus(HttpStatusCode.Created);
```

### 📃 Clear Reporting

When an unexpected request occurs and there are configured mocks, Mockly helps you diagnose by reporting the closest matching mock (method, scheme/host/path/query) so you can quickly see what to adjust in your setup.

```
Unexpected request to:
  GET http://localhost/fnv_collectiveschemes(111)

Closest matching mock:
  GET https://*/fnv_collectiveschemes(123*)

Registered mocks:
 - GET https://*/fnv_collectiveschemes
 - POST https://*/fnv_collectiveschemes
 - GET https://*/fnv_collectiveschemes(123*)
 - GET https://*/fnv_collectiveschemes(123*) (1 custom matcher(s)) where (request => request.Uri?.Query == "?$count=1")
 - GET https://*/fnv_collectiveschemes(456)
```

### 🔍 Request Capture & Inspection

```csharp
var patches = new RequestCollection();
mock.ForPatch().WithPath("/api/update").CollectingRequestIn(patches);

// After test execution
patches.Count.Should().Be(3);
patches.First().Path.Should().Contain("/api/update");
```

### ✅ Powerful Assertions

```csharp
mock.Should().HaveAllRequestsCalled();
mock.Requests.Should().NotBeEmpty();
mock.Requests.Should().NotContainUnexpectedCalls();

// Assert JSON-equivalence using a JSON string (ignores formatting/ordering)
mock.Requests.Should().ContainRequest()
    .WithBodyMatchingJson("{ \"id\": 1, \"name\": \"x\" }");

// Assert the body deserializes and is equivalent to an object graph
var expected = new { id = 1, name = "x" };

mock.Requests.Should().ContainRequestForUrl("http://localhost:7021/api/*")
    .WithBodyEquivalentTo(expected);
```

### 🎨 Multiple Response Types

* JSON content with automatic serialization
* Test data builder integration via `IResponseBuilder<T>`
* Raw string content
* Custom HTTP status codes
* Custom response generators
* OData support

### 🛡️ Fail-Fast Testing

```csharp
mock.FailOnUnexpectedCalls = true; // Default behavior
// Throws UnexpectedRequestException if an unmocked request is made
```

## Quick Start

Install the package:

```bash
dotnet add package mockly
```

To get the assertions, also install one of the two assertion packages, depending on which version of FluentAssertions you're using:

```bash
dotnet add package FluentAssertions.Mockly.v7
dotnet add package FluentAssertions.Mockly.v8
```

Basic usage:

```csharp
using Mockly;
using FluentAssertions;

// Arrange
var mock = new HttpMock();
mock.ForGet()
    .WithPath("/api/users/123")
    .RespondsWithJsonContent(new { Id = 123, Name = "John Doe" });

HttpClient client = mock.GetClient();

// Act
// Note: BaseAddress defaults to https://localhost/
var response = await client.GetAsync("/api/users/123");
var content = await response.Content.ReadAsStringAsync();

// Assert
response.StatusCode.Should().Be(HttpStatusCode.OK);
content.Should().Contain("John Doe");
mock.Should().HaveAllRequestsCalled();
```

**For complete documentation and advanced examples, visit [dennisdoomen.github.io/mockly](https://dennisdoomen.github.io/mockly/)**

## Versioning

This library uses [Semantic Versioning](https://semver.org/) to give meaning to the version numbers. For the versions available, see the [releases](https://github.com/dennisdoomen/fluentassertions.mockly/releases) on this repository.

## Credits

This library wouldn't have been possible without the following tools, packages and companies:

* [FluentAssertions](https://fluentassertions.com/) - Fluent API for asserting the results of unit tests by [Dennis Doomen](https://github.com/dennisdoomen)
* [Nuke](https://nuke.build/) - Smart automation for DevOps teams and CI/CD pipelines by [Matthias Koch](https://github.com/matkoch)
* [xUnit](https://xunit.net/) - Community-focused unit testing tool for .NET by [Brad Wilson](https://github.com/bradwilson)
* [Coverlet](https://github.com/coverlet-coverage/coverlet) - Cross platform code coverage for .NET by [Toni Solarin-Sodara](https://github.com/tonerdo)
* [GitVersion](https://gitversion.net/) - From git log to SemVer in no time
* [ReportGenerator](https://reportgenerator.io/) - Converts coverage reports by [Daniel Palme](https://github.com/danielpalme)
* [StyleCopyAnalyzer](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) - StyleCop rules for .NET
* [Roslynator](https://github.com/dotnet/roslynator) - A set of code analysis tools for C# by [Josef Pihrt](https://github.com/josefpihrt)
* [CSharpCodingGuidelines](https://github.com/bkoelman/CSharpGuidelinesAnalyzer) - Roslyn analyzers by [Bart Koelman](https://github.com/bkoelman) to go with the [C# Coding Guidelines](https://csharpcodingguidelines.com/)
* [Meziantou](https://github.com/meziantou/Meziantou.Framework) - Another set of awesome Roslyn analyzers by [Gérald Barré](https://github.com/meziantou)
