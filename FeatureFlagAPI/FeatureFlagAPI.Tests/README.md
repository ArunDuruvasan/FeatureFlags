# Feature Flag API Tests

This directory contains comprehensive unit and integration tests for the Feature Flag API.

## Test Structure

### Unit Tests

- **Service Tests** (`Service/FeatureEvaluationServiceTests.cs`)
  - Tests for feature evaluation logic
  - Priority testing (User > Group > Region > Default)
  - Cache behavior testing
  - Error handling

- **Controller Tests** (`Controllers/`)
  - `FeaturesControllerTests.cs` - Feature CRUD operations
  - `OverridesControllerTests.cs` - Override management operations

### Integration Tests

- **API Integration Tests** (`Integration/FeatureFlagApiIntegrationTests.cs`)
  - End-to-end API testing
  - Full request/response cycle
  - Database interaction testing

## Running Tests

### Run all tests
```bash
dotnet test
```

### Run with coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Run specific test class
```bash
dotnet test --filter FullyQualifiedName~FeatureEvaluationServiceTests
```

### Run with detailed output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

The tests cover:
- ✅ Feature flag CRUD operations
- ✅ Override management (User, Group, Region)
- ✅ Evaluation logic with priority
- ✅ Cache invalidation
- ✅ Error handling and validation
- ✅ Integration scenarios

## Dependencies

- **xUnit** - Testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **Microsoft.EntityFrameworkCore.InMemory** - In-memory database for testing
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing
