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
    public void AddPropagators_WhenOptionsPropagatorsIsNull_SetsNoopTextMapPropagator()
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

            Assert.Equal("OpenTelemetry.Context.Propagation.NoopTextMapPropagator", Propagators.DefaultTextMapPropagator.GetType().FullName);
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

    [Fact]
    public void AddPropagators_WithSingleStringValue_SetsSinglePropagator()
    {
        var original = Propagators.DefaultTextMapPropagator;

        try
        {
            var sut = new PropagatorLoader(_configuration);
            var options = new SimpleOpenTelemetryBuilderOptions
            {
                Propagators = new[] { nameof(PropagatorEnum.TraceContext) }
            };

            sut.AddPropagators(options, _logger.Object);

            Assert.IsType<TraceContextPropagator>(Propagators.DefaultTextMapPropagator);
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
