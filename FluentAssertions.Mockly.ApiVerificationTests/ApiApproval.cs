using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Pathy;
using PublicApiGenerator;
using VerifyTests;
using VerifyTests.DiffPlex;
using VerifyXunit;
using Xunit;

namespace FluentAssertions.Mockly.ApiVerificationTests;

public class ApiApproval
{
    private static readonly ChainablePath SourcePath = ChainablePath.Current / ".." / ".." / ".." / "..";

    static ApiApproval()
    {
        VerifyDiffPlex.Initialize(OutputType.Minimal);
    }

    [Theory]
    [ClassData(typeof(AssertionsV7FrameworksTheoryData))]
    public Task ApproveApi_FluentAssertions_v7(string framework)
    {
        return ApproveApiForProject("FluentAssertions.Mockly.v7", framework);
    }

    [Theory]
    [ClassData(typeof(AssertionsV8FrameworksTheoryData))]
    public Task ApproveApi_FluentAssertions_v8(string framework)
    {
        return ApproveApiForProject("FluentAssertions.Mockly.v8", framework);
    }

    private static Task ApproveApiForProject(string projectName, string framework)
    {
        string configuration = typeof(ApiApproval).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var assemblyFile = SourcePath / projectName / "bin" / configuration / framework / (projectName + ".dll");

        var assembly = Assembly.LoadFile(assemblyFile);
        var publicApi = assembly.GeneratePublicApi(options: null);

        return Verifier
            .Verify(publicApi)
            .ScrubLinesContaining("FrameworkDisplayName")
            .UseDirectory("ApprovedApi")
            .UseFileName($"{projectName}.{framework}")
            .DisableDiff();
    }

    private class AssertionsV7FrameworksTheoryData : TheoryData<string>
    {
        public AssertionsV7FrameworksTheoryData()
        {
            AddFrameworksFromProject("FluentAssertions.Mockly.v7");
        }

        private void AddFrameworksFromProject(string projectName)
        {
            var csproj = SourcePath / projectName / (projectName + ".csproj");
            var project = XDocument.Load(csproj);

            var targetFrameworks = project.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks");
            if (targetFrameworks is not null)
            {
                AddRange(targetFrameworks.Value.Split(';'));
                return;
            }

            var targetFramework = project.XPathSelectElement("/Project/PropertyGroup/TargetFramework");
            if (targetFramework is not null)
            {
                Add(targetFramework.Value);
            }
        }
    }

    private class AssertionsV8FrameworksTheoryData : TheoryData<string>
    {
        public AssertionsV8FrameworksTheoryData()
        {
            AddFrameworksFromProject("FluentAssertions.Mockly.v8");
        }

        private void AddFrameworksFromProject(string projectName)
        {
            var csproj = SourcePath / projectName / (projectName + ".csproj");
            var project = XDocument.Load(csproj);

            var targetFrameworks = project.XPathSelectElement("/Project/PropertyGroup/TargetFrameworks");
            if (targetFrameworks is not null)
            {
                AddRange(targetFrameworks.Value.Split(';'));
                return;
            }

            var targetFramework = project.XPathSelectElement("/Project/PropertyGroup/TargetFramework");
            if (targetFramework is not null)
            {
                Add(targetFramework.Value);
            }
        }
    }
}
