using FeatureFlagAPI;
using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace FeatureFlagAPI.Tests.Integration
{
    public class FeatureFlagApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;

        public FeatureFlagApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real database
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add in-memory database
                    services.AddDbContext<AppDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                    });
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateFeature_ThenGetAll_ReturnsCreatedFeature()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "integration-test-feature",
                Description = "Integration test",
                DefaultState = true
            };

            // Act - Create
            var createResponse = await _client.PostAsJsonAsync("/api/features", createDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Act - Get All
            var getAllResponse = await _client.GetAsync("/api/features");
            getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var features = await getAllResponse.Content.ReadFromJsonAsync<List<FeatureFlagResponseDto>>();

            // Assert
            createdFeature.Should().NotBeNull();
            createdFeature!.Name.Should().Be("integration-test-feature");
            features.Should().Contain(f => f.Id == createdFeature.Id);
        }

        [Fact]
        public async Task CreateFeature_ThenEvaluate_ReturnsCorrectResult()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "eval-feature",
                DefaultState = false
            };

            // Act - Create
            var createResponse = await _client.PostAsJsonAsync("/api/features", createDto);
            var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Act - Evaluate
            var evalResponse = await _client.GetAsync($"/api/features/evaluate?featureName=eval-feature");
            evalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var evaluation = await evalResponse.Content.ReadFromJsonAsync<EvaluationResponseDto>();

            // Assert
            evaluation.Should().NotBeNull();
            evaluation!.IsEnabled.Should().BeFalse();
            evaluation.AppliedOverride.Should().Be("Default");
        }

        [Fact]
        public async Task CreateFeature_AddOverride_Evaluate_ReturnsOverride()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "override-feature",
                DefaultState = false
            };

            // Act - Create Feature
            var createResponse = await _client.PostAsJsonAsync("/api/features", createDto);
            var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Act - Create Override
            var overrideDto = new CreateOverrideDto
            {
                OverrideType = "User",
                OverrideKey = "test-user",
                State = true
            };
            var overrideResponse = await _client.PostAsJsonAsync($"/api/overrides/{createdFeature!.Id}", overrideDto);
            overrideResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Act - Evaluate
            var evalResponse = await _client.GetAsync($"/api/features/evaluate?featureName=override-feature&userId=test-user");
            evalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var evaluation = await evalResponse.Content.ReadFromJsonAsync<EvaluationResponseDto>();

            // Assert
            evaluation.Should().NotBeNull();
            evaluation!.IsEnabled.Should().BeTrue();
            evaluation.AppliedOverride.Should().Be("User");
            evaluation.AppliedOverrideKey.Should().Be("test-user");
        }

        [Fact]
        public async Task CreateFeature_UpdateFeature_ReturnsUpdatedFeature()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "update-feature",
                DefaultState = false
            };

            // Act - Create
            var createResponse = await _client.PostAsJsonAsync("/api/features", createDto);
            var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Act - Update
            var updateDto = new UpdateFeatureFlagDto
            {
                Description = "Updated description",
                DefaultState = true
            };
            var updateResponse = await _client.PutAsJsonAsync($"/api/features/{createdFeature!.Id}", updateDto);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act - Get
            var getResponse = await _client.GetAsync($"/api/features/{createdFeature.Id}");
            var updatedFeature = await getResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Assert
            updatedFeature.Should().NotBeNull();
            updatedFeature!.DefaultState.Should().BeTrue();
            updatedFeature.Description.Should().Be("Updated description");
        }

        [Fact]
        public async Task CreateFeature_DeleteFeature_ReturnsNotFound()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "delete-feature",
                DefaultState = false
            };

            // Act - Create
            var createResponse = await _client.PostAsJsonAsync("/api/features", createDto);
            var createdFeature = await createResponse.Content.ReadFromJsonAsync<FeatureFlagResponseDto>();

            // Act - Delete
            var deleteResponse = await _client.DeleteAsync($"/api/features/{createdFeature!.Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act - Get (should return 404)
            var getResponse = await _client.GetAsync($"/api/features/{createdFeature.Id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateDuplicateFeature_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateFeatureFlagDto
            {
                Name = "duplicate-feature",
                DefaultState = false
            };

            // Act - Create first
            await _client.PostAsJsonAsync("/api/features", createDto);

            // Act - Create duplicate
            var duplicateResponse = await _client.PostAsJsonAsync("/api/features", createDto);

            // Assert
            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
