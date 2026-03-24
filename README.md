
<h1 align="center">
  <br>
    <img src="./Logo.png" style="width:300px" alt="Mockly"/>
  <br>
</h1>

<h4 align="center">FluentAssertions extensions for Mockly</h4>

<div align="center">

[![](https://img.shields.io/github/actions/workflow/status/dennisdoomen/fluentassertions.mockly/build.yml?branch=main)](https://github.com/dennisdoomen/fluentassertions.mockly/actions?query=branch%3Amain)
[![Coveralls branch](https://img.shields.io/coverallsCoverage/github/dennisdoomen/fluentassertions.mockly?branch=main)](https://coveralls.io/github/dennisdoomen/fluentassertions.mockly?branch=main)
[![](https://img.shields.io/github/release/dennisdoomen/fluentassertions.mockly.svg?label=latest%20release&color=007edf)](https://github.com/dennisdoomen/fluentassertions.mockly/releases/latest)
[![](https://img.shields.io/nuget/dt/FluentAssertions.Mockly.v7.svg?label=v7%20downloads&color=007edf&logo=nuget)](https://www.nuget.org/packages/FluentAssertions.Mockly.v7)
[![](https://img.shields.io/nuget/dt/FluentAssertions.Mockly.v8.svg?label=v8%20downloads&color=007edf&logo=nuget)](https://www.nuget.org/packages/FluentAssertions.Mockly.v8)
![GitHub Repo stars](https://img.shields.io/github/stars/dennisdoomen/fluentassertions.mockly?style=flat)
[![open issues](https://img.shields.io/github/issues/dennisdoomen/fluentassertions.mockly)](https://github.com/dennisdoomen/fluentassertions.mockly/issues)
![Static Badge](https://img.shields.io/badge/4.7.2%2C_8.0-dummy?label=dotnet&color=%235027d5)

</div>

## Documentation

Mockly documentation (including assertion usage) lives on **https://mockly.org**.

This repository contains **only the FluentAssertions assertion packages** for Mockly.
For the core HTTP mocking library, see:

- Website: https://mockly.org
- Core repo: https://github.com/dennisdoomen/mockly
- Core NuGet package: https://www.nuget.org/packages/Mockly

## Packages

Choose the package that matches your FluentAssertions major version:

| Package | FluentAssertions | NuGet |
| --- | --- | --- |
| `FluentAssertions.Mockly.v7` | 7.x | https://www.nuget.org/packages/FluentAssertions.Mockly.v7 |
| `FluentAssertions.Mockly.v8` | 8.x | https://www.nuget.org/packages/FluentAssertions.Mockly.v8 |

## Quick start

Install Mockly and the assertion package you need:

```bash
dotnet add package Mockly

# Pick one:
dotnet add package FluentAssertions.Mockly.v7
dotnet add package FluentAssertions.Mockly.v8
```

Example:

```csharp
using System.Net;
using FluentAssertions;
using Mockly;

var mock = new HttpMock();
mock.ForGet()
    .WithPath("/api/users/123")
    .RespondsWithStatus(HttpStatusCode.OK);

var client = mock.GetClient();
var response = await client.GetAsync("/api/users/123");

response.StatusCode.Should().Be(HttpStatusCode.OK);
mock.Should().HaveAllRequestsCalled();
```

## Building

```bash
./build.ps1
```

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) and open an issue or PR.

## Versioning

This repository uses [Semantic Versioning](https://semver.org/). See the [releases](https://github.com/dennisdoomen/fluentassertions.mockly/releases).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
