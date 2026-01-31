using FeatureFlagAPI.DTOs;
using FeatureFlagAPI.Models;
using FeatureFlagAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FeatureFlagAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeaturesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FeatureEvaluationService _evaluationService;
        private readonly ILogger<FeaturesController> _logger;

        public FeaturesController(
            AppDbContext context, 
            FeatureEvaluationService evaluationService,
            ILogger<FeaturesController> logger)
        {
            _context = context;
            _evaluationService = evaluationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FeatureFlagResponseDto>>> GetAll()
        {
            try
            {
                var features = await _context.FeatureFlags
                    .Select(f => new FeatureFlagResponseDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Description = f.Description,
                        DefaultState = f.DefaultState
                    })
                    .ToListAsync();

                return Ok(features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feature flags");
                return StatusCode(500, "An error occurred while retrieving feature flags");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FeatureFlagResponseDto>> GetById(Guid id)
        {
            try
            {
                var feature = await _context.FeatureFlags.FindAsync(id);
                if (feature == null)
                {
                    return NotFound($"Feature flag with ID '{id}' not found");
                }

                return Ok(new FeatureFlagResponseDto
                {
                    Id = feature.Id,
                    Name = feature.Name,
                    Description = feature.Description,
                    DefaultState = feature.DefaultState
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feature flag {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the feature flag");
            }
        }

        [HttpPost]
        public async Task<ActionResult<FeatureFlagResponseDto>> Create([FromBody] CreateFeatureFlagDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check for duplicate name
                if (await _context.FeatureFlags.AnyAsync(f => f.Name == dto.Name))
                {
                    return BadRequest($"A feature flag with the name '{dto.Name}' already exists");
                }

                var feature = new FeatureFlag
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Description = dto.Description,
                    DefaultState = dto.DefaultState
                };

                _context.FeatureFlags.Add(feature);
                await _context.SaveChangesAsync();

                _evaluationService.InvalidateCache(feature.Id, feature.Name);

                var response = new FeatureFlagResponseDto
                {
                    Id = feature.Id,
                    Name = feature.Name,
                    Description = feature.Description,
                    DefaultState = feature.DefaultState
                };

                return CreatedAtAction(nameof(GetById), new { id = feature.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating feature flag");
                return StatusCode(500, "An error occurred while creating the feature flag");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFeatureFlagDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var feature = await _context.FeatureFlags.FindAsync(id);
                if (feature == null)
                {
                    return NotFound($"Feature flag with ID '{id}' not found");
                }

                feature.Description = dto.Description;
                feature.DefaultState = dto.DefaultState;

                await _context.SaveChangesAsync();

                _evaluationService.InvalidateCache(feature.Id, feature.Name);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature flag {Id}", id);
                return StatusCode(500, "An error occurred while updating the feature flag");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var feature = await _context.FeatureFlags.FindAsync(id);
                if (feature == null)
                {
                    return NotFound($"Feature flag with ID '{id}' not found");
                }

                // Delete associated overrides first
                var overrides = await _context.FeatureOverrides
                    .Where(o => o.FeatureId == id)
                    .ToListAsync();
                _context.FeatureOverrides.RemoveRange(overrides);

                _context.FeatureFlags.Remove(feature);
                await _context.SaveChangesAsync();

                _evaluationService.InvalidateCache(feature.Id, feature.Name);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting feature flag {Id}", id);
                return StatusCode(500, "An error occurred while deleting the feature flag");
            }
        }

        [HttpGet("evaluate")]
        public ActionResult<EvaluationResponseDto> Evaluate(
            [FromQuery] string featureName, 
            [FromQuery] string? userId = null, 
            [FromQuery] string? groupId = null, 
            [FromQuery] string? region = null)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                return BadRequest("Feature name is required");
            }

            try
            {
                var result = _evaluationService.EvaluateFeature(featureName, userId, groupId, region);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating feature flag {FeatureName}", featureName);
                return StatusCode(500, "An error occurred while evaluating the feature flag");
            }
        }
    }
}
