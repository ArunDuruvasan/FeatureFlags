using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FeatureFlagAPI.Service
{
    public class FeatureEvaluationService
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FeatureEvaluationService> _logger;
        private const string CacheKeyPrefix = "FeatureFlag_";
        private const string OverrideCacheKeyPrefix = "Overrides_";
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

        public FeatureEvaluationService(AppDbContext context, IMemoryCache cache, ILogger<FeatureEvaluationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public EvaluationResponseDto EvaluateFeature(string featureName, string? userId, string? groupId, string? region)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name cannot be null or empty", nameof(featureName));
            }

            // Normalize feature name (trim whitespace)
            var normalizedFeatureName = featureName.Trim();

            // Try to get feature from cache first (case-sensitive cache key)
            var cacheKey = $"{CacheKeyPrefix}{normalizedFeatureName}";
            if (!_cache.TryGetValue(cacheKey, out FeatureFlag? feature))
            {
                // Query database - try exact match first (most common case)
                feature = _context.FeatureFlags
                    .AsNoTracking() // Improve performance for read-only queries
                    .FirstOrDefault(f => f.Name == normalizedFeatureName);
                
                // If not found, try case-insensitive search using EF.Functions
                if (feature == null)
                {
                    // Use a more efficient case-insensitive query
                    // For SQL Server, we can use COLLATE or compare in memory for small datasets
                    var features = _context.FeatureFlags
                        .AsNoTracking()
                        .Where(f => f.Name.ToLower() == normalizedFeatureName.ToLower())
                        .ToList();
                    
                    feature = features.FirstOrDefault();
                }
                
                if (feature == null)
                {
                    // Log available features for debugging (only in development or when explicitly needed)
                    var availableFeatures = _context.FeatureFlags
                        .AsNoTracking()
                        .Select(f => f.Name)
                        .Take(10) // Limit to first 10 to avoid performance issues
                        .ToList();
                    
                    _logger.LogWarning(
                        "Feature flag '{FeatureName}' not found in database. Sample available features: {AvailableFeatures}",
                        normalizedFeatureName,
                        string.Join(", ", availableFeatures));
                    
                    throw new KeyNotFoundException($"Feature flag '{normalizedFeatureName}' not found. Please ensure the feature flag exists and the name is spelled correctly.");
                }
                
                // Cache the feature using the normalized name
                _cache.Set(cacheKey, feature, CacheExpiration);
                _logger.LogDebug("Feature flag '{FeatureName}' (ID: {FeatureId}) loaded from database and cached", 
                    normalizedFeatureName, feature.Id);
            }
            else
            {
                _logger.LogDebug("Feature flag '{FeatureName}' loaded from cache", normalizedFeatureName);
            }

            // Get overrides from cache or database
            var overrideCacheKey = $"{OverrideCacheKeyPrefix}{feature.Id}";
            if (!_cache.TryGetValue(overrideCacheKey, out List<FeatureOverride>? overrides))
            {
                overrides = _context.FeatureOverrides
                    .Where(o => o.FeatureId == feature.Id)
                    .ToList();
                _cache.Set(overrideCacheKey, overrides, CacheExpiration);
            }

            // Evaluate with priority: User > Group > Region > Default
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var userOverride = overrides?.FirstOrDefault(o => 
                    o.OverrideType == "User" && o.OverrideKey == userId);
                if (userOverride != null)
                {
                    return new EvaluationResponseDto
                    {
                        IsEnabled = userOverride.State,
                        AppliedOverride = "User",
                        AppliedOverrideKey = userId
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(groupId))
            {
                var groupOverride = overrides?.FirstOrDefault(o => 
                    o.OverrideType == "Group" && o.OverrideKey == groupId);
                if (groupOverride != null)
                {
                    return new EvaluationResponseDto
                    {
                        IsEnabled = groupOverride.State,
                        AppliedOverride = "Group",
                        AppliedOverrideKey = groupId
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(region))
            {
                var regionOverride = overrides?.FirstOrDefault(o => 
                    o.OverrideType == "Region" && o.OverrideKey == region);
                if (regionOverride != null)
                {
                    return new EvaluationResponseDto
                    {
                        IsEnabled = regionOverride.State,
                        AppliedOverride = "Region",
                        AppliedOverrideKey = region
                    };
                }
            }

            return new EvaluationResponseDto
            {
                IsEnabled = feature.DefaultState,
                AppliedOverride = "Default",
                AppliedOverrideKey = null
            };
        }

        public void InvalidateCache(Guid? featureId = null, string? featureName = null)
        {
            if (featureId.HasValue)
            {
                _cache.Remove($"{OverrideCacheKeyPrefix}{featureId.Value}");
            }
            if (!string.IsNullOrWhiteSpace(featureName))
            {
                // Normalize the feature name to match how it's stored in cache
                var normalizedName = featureName.Trim();
                _cache.Remove($"{CacheKeyPrefix}{normalizedName}");
                _logger.LogDebug("Cache invalidated for feature '{FeatureName}'", normalizedName);
            }
        }
    }
}
