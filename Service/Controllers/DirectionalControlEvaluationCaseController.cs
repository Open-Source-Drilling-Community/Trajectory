using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class DirectionalControlEvaluationCaseController : ControllerBase
{
    private readonly DirectionalControlEvaluationCaseManager manager_;
    private readonly DirectionalControlEvaluationCalculationWorker worker_;

    public DirectionalControlEvaluationCaseController(
        ILogger<DirectionalControlEvaluationCaseManager> logger,
        SqlConnectionManager connectionManager,
        DirectionalControlEvaluationCalculationWorker worker)
    {
        manager_ = DirectionalControlEvaluationCaseManager.GetInstance(logger, connectionManager);
        worker_ = worker;
    }

    [HttpGet(Name = "GetAllDirectionalControlEvaluationCaseId")]
    [ProducesResponseType<IEnumerable<Guid>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Guid>> GetAllIds()
    {
        Count("GetAllDirectionalControlEvaluationCaseId");
        return manager_.GetAllIds() is { } values ? Ok(values) : Problem();
    }

    [HttpGet("LightData", Name = "GetAllDirectionalControlEvaluationCaseLight")]
    [ProducesResponseType<IEnumerable<DirectionalControlEvaluationCaseLight>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DirectionalControlEvaluationCaseLight>> GetAllLight()
    {
        Count("GetAllDirectionalControlEvaluationCaseLight");
        return manager_.GetAllLight() is { } values ? Ok(values) : Problem();
    }

    [HttpGet("{id}", Name = "GetDirectionalControlEvaluationCaseById")]
    [ProducesResponseType<DirectionalControlEvaluationCase>(StatusCodes.Status200OK)]
    public ActionResult<DirectionalControlEvaluationCase> GetById(Guid id)
    {
        Count("GetDirectionalControlEvaluationCaseById");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty case UUID is required."));
        return manager_.GetById(id) is { } value
            ? Ok(value)
            : NotFound(Error("not_found", "The directional-control evaluation case does not exist."));
    }

    [HttpGet("{id}/Status", Name = "GetDirectionalControlEvaluationCaseStatus")]
    [ProducesResponseType<DirectionalControlEvaluationCaseLight>(StatusCodes.Status200OK)]
    public ActionResult<DirectionalControlEvaluationCaseLight> GetStatus(Guid id)
    {
        Count("GetDirectionalControlEvaluationCaseStatus");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty case UUID is required."));
        return manager_.GetLightById(id) is { } value
            ? Ok(value)
            : NotFound(Error("not_found", "The directional-control evaluation case does not exist."));
    }

    [HttpGet("{id}/Samples/ChunkCount", Name = "GetDirectionalControlEvaluationSampleChunkCount")]
    [ProducesResponseType<int>(StatusCodes.Status200OK)]
    public ActionResult<int> GetSampleChunkCount(Guid id)
    {
        Count("GetDirectionalControlEvaluationSampleChunkCount");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty case UUID is required."));
        if (manager_.GetById(id) == null) return NotFound(Error("not_found", "The case does not exist."));
        return manager_.GetSampleChunkCount(id) is int count ? Ok(count) : Problem();
    }

    [HttpGet("{id}/Samples/Chunks/{chunkIndex:int}", Name = "GetDirectionalControlEvaluationSampleChunk")]
    [ProducesResponseType<DirectionalControlEvaluationSampleChunk>(StatusCodes.Status200OK)]
    public ActionResult<DirectionalControlEvaluationSampleChunk> GetSampleChunk(Guid id, int chunkIndex)
    {
        Count("GetDirectionalControlEvaluationSampleChunk");
        if (id == Guid.Empty || chunkIndex < 0)
            return BadRequest(Error("invalid_chunk", "A non-empty case UUID and non-negative chunk index are required."));
        return manager_.GetSampleChunk(id, chunkIndex) is { } value
            ? Ok(value)
            : NotFound(Error("not_found", "The sample chunk does not exist."));
    }

    [HttpPost(Name = "PostDirectionalControlEvaluationCase")]
    [ProducesResponseType<DirectionalControlEvaluationCase>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<DirectionalControlEvaluationCase>> Post(
        [FromBody] DirectionalControlEvaluationCase? value)
    {
        Count("PostDirectionalControlEvaluationCase");
        List<string> errors = DirectionalControlEvaluationValidation.Validate(value);
        if (value != null) errors.AddRange(manager_.ValidateReferences(value));
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        Guid id = value!.MetaInfo!.ID;
        if (manager_.GetById(id) != null)
            return Conflict(Error("already_exists", "A case with this UUID already exists."));
        if (!await manager_.AddAsync(value)) return Problem("The case could not be saved.");
        worker_.Queue(id, value.LastModificationDate!.Value);
        return AcceptedAtAction(nameof(GetStatus), new { id }, value);
    }

    [HttpPut("{id}", Name = "PutDirectionalControlEvaluationCaseById")]
    [ProducesResponseType<DirectionalControlEvaluationCase>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<DirectionalControlEvaluationCase>> Put(
        Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
        [FromBody] DirectionalControlEvaluationCase? value)
    {
        Count("PutDirectionalControlEvaluationCaseById");
        if (id == Guid.Empty || value?.MetaInfo?.ID != id)
            return BadRequest(Error("id_mismatch", "The route and payload UUIDs must match."));
        List<string> errors = DirectionalControlEvaluationValidation.Validate(value);
        errors.AddRange(manager_.ValidateReferences(value));
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        DirectionalControlEvaluationCase? current = manager_.GetById(id);
        if (current == null) return NotFound(Error("not_found", "The case does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc)
            return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (!await manager_.UpdateAsync(id, expectedModifiedUtc, value))
        {
            DirectionalControlEvaluationCase? latest = manager_.GetById(id);
            if (latest?.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = latest?.LastModificationDate });
            return Problem("The case could not be saved.");
        }
        worker_.Queue(id, value.LastModificationDate!.Value);
        return AcceptedAtAction(nameof(GetStatus), new { id }, value);
    }

    [HttpDelete("{id}", Name = "DeleteDirectionalControlEvaluationCaseById")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult Delete(
        Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
    {
        Count("DeleteDirectionalControlEvaluationCaseById");
        DirectionalControlEvaluationCase? current = manager_.GetById(id);
        if (current == null) return NotFound(Error("not_found", "The case does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc)
            return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (manager_.Delete(id, expectedModifiedUtc)) return NoContent();
        return Problem("The case could not be deleted.");
    }

    private static object Error(string code, string message) => new { error = code, message };
    private static void Count(string operation) => UsageStatisticsTrajectory.Instance.IncrementOperation(operation);
}
