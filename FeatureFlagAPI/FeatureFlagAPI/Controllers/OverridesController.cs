using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using FeatureFlagAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OverridesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FeatureEvaluationService _evaluationService;
        private readonly ILogger<OverridesController> _logger;
        private static readonly string[] ValidOverrideTypes = { "User", "Group", "Region" };

        public OverridesController(
            AppDbContext context, 
            FeatureEvaluationService evaluationService,
            ILogger<OverridesController> logger)
        {
            _context = context;
            _evaluationService = evaluationService;
            _logger = logger;
        }

        [HttpGet("{featureId}")]
        public async Task<ActionResult<IEnumerable<OverrideResponseDto>>> GetByFeature(Guid featureId)
        {
            try
            {
                // Verify feature exists
                var featureExists = await _context.FeatureFlags.AnyAsync(f => f.Id == featureId);
                if (!featureExists)
                {
                    return NotFound($"Feature flag with ID '{featureId}' not found");
                }

                var overrides = await _context.FeatureOverrides
                    .Where(o => o.FeatureId == featureId)
                    .Select(o => new OverrideResponseDto
                    {
                        Id = o.Id,
                        FeatureId = o.FeatureId,
                        OverrideType = o.OverrideType,
                        OverrideKey = o.OverrideKey,
                        State = o.State
                    })
                    .ToListAsync();

                return Ok(overrides);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overrides for feature {FeatureId}", featureId);
                return StatusCode(500, "An error occurred while retrieving overrides");
            }
        }

        [HttpPost("{featureId}")]
        public async Task<ActionResult<OverrideResponseDto>> Create(Guid featureId, [FromBody] CreateOverrideDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validate override type
                if (!ValidOverrideTypes.Contains(dto.OverrideType))
                {
                    return BadRequest($"Invalid override type. Must be one of: {string.Join(", ", ValidOverrideTypes)}");
                }

                // Verify feature exists
                var feature = await _context.FeatureFlags.FindAsync(featureId);
                if (feature == null)
                {
                    return NotFound($"Feature flag with ID '{featureId}' not found");
                }

                // Check for duplicate override (same type and key for the same feature)
                var duplicateExists = await _context.FeatureOverrides
                    .AnyAsync(o => o.FeatureId == featureId 
                        && o.OverrideType == dto.OverrideType 
                        && o.OverrideKey == dto.OverrideKey);
                
                if (duplicateExists)
                {
                    return Conflict($"An override of type '{dto.OverrideType}' with key '{dto.OverrideKey}' already exists for this feature");
                }

                var overrideData = new FeatureOverride
                {
                    Id = Guid.NewGuid(),
                    FeatureId = featureId,
                    OverrideType = dto.OverrideType,
                    OverrideKey = dto.OverrideKey,
                    State = dto.State
                };

                _context.FeatureOverrides.Add(overrideData);
                await _context.SaveChangesAsync();

                _evaluationService.InvalidateCache(featureId, feature.Name);

                var response = new OverrideResponseDto
                {
                    Id = overrideData.Id,
                    FeatureId = overrideData.FeatureId,
                    OverrideType = overrideData.OverrideType,
                    OverrideKey = overrideData.OverrideKey,
                    State = overrideData.State
                };

                return CreatedAtAction(nameof(GetByFeature), new { featureId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating override for feature {FeatureId}", featureId);
                return StatusCode(500, "An error occurred while creating the override");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<OverrideResponseDto>> Update(Guid id, [FromBody] CreateOverrideDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validate override type
                if (!ValidOverrideTypes.Contains(dto.OverrideType))
                {
                    return BadRequest($"Invalid override type. Must be one of: {string.Join(", ", ValidOverrideTypes)}");
                }

                var existing = await _context.FeatureOverrides
                    .FirstOrDefaultAsync(o => o.Id == id);
                
                if (existing == null)
                {
                    return NotFound($"Override with ID '{id}' not found");
                }

                // Check for duplicate override if type or key changed
                if (existing.OverrideType != dto.OverrideType || existing.OverrideKey != dto.OverrideKey)
                {
                    var duplicateExists = await _context.FeatureOverrides
                        .AnyAsync(o => o.FeatureId == existing.FeatureId 
                            && o.Id != id
                            && o.OverrideType == dto.OverrideType 
                            && o.OverrideKey == dto.OverrideKey);
                    
                    if (duplicateExists)
                    {
                        return Conflict($"An override of type '{dto.OverrideType}' with key '{dto.OverrideKey}' already exists for this feature");
                    }
                }

                existing.OverrideType = dto.OverrideType;
                existing.OverrideKey = dto.OverrideKey;
                existing.State = dto.State;

                await _context.SaveChangesAsync();

                var feature = await _context.FeatureFlags.FindAsync(existing.FeatureId);
                _evaluationService.InvalidateCache(existing.FeatureId, feature?.Name);

                var response = new OverrideResponseDto
                {
                    Id = existing.Id,
                    FeatureId = existing.FeatureId,
                    OverrideType = existing.OverrideType,
                    OverrideKey = existing.OverrideKey,
                    State = existing.State
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating override {Id}", id);
                return StatusCode(500, "An error occurred while updating the override");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var overrideData = await _context.FeatureOverrides.FindAsync(id);
                if (overrideData == null)
                {
                    return NotFound($"Override with ID '{id}' not found");
                }

                var featureId = overrideData.FeatureId;
                var feature = await _context.FeatureFlags.FindAsync(featureId);

                _context.FeatureOverrides.Remove(overrideData);
                await _context.SaveChangesAsync();

                _evaluationService.InvalidateCache(featureId, feature?.Name);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting override {Id}", id);
                return StatusCode(500, "An error occurred while deleting the override");
            }
        }
    }
}
