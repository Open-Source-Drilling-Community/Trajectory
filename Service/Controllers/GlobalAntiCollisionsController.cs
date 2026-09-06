using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service.Managers;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public class GlobalAntiCollisionsController : ControllerBase
{
    private readonly ILogger<GlobalAntiCollisionManager> logger_;
    private readonly GlobalAntiCollisionManager manager_;
    private readonly GlobalAntiCollisionCalculationWorker worker_;

    public GlobalAntiCollisionsController(
        ILogger<GlobalAntiCollisionManager> logger,
        SqlConnectionManagerSeparationFactorResults connectionManager,
        GlobalAntiCollisionCalculationWorker worker)
    {
        logger_ = logger;
        manager_ = GlobalAntiCollisionManager.GetInstance(logger_, connectionManager);
        worker_ = worker;
    }

    [HttpGet]
    public IEnumerable<string> Get() => manager_.GetIDs();

    [HttpGet("{id}")]
    public ActionResult<GlobalAntiCollision.GlobalAntiCollision> Get(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "invalid_id" });
        }
        GlobalAntiCollision.GlobalAntiCollision? value = manager_.Get(id);
        return value == null ? NotFound() : Ok(value);
    }

    [HttpGet("{id}/Status")]
    public ActionResult<GlobalAntiCollisionCalculationStatus> GetStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "invalid_id" });
        }
        GlobalAntiCollisionCalculationStatus? status = worker_.GetStatus(id);
        return status == null ? NotFound() : Ok(status);
    }

    /// <summary>
    /// Creates and queues a global anti-collision calculation. Poll GET
    /// GlobalAntiCollisions/{id}/Status for progress, then GET the record once completed.
    /// </summary>
    [HttpPost]
    public ActionResult<GlobalAntiCollision.GlobalAntiCollision> Post(
        [FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ID))
        {
            logger_.LogWarning("Post value or its ID is missing");
            return BadRequest(new { error = "invalid_global_anti_collision" });
        }
        if (value.ReferenceTrajectoryID == Guid.Empty && value.ReferenceWellPathID == Guid.Empty)
        {
            return BadRequest(new { error = "reference_trajectory_or_well_path_required" });
        }
        if (!IsValidConfidenceFactor(value.ConfidenceFactor))
        {
            return BadRequest(new
            {
                error = "invalid_confidence_factor",
                maximumConfidenceFactor = GlobalAntiCollision.GlobalAntiCollision.MaximumConfidenceFactor
            });
        }
        if (manager_.Contains(value.ID))
        {
            return Conflict(new { error = "global_anti_collision_already_exists" });
        }

        MarkQueued(value);
        if (!manager_.Add(value))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "global_anti_collision_create_failed" });
        }
        if (!worker_.Queue(value.ID))
        {
            manager_.Remove(value.ID);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "global_anti_collision_queue_unavailable" });
        }
        return Ok(value);
    }

    /// <summary>
    /// Replaces and requeues an existing global anti-collision calculation.
    /// </summary>
    [HttpPut("{id}")]
    public ActionResult<GlobalAntiCollision.GlobalAntiCollision> Put(string id,
        [FromBody] GlobalAntiCollision.GlobalAntiCollision? value)
    {
        if (string.IsNullOrWhiteSpace(id) || value == null ||
            string.IsNullOrWhiteSpace(value.ID) || !string.Equals(id, value.ID, StringComparison.Ordinal))
        {
            logger_.LogWarning("Put route ID and body ID are missing or inconsistent");
            return BadRequest(new { error = "route_body_id_mismatch" });
        }
        if (value.ReferenceTrajectoryID == Guid.Empty && value.ReferenceWellPathID == Guid.Empty)
        {
            return BadRequest(new { error = "reference_trajectory_or_well_path_required" });
        }
        if (!IsValidConfidenceFactor(value.ConfidenceFactor))
        {
            return BadRequest(new
            {
                error = "invalid_confidence_factor",
                maximumConfidenceFactor = GlobalAntiCollision.GlobalAntiCollision.MaximumConfidenceFactor
            });
        }
        if (!manager_.Contains(id))
        {
            return NotFound();
        }
        GlobalAntiCollisionCalculationStatus? currentStatus = worker_.GetStatus(id);
        if (currentStatus?.CalculationState is GlobalAntiCollisionCalculationState.Queued or GlobalAntiCollisionCalculationState.Running)
        {
            return Conflict(new { error = "global_anti_collision_calculation_in_progress" });
        }

        MarkQueued(value);
        if (!manager_.Update(id, value))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "global_anti_collision_update_failed" });
        }
        if (!worker_.Queue(id))
        {
            value.CalculationState = GlobalAntiCollisionCalculationState.Failed;
            value.CalculationMessage = "The calculation queue is unavailable";
            manager_.Update(id, value);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = "global_anti_collision_queue_unavailable" });
        }
        return Ok(value);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "invalid_id" });
        }
        if (!manager_.Contains(id))
        {
            return NotFound();
        }
        if (!manager_.Remove(id))
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "global_anti_collision_delete_failed" });
        }
        worker_.Forget(id);
        return Ok();
    }

    private static void MarkQueued(GlobalAntiCollision.GlobalAntiCollision value)
    {
        value.SeparationFactorResults = [];
        value.CalculationState = GlobalAntiCollisionCalculationState.Queued;
        value.CalculationProgress = 0.0;
        value.CalculationMessage = "Calculation queued";
    }

    internal static bool IsValidConfidenceFactor(double confidenceFactor) =>
        GlobalAntiCollision.GlobalAntiCollision.IsConfidenceFactorSupported(confidenceFactor);
}
