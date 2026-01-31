# Feature Flag Management System

A comprehensive feature flag system built with Angular, .NET Core Web API, and SQL Server. This system allows you to define feature flags, manage overrides at user, group, and region levels, and evaluate feature states at runtime.

## Features

### Phase 1: Core Requirements
- ✅ **Feature Definition**: Create feature flags with unique names, global default states, and descriptions
- ✅ **Feature Lookup**: Retrieve individual feature flags or list all flags
- ✅ **Runtime Evaluation**: Evaluate feature state based on context (global, user, group, or region)
- ✅ **Level-Based Overrides**: Support for user, group, and region overrides with priority (User > Group > Region > Default)
- ✅ **Feature Mutation**: Full CRUD operations for feature flags and overrides
- ✅ **Validation & Error Handling**: Comprehensive input validation and error handling

### Phase 2: Nice to Have
- ✅ **Performance Optimizations**: In-memory caching for fast feature evaluation
- ✅ **Region-Based Overrides**: Extended override mechanism for region-specific feature control

## Architecture

### Backend (.NET Core Web API)
- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **Caching**: In-memory caching for performance
- **API**: RESTful API with proper DTOs and validation

### Frontend (Angular)
- **Framework**: Angular 18
- **UI**: Bootstrap 5 with responsive design
- **State Management**: Service-based with RxJS
- **Notifications**: Toastr for user feedback

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server (Express or higher)
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Database Setup

1. Update the connection string in `FeatureFlagAPI/FeatureFlagAPI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=FeatureFlagDB;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=False;"
  }
}
```

2. Create the database and run migrations:
```bash
cd FeatureFlagAPI/FeatureFlagAPI
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Or manually create the database and let EF Core create tables on first run.

### 2. Backend Setup

1. Navigate to the API project:
```bash
cd FeatureFlagAPI/FeatureFlagAPI
```

2. Restore packages:
```bash
dotnet restore
```

3. Run the API:
```bash
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:7119`

### 3. Frontend Setup

1. Navigate to the Angular project:
```bash
cd feature-flag-ui
```

2. Install dependencies:
```bash
npm install
```

3. Update the API URL in `src/app/Services/feature-service.service.ts` if needed:
```typescript
private apiUrl = 'https://localhost:7119/api';
```

4. Run the Angular development server:
```bash
npm start
```

The frontend will be available at `http://localhost:4200`

## API Endpoints

### Feature Flags

- `GET /api/features` - Get all feature flags
- `GET /api/features/{id}` - Get a specific feature flag
- `POST /api/features` - Create a new feature flag
- `PUT /api/features/{id}` - Update a feature flag
- `DELETE /api/features/{id}` - Delete a feature flag
- `GET /api/features/evaluate?featureName={name}&userId={id}&groupId={id}&region={region}` - Evaluate feature state

### Overrides

- `GET /api/overrides/{featureId}` - Get all overrides for a feature
- `POST /api/overrides/{featureId}` - Create a new override
- `PUT /api/overrides/{id}` - Update an override
- `DELETE /api/overrides/{id}` - Delete an override

## Usage Examples

### Creating a Feature Flag

1. Click "Add Feature" button
2. Enter:
   - Name: `new-checkout-flow`
   - Description: `New checkout experience`
   - Default State: `Enabled` or `Disabled`
3. Click "Save"

### Adding Overrides

1. Click "Manage Overrides" on a feature
2. Select override type (User, Group, or Region)
3. Enter the override key (e.g., user ID, group name, region code)
4. Set the state (Enabled/Disabled)
5. Click "Add Override"

### Evaluating Features

1. Use the "Evaluate Feature" section
2. Enter:
   - Feature Name: `new-checkout-flow`
   - User ID: `user123` (optional)
   - Group ID: `premium-users` (optional)
   - Region: `US` (optional)
3. Click "Evaluate"
4. View the result showing:
   - Whether the feature is enabled
   - Which override was applied (User, Group, Region, or Default)
   - The override key that was used

## Evaluation Priority

The system evaluates feature flags in the following priority order:

1. **User Override** - Highest priority
2. **Group Override** - Second priority
3. **Region Override** - Third priority
4. **Default State** - Fallback if no overrides match

## Performance Considerations

- **In-Memory Caching**: Feature flags and overrides are cached for 5 minutes to reduce database queries
- **Fast Evaluation**: Evaluation logic uses in-memory lookups, avoiding I/O operations
- **Cache Invalidation**: Cache is automatically invalidated when features or overrides are modified

## Database Schema
Attached DB schema file with the project and exceute the query for table creation.
### FeatureFlag
- `Id` (Guid, PK)
- `Name` (string, unique, required, max 100 chars)
- `Description` (string, optional, max 500 chars)
- `DefaultState` (bool, required)

### FeatureOverride
- `Id` (Guid, PK)
- `FeatureId` (Guid, FK to FeatureFlag)
- `OverrideType` (string, required: "User", "Group", or "Region")
- `OverrideKey` (string, required, max 100 chars)
- `State` (bool, required)

Unique constraint on (FeatureId, OverrideType, OverrideKey)

## Error Handling

The system includes comprehensive error handling:
- Validation errors for invalid inputs
- Duplicate feature name detection
- Non-existent feature/override handling
- Database constraint violations
- User-friendly error messages via Toastr notifications

## Development Notes

- The API uses DTOs for all requests/responses
- CORS is configured to allow requests from `http://localhost:4200`
- Swagger UI is available at `/swagger` in development mode
- All API endpoints return appropriate HTTP status codes

## Testing

### Backend Tests

The backend includes comprehensive unit and integration tests:

```bash
cd FeatureFlagAPI/FeatureFlagAPI.Tests
dotnet test
```

**Test Coverage:**
- ✅ FeatureEvaluationService - Evaluation logic, priority, caching
- ✅ FeaturesController - CRUD operations
- ✅ OverridesController - Override management
- ✅ Integration tests - End-to-end API testing

**Run with coverage:**
```bash
dotnet test /p:CollectCoverage=true
```

### Frontend Tests

The Angular frontend includes unit tests for services and components:

```bash
cd feature-flag-ui
npm test
```

**Test Coverage:**
- ✅ FeatureService - API communication
- ✅ FeatureListComponent - Feature management UI
- ✅ EvaluationTesterComponent - Feature evaluation

**Run with coverage:**
```bash
npm test -- --code-coverage
```

## Future Enhancements

Potential improvements:
- Authentication and authorization
- Audit logging
- Feature flag analytics
- Percentage-based rollouts
- Scheduled feature releases
- A/B testing integration

## License

This project is provided as-is for demonstration purposes.
