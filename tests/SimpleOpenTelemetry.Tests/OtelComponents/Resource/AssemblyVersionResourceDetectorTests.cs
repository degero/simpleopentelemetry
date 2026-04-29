using System.Reflection;
using Moq;
using OpenTelemetry.Resources;
using SimpleOpenTelemetry.OtelComponents.Resource;
using SimpleOpenTelemetry.Utils;
using Xunit;
using OtelResource = OpenTelemetry.Resources.Resource;

namespace SimpleOpenTelemetryTests.OtelComponents.Resource;

public class AssemblyVersionResourceDetectorTests
{
    [Fact]
    public void Detect_WithValidAssemblyVersion_ReturnsResourceWithServiceVersion()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();

        // Assert
        Assert.NotNull(resource);
        Assert.NotEqual(OtelResource.Empty, resource);
        
        var attributes = resource.Attributes;
        Assert.NotNull(attributes);
        Assert.Contains(
            attributes,
            kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion);
    }

    [Fact]
    public void Detect_WithValidAssemblyVersion_HasCorrectAttributeKey()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();
        var attributes = resource.Attributes.ToList();

        // Assert
        var versionAttribute = attributes.FirstOrDefault(
            kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion);
        
        Assert.NotNull(versionAttribute.Key);
        Assert.Equal(OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion, versionAttribute.Key);
    }

    [Fact]
    public void Detect_WithValidAssemblyVersion_VersionValueIsNotEmpty()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();
        var attributes = resource.Attributes.ToList();

        // Assert
        var versionAttribute = attributes.FirstOrDefault(
            kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion);
        
        Assert.NotNull(versionAttribute.Value);
        Assert.IsType<string>(versionAttribute.Value);
        Assert.NotEmpty((string)versionAttribute.Value);
    }

    [Fact]
    public void Detect_WithVersionMetadata_StripsMetadataAfterPlus()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();
        var attributes = resource.Attributes.ToList();

        // Assert
        var versionAttribute = attributes.FirstOrDefault(
            kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion);
        
        var version = (string)versionAttribute.Value;
        // Ensure no metadata (after +) is included
        Assert.DoesNotContain("+", version);
    }

    [Fact]
    public void Detect_ImplementsIResourceDetector()
    {
        // Arrange & Act
        var detector = new AssemblyVersionResourceDetector();

        // Assert
        Assert.IsAssignableFrom<IResourceDetector>(detector);
    }

    [Fact]
    public void Detect_ReturnTypeIsOpenTelemetryResource()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();

        // Assert
        Assert.IsType<OtelResource>(resource);
    }

    [Fact]
    public void Detect_WithValidVersion_ResourceAttributesIsEnumerable()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();

        // Assert
        Assert.NotNull(resource.Attributes);
        Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object>>>(resource.Attributes);
    }

    [Fact]
    public void Detect_WithValidVersion_ContainsExactlyOneAttribute()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();
        var attributeCount = resource.Attributes.Count();

        // Assert
        Assert.Equal(1, attributeCount);
    }

    [Fact]
    public void Detect_MultipleCallsReturnSameVersion()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource1 = detector.Detect();
        var resource2 = detector.Detect();

        var version1 = resource1.Attributes
            .FirstOrDefault(kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion)
            .Value;
        var version2 = resource2.Attributes
            .FirstOrDefault(kvp => kvp.Key == OpenTelemetryConstants.ResourceAttributes.AttributeServiceVersion)
            .Value;

        // Assert
        Assert.Equal(version1, version2);
    }

    [Fact]
    public void Detect_DoesNotThrowException()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act & Assert
        var exception = Record.Exception(() => detector.Detect());
        Assert.Null(exception);
    }

    [Fact]
    public void Detect_ResourceAttributesAreNotNull()
    {
        // Arrange
        var detector = new AssemblyVersionResourceDetector();

        // Act
        var resource = detector.Detect();

        // Assert
        Assert.NotNull(resource.Attributes);
    }
}
