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
    public class FeaturesControllerTests
    {
        private readonly AppDbContext _context;
        private readonly FeatureEvaluationService _evaluationService;
        private readonly Mock<ILogger<FeaturesController>> _loggerMock;
        private readonly FeaturesController _controller;

        public FeaturesControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            var cache = new MemoryCache(new MemoryCacheOptions());
            var evalLogger = new Mock<ILogger<FeatureEvaluationService>>();
            _evaluationService = new FeatureEvaluationService(_context, cache, evalLogger.Object);
            _loggerMock = new Mock<ILogger<FeaturesController>>();
            _controller = new FeaturesController(_context, _evaluationService, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAll_ReturnsAllFeatures()
        {
            // Arrange
            var features = new List<FeatureFlag>
            {
                new FeatureFlag { Id = Guid.NewGuid(), Name = "feature1", DefaultState = true },
                new FeatureFlag { Id = Guid.NewGuid(), Name = "feature2", DefaultState = false }
            };
            _context.FeatureFlags.AddRange(features);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedFeatures = okResult.Value.Should().BeAssignableTo<IEnumerable<FeatureFlagResponseDto>>().Subject;
            returnedFeatures.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsFeature()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "test-feature",
                Description = "Test",
                DefaultState = true
            };
            _context.FeatureFlags.Add(feature);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetById(feature.Id);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedFeature = okResult.Value.Should().BeOfType<FeatureFlagResponseDto>().Subject;
            returnedFeature.Name.Should().Be("test-feature");
            returnedFeature.DefaultState.Should().BeTrue();
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetById(Guid.NewGuid());

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_WithValidData_ReturnsCreatedFeature()
        {
            // Arrange
            var dto = new CreateFeatureFlagDto
            {
                Name = "new-feature",
                Description = "New feature",
                DefaultState = true
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var feature = createdResult.Value.Should().BeOfType<FeatureFlagResponseDto>().Subject;
            feature.Name.Should().Be("new-feature");
            feature.DefaultState.Should().BeTrue();
            
            // Verify it was saved
            var savedFeature = await _context.FeatureFlags.FirstOrDefaultAsync(f => f.Name == "new-feature");
            savedFeature.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_WithDuplicateName_ReturnsBadRequest()
        {
            // Arrange
            var existingFeature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "existing-feature",
                DefaultState = false
            };
            _context.FeatureFlags.Add(existingFeature);
            await _context.SaveChangesAsync();

            var dto = new CreateFeatureFlagDto
            {
                Name = "existing-feature",
                DefaultState = true
            };

            // Act
            var result = await _controller.Create(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Update_WithValidData_UpdatesFeature()
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

            var dto = new UpdateFeatureFlagDto
            {
                Description = "Updated description",
                DefaultState = true
            };

            // Act
            var result = await _controller.Update(feature.Id, dto);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            
            var updatedFeature = await _context.FeatureFlags.FindAsync(feature.Id);
            updatedFeature!.DefaultState.Should().BeTrue();
            updatedFeature.Description.Should().Be("Updated description");
        }

        [Fact]
        public async Task Update_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var dto = new UpdateFeatureFlagDto { DefaultState = true };

            // Act
            var result = await _controller.Update(Guid.NewGuid(), dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Delete_WithValidId_DeletesFeature()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "to-delete",
                DefaultState = false
            };
            _context.FeatureFlags.Add(feature);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.Delete(feature.Id);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            
            var deletedFeature = await _context.FeatureFlags.FindAsync(feature.Id);
            deletedFeature.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Delete(Guid.NewGuid());

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Evaluate_WithValidFeature_ReturnsEvaluationResult()
        {
            // Arrange
            var feature = new FeatureFlag
            {
                Id = Guid.NewGuid(),
                Name = "eval-feature",
                DefaultState = true
            };
            _context.FeatureFlags.Add(feature);
            _context.SaveChanges();

            // Act
            var result = _controller.Evaluate("eval-feature", null, null, null);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var evaluation = okResult.Value.Should().BeOfType<EvaluationResponseDto>().Subject;
            evaluation.IsEnabled.Should().BeTrue();
        }

        [Fact]
        public void Evaluate_WithInvalidFeature_ReturnsNotFound()
        {
            // Act
            var result = _controller.Evaluate("non-existent", null, null, null);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void Evaluate_WithEmptyFeatureName_ReturnsBadRequest()
        {
            // Act
            var result = _controller.Evaluate("", null, null, null);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
