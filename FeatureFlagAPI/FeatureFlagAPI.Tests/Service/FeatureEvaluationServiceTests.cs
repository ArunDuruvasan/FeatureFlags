using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using FeatureFlagAPI.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeatureFlagAPI.Tests.Service
{
    public class FeatureEvaluationServiceTests
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<FeatureEvaluationService>> _loggerMock;
        private readonly FeatureEvaluationService _service;

        public FeatureEvaluationServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _cache = new MemoryCache(new MemoryCacheOptions());
            _loggerMock = new Mock<ILogger<FeatureEvaluationService>>();
            _service = new FeatureEvaluationService(_context, _cache, _loggerMock.Object);

            SeedDatabase();
        }

        private void SeedDatabase()
        {
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                Description = "Test feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);
            _context.SaveChanges();
        }

        [Fact]
        public void EvaluateFeature_WithEmptyFeatureName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.EvaluateFeature("", null, null, null));
            Assert.Throws<ArgumentException>(() => _service.EvaluateFeature("   ", null, null, null));
        }

        [Fact]
        public void EvaluateFeature_WithNonExistentFeature_ThrowsKeyNotFoundException()
        {
            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => 
                _service.EvaluateFeature("non-existent", null, null, null));
        }

        [Fact]
        public void EvaluateFeature_WithNoOverrides_ReturnsDefaultState()
        {
            // Act
            var result = _service.EvaluateFeature("test-feature", null, null, null);

            // Assert
            result.IsEnabled.Should().BeFalse();
            result.AppliedOverride.Should().Be("Default");
            result.AppliedOverrideKey.Should().BeNull();
        }

        [Fact]
        public void EvaluateFeature_WithUserOverride_ReturnsUserOverride()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            var userOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };
            _context.FeatureOverrides.Add(userOverride);
            _context.SaveChanges();
            _service.InvalidateCache(feature.Id, feature.Name);

            // Act
            var result = _service.EvaluateFeature("test-feature", "user123", null, null);

            // Assert
            result.IsEnabled.Should().BeTrue();
            result.AppliedOverride.Should().Be("User");
            result.AppliedOverrideKey.Should().Be("user123");
        }

        [Fact]
        public void EvaluateFeature_WithGroupOverride_ReturnsGroupOverride()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            var groupOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "Group",
                OverrideKey = "premium-users",
                State = true
            };
            _context.FeatureOverrides.Add(groupOverride);
            _context.SaveChanges();
            _service.InvalidateCache(feature.Id, feature.Name);

            // Act
            var result = _service.EvaluateFeature("test-feature", null, "premium-users", null);

            // Assert
            result.IsEnabled.Should().BeTrue();
            result.AppliedOverride.Should().Be("Group");
            result.AppliedOverrideKey.Should().Be("premium-users");
        }

        [Fact]
        public void EvaluateFeature_WithRegionOverride_ReturnsRegionOverride()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            var regionOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "Region",
                OverrideKey = "US",
                State = true
            };
            _context.FeatureOverrides.Add(regionOverride);
            _context.SaveChanges();
            _service.InvalidateCache(feature.Id, feature.Name);

            // Act
            var result = _service.EvaluateFeature("test-feature", null, null, "US");

            // Assert
            result.IsEnabled.Should().BeTrue();
            result.AppliedOverride.Should().Be("Region");
            result.AppliedOverrideKey.Should().Be("US");
        }

        [Fact]
        public void EvaluateFeature_WithUserAndGroupOverride_ReturnsUserOverride()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            var userOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };
            var groupOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "Group",
                OverrideKey = "premium-users",
                State = false
            };
            _context.FeatureOverrides.AddRange(userOverride, groupOverride);
            _context.SaveChanges();
            _service.InvalidateCache(feature.Id, feature.Name);

            // Act
            var result = _service.EvaluateFeature("test-feature", "user123", "premium-users", null);

            // Assert
            result.IsEnabled.Should().BeTrue(); // User override takes precedence
            result.AppliedOverride.Should().Be("User");
            result.AppliedOverrideKey.Should().Be("user123");
        }

        [Fact]
        public void EvaluateFeature_WithGroupAndRegionOverride_ReturnsGroupOverride()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            var groupOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "Group",
                OverrideKey = "premium-users",
                State = true
            };
            var regionOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "Region",
                OverrideKey = "US",
                State = false
            };
            _context.FeatureOverrides.AddRange(groupOverride, regionOverride);
            _context.SaveChanges();
            _service.InvalidateCache(feature.Id, feature.Name);

            // Act
            var result = _service.EvaluateFeature("test-feature", null, "premium-users", "US");

            // Assert
            result.IsEnabled.Should().BeTrue(); // Group override takes precedence
            result.AppliedOverride.Should().Be("Group");
            result.AppliedOverrideKey.Should().Be("premium-users");
        }

        [Fact]
        public void EvaluateFeature_UsesCache_AfterFirstCall()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            
            // Act - First call
            var result1 = _service.EvaluateFeature("test-feature", null, null, null);
            
            // Modify database directly (simulating external change)
            feature.DefaultState = true;
            _context.SaveChanges();

            // Act - Second call should use cache
            var result2 = _service.EvaluateFeature("test-feature", null, null, null);

            // Assert - Should still return cached value
            result2.IsEnabled.Should().BeFalse(); // Cached value, not updated
        }

        [Fact]
        public void InvalidateCache_RemovesFeatureFromCache()
        {
            // Arrange
            var feature = _context.FeatureFlags.First(f => f.Name == "test-feature");
            _service.EvaluateFeature("test-feature", null, null, null); // Populate cache

            // Act
            _service.InvalidateCache(feature.Id, feature.Name);

            // Assert - Cache should be cleared, next call should hit database
            feature.DefaultState = true;
            _context.SaveChanges();
            var result = _service.EvaluateFeature("test-feature", null, null, null);
            result.IsEnabled.Should().BeTrue(); // Should get updated value
        }
    }
}
