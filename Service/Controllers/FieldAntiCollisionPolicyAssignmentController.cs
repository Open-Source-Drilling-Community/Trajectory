using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using OSDC.Drilling.GlobalAntiCollision;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class FieldAntiCollisionPolicyAssignmentController(AntiCollisionPolicyManager manager) : ControllerBase
{
    [HttpGet(Name = "GetAllFieldAntiCollisionPolicyAssignment")]
    public ActionResult<IEnumerable<FieldAntiCollisionPolicyAssignment>> GetAll([FromQuery] Guid? fieldId = null)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllFieldAntiCollisionPolicyAssignment");
        List<FieldAntiCollisionPolicyAssignment>? values = manager.GetAssignments(fieldId);
        return values != null ? Ok(values) : StatusCode(500, Error("persistence_failed", "Assignments could not be listed."));
    }

    [HttpGet("{id}", Name = "GetFieldAntiCollisionPolicyAssignmentById")]
    public ActionResult<FieldAntiCollisionPolicyAssignment> GetById(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetFieldAntiCollisionPolicyAssignmentById");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty assignment UUID is required."));
        FieldAntiCollisionPolicyAssignment? value = manager.GetAssignment(id);
        return value != null ? Ok(value) : NotFound(Error("not_found", "The assignment does not exist."));
    }

    [HttpGet("Effective/{fieldId}", Name = "GetEffectiveFieldAntiCollisionPolicyAssignment")]
    public ActionResult<FieldAntiCollisionPolicyAssignment> GetEffective(Guid fieldId, [FromQuery] DateTimeOffset atUtc)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetEffectiveFieldAntiCollisionPolicyAssignment");
        if (fieldId == Guid.Empty || atUtc == default) return BadRequest(Error("invalid_query", "A Field UUID and UTC evaluation instant are required."));
        FieldAntiCollisionPolicyAssignment? value = manager.GetEffectiveAssignment(fieldId, atUtc);
        return value != null ? Ok(value) : NotFound(Error("not_found", "No policy assignment is effective at that instant."));
    }

    [HttpPost(Name = "PostFieldAntiCollisionPolicyAssignment")]
    [ProducesResponseType<FieldAntiCollisionPolicyAssignment>(StatusCodes.Status201Created)]
    public ActionResult<FieldAntiCollisionPolicyAssignment> Post([FromBody] FieldAntiCollisionPolicyAssignmentMutation? request)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("PostFieldAntiCollisionPolicyAssignment");
        FieldAntiCollisionPolicyAssignment? value = ToValue(request);
        List<string> errors = AntiCollisionPolicyValidation.Validate(value);
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        if (!manager.AddAssignment(value!)) return Conflict(Error("assignment_not_created", "The policy revision is missing, the UUID exists, or the Field validity interval overlaps another assignment."));
        return CreatedAtAction(nameof(GetById), new { id = value!.MetaInfo!.ID }, value);
    }

    [HttpPut("{id}", Name = "PutFieldAntiCollisionPolicyAssignmentById")]
    public ActionResult<FieldAntiCollisionPolicyAssignment> Put(Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
        [FromBody] FieldAntiCollisionPolicyAssignmentMutation? request)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("PutFieldAntiCollisionPolicyAssignmentById");
        FieldAntiCollisionPolicyAssignment? value = ToValue(request);
        if (id == Guid.Empty || value?.MetaInfo?.ID != id) return BadRequest(Error("id_mismatch", "Route and payload assignment UUIDs must match."));
        List<string> errors = AntiCollisionPolicyValidation.Validate(value);
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        FieldAntiCollisionPolicyAssignment? current = manager.GetAssignment(id);
        if (current == null) return NotFound(Error("not_found", "The assignment does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (!manager.UpdateAssignment(value!, expectedModifiedUtc))
        {
            FieldAntiCollisionPolicyAssignment? latest = manager.GetAssignment(id);
            if (latest?.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = latest?.LastModificationDate });
            return Conflict(Error("assignment_not_updated", "The policy revision is missing or the Field validity interval overlaps another assignment."));
        }
        return Ok(value);
    }

    [HttpDelete("{id}", Name = "DeleteFutureFieldAntiCollisionPolicyAssignmentById")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult Delete(Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("DeleteFutureFieldAntiCollisionPolicyAssignmentById");
        FieldAntiCollisionPolicyAssignment? current = manager.GetAssignment(id);
        if (current == null) return NotFound(Error("not_found", "The assignment does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (current.ValidFromUtc <= DateTimeOffset.UtcNow)
            return Conflict(Error("historical_assignment_immutable", "An assignment that has already become effective cannot be deleted."));
        return manager.DeleteFutureAssignment(id, expectedModifiedUtc, DateTimeOffset.UtcNow)
            ? NoContent() : Conflict(Error("assignment_not_deleted", "The assignment changed concurrently."));
    }

    private static FieldAntiCollisionPolicyAssignment? ToValue(FieldAntiCollisionPolicyAssignmentMutation? request) => request == null ? null : new()
    {
        MetaInfo = request.MetaInfo,
        FieldID = request.FieldID,
        PolicyRevisionID = request.PolicyRevisionID,
        ValidFromUtc = request.ValidFromUtc,
        ValidToUtc = request.ValidToUtc
    };

    private static object Error(string code, string message) => new { error = code, message };
}
