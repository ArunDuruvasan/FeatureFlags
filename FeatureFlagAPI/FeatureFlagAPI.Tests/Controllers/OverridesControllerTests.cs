using FeatureFlagAPI.Controllers;
using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using FeatureFlagAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeatureFlagAPI.Tests.Controllers
{
    public class OverridesControllerTests
    {
        private readonly AppDbContext _context;
        private readonly FeatureEvaluationService _evaluationService;
        private readonly Mock<ILogger<OverridesController>> _loggerMock;
        private readonly OverridesController _controller;

        public OverridesControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var evalLogger = new Mock<ILogger<FeatureEvaluationService>>();
            _evaluationService = new FeatureEvaluationService(_context, cache, evalLogger.Object);
            _loggerMock = new Mock<ILogger<OverridesController>>();
            _controller = new OverridesController(_context, _evaluationService, _loggerMock.Object);
        }

        [Fact]
        public async Task GetByFeature_WithValidFeatureId_ReturnsOverrides()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);

            var overrides = new List<FeatureOverride>
            {
                new FeatureOverride
                {
                    Id = Guid.NewGuid(),
                    FeatureId = feature.Id,
                    OverrideType = "User",
                    OverrideKey = "user1",
                    State = true
                },
                new FeatureOverride
                {
                    Id = Guid.NewGuid(),
                    FeatureId = feature.Id,
                    OverrideType = "Group",
                    OverrideKey = "group1",
                    State = false
                }
            };
            _context.FeatureOverrides.AddRange(overrides);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetByFeature(feature.Id);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedOverrides = okResult.Value.Should().BeAssignableTo<IEnumerable<OverrideResponseDto>>().Subject;
            returnedOverrides.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByFeature_WithInvalidFeatureId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetByFeature(Guid.NewGuid());

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedOverride()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);
            await _context.SaveChangesAsync();

            var dto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };

            // Act
            var result = await _controller.Create(feature.Id, dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var overrideResponse = createdResult.Value.Should().BeOfType<OverrideResponseDto>().Subject;
            overrideResponse.OverrideType.Should().Be("User");
            overrideResponse.OverrideKey.Should().Be("user123");
            overrideResponse.State.Should().BeTrue();

            // Verify it was saved
            var savedOverride = await _context.FeatureOverrides
                .FirstOrDefaultAsync(o => o.FeatureId == feature.Id && o.OverrideKey == "user123");
            savedOverride.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_WithInvalidFeatureId_ReturnsNotFound()
        {
            // Arrange
            var dto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };

            // Act
            var result = await _controller.Create(Guid.NewGuid(), dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_WithDuplicateOverride_ReturnsConflict()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);

            var existingOverride = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };
            _context.FeatureOverrides.Add(existingOverride);
            await _context.SaveChangesAsync();

            var dto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "user123",
                State = false
            };

            // Act
            var result = await _controller.Create(feature.Id, dto);

            // Assert
            result.Result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task Create_WithInvalidOverrideType_ReturnsBadRequest()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);
            await _context.SaveChangesAsync();

            var dto = new CreateOverrideDto
            {
                OverrideType = "InvalidType",
                OverrideKey = "key1",
                State = true
            };

            // Act
            var result = await _controller.Create(feature.Id, dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_WithValidData_UpdatesOverride()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);

            var overrideData = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "User",
                OverrideKey = "user123",
                State = false
            };
            _context.FeatureOverrides.Add(overrideData);
            await _context.SaveChangesAsync();

            var dto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };

            // Act
            var result = await _controller.Update(overrideData.Id, dto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var updatedOverride = okResult.Value.Should().BeOfType<OverrideResponseDto>().Subject;
            updatedOverride.State.Should().BeTrue();

            // Verify it was updated in database
            var dbOverride = await _context.FeatureOverrides.FindAsync(overrideData.Id);
            dbOverride!.State.Should().BeTrue();
        }

        [Fact]
        public async Task Update_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var dto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };

            // Act
            var result = await _controller.Update(Guid.NewGuid(), dto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Delete_WithValidId_DeletesOverride()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);

            var overrideData = new FeatureOverride
            {
                Id = Guid.NewGuid(),
                FeatureId = feature.Id,
                OverrideType = "User",
                OverrideKey = "user123",
                State = true
            };
            _context.FeatureOverrides.Add(overrideData);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(overrideData.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            var deletedOverride = await _context.FeatureOverrides.FindAsync(overrideData.Id);
            deletedOverride.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Delete(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
