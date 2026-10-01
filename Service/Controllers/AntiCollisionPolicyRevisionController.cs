using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class AntiCollisionPolicyRevisionController(AntiCollisionPolicyManager manager) : ControllerBase
{
    [HttpGet(Name = "GetAllAntiCollisionPolicyRevisionId")]
    public ActionResult<IEnumerable<Guid>> GetIds()
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllAntiCollisionPolicyRevisionId");
        List<Guid>? values = manager.GetRevisionIds();
        return values != null ? Ok(values) : StatusCode(500, Error("persistence_failed", "Policy revisions could not be listed."));
    }

    [HttpGet("HeavyData", Name = "GetAllAntiCollisionPolicyRevision")]
    public ActionResult<IEnumerable<AntiCollisionPolicyRevision>> GetAll([FromQuery] Guid? policyId = null)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllAntiCollisionPolicyRevision");
        List<AntiCollisionPolicyRevision>? values = manager.GetRevisions(policyId);
        return values != null ? Ok(values) : StatusCode(500, Error("persistence_failed", "Policy revisions could not be listed."));
    }

    [HttpGet("{id}", Name = "GetAntiCollisionPolicyRevisionById")]
    public ActionResult<AntiCollisionPolicyRevision> GetById(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetAntiCollisionPolicyRevisionById");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty revision UUID is required."));
        AntiCollisionPolicyRevision? value = manager.GetRevision(id);
        return value != null ? Ok(value) : NotFound(Error("not_found", "The policy revision does not exist."));
    }

    [HttpPost(Name = "PostAntiCollisionPolicyRevision")]
    [ProducesResponseType<AntiCollisionPolicyRevision>(StatusCodes.Status201Created)]
    public ActionResult<AntiCollisionPolicyRevision> Post([FromBody] AntiCollisionPolicyRevisionCreate? request)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("PostAntiCollisionPolicyRevision");
        if (request == null) return BadRequest(Error("request_required", "A policy revision is required."));
        AntiCollisionPolicyRevision value = new()
        {
            MetaInfo = request.MetaInfo,
            PolicyID = request.PolicyID,
            Name = request.Name,
            Description = request.Description,
            ConfidenceFactor = request.ConfidenceFactor,
            Rules = request.Rules
        };
        List<string> errors = AntiCollisionPolicyValidation.Validate(value);
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        if (!manager.AddRevision(value)) return Conflict(Error("revision_not_created", "The revision UUID already exists or persistence failed."));
        return CreatedAtAction(nameof(GetById), new { id = value.MetaInfo!.ID }, value);
    }

    private static object Error(string code, string message) => new { error = code, message };
}
