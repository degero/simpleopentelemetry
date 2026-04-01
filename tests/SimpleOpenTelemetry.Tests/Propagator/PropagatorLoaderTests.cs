using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using SimpleOpenTelemetry.Builder;
using SimpleOpenTelemetry.Propagator;
using Xunit;

namespace SimpleOpenTelemetryTests.Propagator;

[Collection("PropagatorLoaderTests")]
public class PropagatorLoaderTests
{
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly Mock<ILogger> _logger = new();

    [Fact]
    public void AddPropagators_WhenOptionsPropagatorsIsNull_SetsDefaultCompositeTextMapPropagator()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = null
            };

            sut.AddPropagators(options, _logger.Object);

            var composite = Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
            var innerPropagators = GetCompositePropagators(composite).ToList();

            Assert.Equal(2, innerPropagators.Count);
            Assert.IsType<TraceContextPropagator>(innerPropagators[0]);
            Assert.IsType<BaggagePropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Fact]
    public void AddPropagators_WhenOptionsPropagatorsIsEmpty_SetsDefaultCompositeTextMapPropagator()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = Array.Empty<string>()
            };

            sut.AddPropagators(options, _logger.Object);

            var composite = Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
            var innerPropagators = GetCompositePropagators(composite).ToList();

            Assert.Equal(2, innerPropagators.Count);
            Assert.IsType<TraceContextPropagator>(innerPropagators[0]);
            Assert.IsType<BaggagePropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Fact]
    public void AddPropagators_WhenPropagatorsContainsNone_SetsNoopTextMapPropagator()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { "None" }
            };

            sut.AddPropagators(options, _logger.Object);

            Assert.Equal("OpenTelemetry.Context.Propagation.NoopTextMapPropagator", Propagators.DefaultTextMapPropagator.GetType().FullName);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Theory]
    [InlineData(PropagatorEnum.TraceContext, typeof(TraceContextPropagator) )]
    [InlineData(PropagatorEnum.Baggage, typeof(BaggagePropagator) )]
    public void AddPropagators_WithSingleStringValue_SetsSinglePropagator(PropagatorEnum propagatorEnum, Type t)
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { propagatorEnum.ToString() }
            };

            sut.AddPropagators(options, _logger.Object);

            Assert.IsType(t, Propagators.DefaultTextMapPropagator);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Theory]
    [InlineData(PropagatorEnum.B3, "OpenTelemetry.Extensions.Propagators.B3Propagator")]
    [InlineData(PropagatorEnum.AWS, "OpenTelemetry.Extensions.AWS.Trace.AWSXRayPropagator")]
    public void AddPropagators_WithNupkgPropagator_SetsSingleNupkgPropagator(PropagatorEnum propagator, string className)
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { propagator.ToString() }
            };

            sut.AddPropagators(options, _logger.Object);

            Assert.Equal(className, Propagators.DefaultTextMapPropagator.GetType().FullName);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Fact]
    public void AddPropagators_WithMultipleStringValues_SetsCompositeTextMapPropagator()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { nameof(PropagatorEnum.TraceContext), nameof(PropagatorEnum.Baggage) }
            };

            sut.AddPropagators(options, _logger.Object);

            var composite = Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
            var innerPropagators = GetCompositePropagators(composite).ToList();

            Assert.Equal(2, innerPropagators.Count);
            Assert.IsType<TraceContextPropagator>(innerPropagators[0]);
            Assert.IsType<BaggagePropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Fact]
    public void AddPropagators_WithAwsAndOtherPropagators_SetsCompositeWithAWS()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { nameof(PropagatorEnum.AWS), nameof(PropagatorEnum.TraceContext) }
            };

            sut.AddPropagators(options, _logger.Object);

            var composite = Assert.IsType<CompositeTextMapPropagator>(Propagators.DefaultTextMapPropagator);
            var innerPropagators = GetCompositePropagators(composite).ToList();

            Assert.Equal(2, innerPropagators.Count);
            Assert.Equal("OpenTelemetry.Extensions.AWS.Trace.AWSXRayPropagator", innerPropagators[0].GetType().FullName);
            Assert.IsType<TraceContextPropagator>(innerPropagators[1]);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    [Fact]
    public void AddPropagators_AcceptsAllPropagatorEnumNamesAsStrings()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = Enum.GetNames<PropagatorEnum>()
            };

            var exception = Record.Exception(() => sut.AddPropagators(options, _logger.Object));

            Assert.Null(exception);
        }
        finally
        {
            Sdk.SetDefaultTextMapPropagator(original);
        }
    }

    private static IEnumerable<TextMapPropagator> GetCompositePropagators(CompositeTextMapPropagator composite)
    {
        var type = composite.GetType();
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in fields)
        {
            if (typeof(IEnumerable<TextMapPropagator>).IsAssignableFrom(field.FieldType))
            {
                var value = field.GetValue(composite);
                if (value is IEnumerable<TextMapPropagator> items)
                {
                    return items;
                }
            }
        }

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var property in properties)
        {
            if (typeof(IEnumerable<TextMapPropagator>).IsAssignableFrom(property.PropertyType))
            {
                var value = property.GetValue(composite);
                if (value is IEnumerable<TextMapPropagator> items)
                {
                    return items;
                }
            }
        }

        throw new InvalidOperationException("Unable to inspect CompositeTextMapPropagator internal propagators.");
    }
}
