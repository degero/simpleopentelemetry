using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Reflection;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Moq;
using SimpleOpenTelemetry.Reflection;
using Xunit;

namespace SimpleOpenTelemetryTests.Reflection;


[CollectionDefinition("AssemblyExecutionTests", DisableParallelization = true)]
public class AssemblyExecutionTestsCollection { }

[Collection("AssemblyExecutionTests")]
public class AssemblyExecutionTests : IDisposable
{
    private readonly TestEventListener _simpleOpenTelemetryEventListener;

    public AssemblyExecutionTests()
    {
        _simpleOpenTelemetryEventListener = new();
    }

    public void Dispose()
    {
        _simpleOpenTelemetryEventListener.Dispose();
    }

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
        var target = new AssemblyExecution();

        var assembly = target.TryLoadAssembly("System.Runtime");

        Assert.NotNull(assembly);
        Assert.Equal("System.Runtime", assembly!.GetName().Name);
    }

    [Fact]
    public void TryLoadAssembly_ReturnsNullWhenAssemblyFileMissing()
    {
        var target = new AssemblyExecution();

        var assembly = target.TryLoadAssembly("Definitely.Not.A.Real.Assembly.For.Tests");

        Assert.Null(assembly);
    }

    [Fact]
    public void GetAssembly_ThrowsWhenAssemblyCannotBeLoaded()
    {
        var target = new AssemblyExecution();

        var ex = Assert.Throws<Exception>(() =>
            target.GetAssembly("Definitely.Not.A.Real.Assembly.For.Tests"));

        Assert.Contains("Cannot load assembly", ex.Message);
    }

    [Fact]
    public void FindParameterlessMethod_ReturnsMatchingMethod()
    {
        var target = new AssemblyExecution();
        var method = target.FindParameterlessMethod(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(method);
        Assert.Equal("AddFake", method!.Name);
    }

    [Fact]
    public void FindActionOverload_ReturnsMatchingMethod()
    {
        var target = new AssemblyExecution();
        var method = target.FindActionOverload(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(method);
        Assert.Equal("AddFake", method!.Name);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void InvokeParameterless_InvokesMethodInfo()
    {
        var target = new AssemblyExecution();
        var builder = new FakeBuilder();
        var builderType = typeof(FakeExtensions);

        var methodInfo = builderType.GetMethod(
            "AddFake",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(FakeBuilder)],
            modifiers: null);

        Assert.NotNull(methodInfo);
        var result = target.InvokeParameterless(methodInfo!, builder);

        Assert.Same(builder, result);
        Assert.True(builder.Called);
    }

    [Fact]
    public void BuildConfigureAction_BindsValuesFromSection()
    {
        var target = new AssemblyExecution();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fake:Name"] = "my-name",
                ["Fake:Count"] = "42"
            })
            .Build();

        var section = config.GetSection("Fake");
        var action = target.BuildConfigureAction(typeof(FakeOptions), section);

        var opts = new FakeOptions();
        ((Action<FakeOptions>)action)(opts);

        Assert.Equal("my-name", opts.Name);
        Assert.Equal(42, opts.Count);
    }

    [Fact]
    public void InvokeWithAction_InvokesActionOverloadUsingBoundConfig()
    {
        var target = new AssemblyExecution();
        var builder = new FakeBuilder();
        var actionMethod = target.FindActionOverload(typeof(FakeExtensions), typeof(FakeBuilder), "AddFake");

        Assert.NotNull(actionMethod);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Fake:Name"] = "configured",
                ["Fake:Count"] = "7"
            })
            .Build();

        var result = target.InvokeWithAction(actionMethod!, builder, config.GetSection("Fake"));

        Assert.Same(builder, result);
        Assert.Equal("configured", builder.Value);
        Assert.Equal(7, builder.Number);
    }

    [Fact]
    public void CreateKnownInstanceForConfigurationProperty_Should_AssignPropertyValue_WhenObjectIs_In_ComponentOptionsTypes()
    {
        // ARRANGE
        var target = new AssemblyExecution();
        var section = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string?>
           {
               ["Credential"] = "Azure.Identity.DefaultAzureCredential"
           })
           .Build();
        var config = new AzureMonitorOptions()
        {

        };
        Assert.Null(config.Credential);

        // ACT
        target.CreateKnownInstanceForConfigurationProperty(config, section);

        // ASSERT
        Assert.NotNull(config.Credential);
        Assert.IsType<DefaultAzureCredential>(config.Credential);
        var errorEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error);
        Assert.Null(errorEvent);
    }

    [Fact]
    public void CreateKnownInstanceForConfigurationProperty_Should_NotAssignPropertyValue_WhenObjectIs_NotIn_ComponentOptionsTypes()
    {
        // ARRANGE
        var target = new AssemblyExecution();
        var propName = "ComplexProperty";
        var propType = "SimpleOpenTelemetryTests.Reflection.AssemblyExecutionTests.MockComplexCustomClass";
        var optionsType = "MockComponentOptionsClass";
        var section = new ConfigurationBuilder()
           .AddInMemoryCollection(new Dictionary<string, string?>
           {
               [propName] = propType
           })
           .Build();
        var config = new MockComponentOptionsClass()
        {

        };
        Assert.Null(config.ComplexProperty);

        // ACT
        target.CreateKnownInstanceForConfigurationProperty(config, section);

        // ASSERT
        Assert.Null(config.ComplexProperty);
        var errorEvent = _simpleOpenTelemetryEventListener.Events.FirstOrDefault(e =>
            e.Level == EventLevel.Error &&
            (e.Payload?.Any(p => p?.ToString()?.Contains($"'{propType}' is not a supported ComponentOptionsType for property '{propName}' on '{optionsType}'.") ?? false) ?? false));

        Assert.NotNull(errorEvent);
    }

    internal class MockComplexCustomClass
    {
    }

    internal class MockComponentOptionsClass
    {
        public MockComplexCustomClass? ComplexProperty { get; set; }
    }

}

