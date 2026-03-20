using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace SimpleOpenTelemetryTests.Reflection;

public class AssemblyExecutionTests
{
    private sealed class FakeBuilder
    {
        public bool Called { get; set; }
        public string? Value { get; set; }
        public int Number { get; set; }
    }

    private sealed class FakeOptions
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    private static class FakeExtensions
    {
        public static FakeBuilder AddFake(FakeBuilder builder)
        {
            builder.Called = true;
            return builder;
        }

        public static FakeBuilder AddFake(FakeBuilder builder, Action<FakeOptions> configure)
        {
            var opts = new FakeOptions();
            configure(opts);
            builder.Value = opts.Name;
            builder.Number = opts.Count;
            return builder;
        }
    }

    [Fact]
    public void TryLoadAssembly_ReturnsExistingLoadedAssembly()
    {
        var sut = new AssemblyExecution();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();

        var assembly = sut.TryLoadAssembly("System.Runtime", logger.Object);

        Assert.NotNull(assembly);
        Assert.Equal("System.Runtime", assembly!.GetName().Name);
    }

    [Fact]
    public void TryLoadAssembly_ReturnsNullWhenAssemblyFileMissing()
    {
        var sut = new AssemblyExecution();

        var assembly = sut.TryLoadAssembly("Definitely.Not.A.Real.Assembly.For.Tests", logger: null);

        Assert.Null(assembly);
    }

    [Fact]
    public void GetAssembly_ThrowsWhenAssemblyCannotBeLoaded()
    {
        var sut = new AssemblyExecution();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger>();

        var ex = Assert.Throws<Exception>(() =>
            sut.GetAssembly("Definitely.Not.A.Real.Assembly.For.Tests", logger.Object));

        Assert.Contains("Cannot load otel instrumentation assembly", ex.Message);
    }

    [Fact]
    public void FindParameterlessMethod_ReturnsMatchingMethod()
    {
        var sut = new AssemblyExecution();
        var method = sut.FindParameterlessMethod(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(method);
        Assert.Equal("AddFake", method!.Name);
    }

    [Fact]
    public void FindActionOverload_ReturnsMatchingMethod()
    {
        var sut = new AssemblyExecution();
        var method = sut.FindActionOverload(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(method);
        Assert.Equal("AddFake", method!.Name);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void InvokeParameterless_InvokesMatchingMethod()
    {
        var sut = new AssemblyExecution();
        var builder = new FakeBuilder();

        var result = sut.InvokeParameterless(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake", builder);

        Assert.Same(builder, result);
        Assert.True(builder.Called);
    }

    [Fact]
    public void InvokeParameterless_ThrowsWhenMethodNotFound()
    {
        var sut = new AssemblyExecution();
        var builder = new FakeBuilder();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            sut.InvokeParameterless(typeof(FakeExtensions), typeof(FakeBuilder), "MissingMethod", builder));

        Assert.Contains("No parameterless 'MissingMethod' method", ex.Message);
    }

    [Fact]
    public void BuildConfigureAction_BindsValuesFromSection()
    {
        var sut = new AssemblyExecution();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fake:Name"] = "my-name",
                ["Fake:Count"] = "42"
            })
            .Build();

        var section = config.GetSection("Fake");
        var action = sut.BuildConfigureAction(typeof(FakeOptions), section);

        var opts = new FakeOptions();
        ((Action<FakeOptions>)action)(opts);

        Assert.Equal("my-name", opts.Name);
        Assert.Equal(42, opts.Count);
    }

    [Fact]
    public void InvokeWithAction_InvokesActionOverloadUsingBoundConfig()
    {
        var sut = new AssemblyExecution();
        var builder = new FakeBuilder();
        var actionMethod = sut.FindActionOverload(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(actionMethod);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fake:Name"] = "configured",
                ["Fake:Count"] = "7"
            })
            .Build();

        var result = sut.InvokeWithAction(actionMethod!, builder, config.GetSection("Fake"));

        Assert.Same(builder, result);
        Assert.Equal("configured", builder.Value);
        Assert.Equal(7, builder.Number);
    }
}

