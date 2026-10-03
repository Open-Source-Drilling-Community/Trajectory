using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class TargetLandingCaseController : ControllerBase
{
    private static readonly JsonSerializerOptions CompactResponseJson = CreateCompactResponseJson();
    private readonly TargetLandingCaseManager manager_;
    private readonly TargetLandingCalculationWorker worker_;
    public TargetLandingCaseController(ILogger<TargetLandingCaseManager> logger, SqlConnectionManager connectionManager,
        TargetLandingCalculationWorker worker)
    {
        manager_ = TargetLandingCaseManager.GetInstance(logger, connectionManager);
        worker_ = worker;
    }

    [HttpGet(Name = "GetAllTargetLandingCaseId")]
    [ProducesResponseType<IEnumerable<Guid>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<Guid>> GetAllIds() { UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllTargetLandingCaseId"); return manager_.GetAllIds() is { } values ? Ok(values) : Problem(); }

    [HttpGet("MetaInfo", Name = "GetAllTargetLandingCaseMetaInfo")]
    [ProducesResponseType<IEnumerable<MetaInfo>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<MetaInfo>> GetAllMetaInfo() { UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllTargetLandingCaseMetaInfo"); return manager_.GetAllMetaInfo() is { } values ? Ok(values) : Problem(); }

    [HttpGet("LightData", Name = "GetAllTargetLandingCaseLight")]
    [ProducesResponseType<IEnumerable<TargetLandingCaseLight>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TargetLandingCaseLight>> GetAllLight() { UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllTargetLandingCaseLight"); return manager_.GetAllLight() is { } values ? Ok(values) : Problem(); }

    [HttpGet("HeavyData", Name = "GetAllTargetLandingCase")]
    [ProducesResponseType<IEnumerable<TargetLandingCase>>(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TargetLandingCase>> GetAll() { UsageStatisticsTrajectory.Instance.IncrementOperation("GetAllTargetLandingCase"); return manager_.GetAll() is { } values ? Ok(values) : Problem(); }

    [HttpGet("{id}", Name = "GetTargetLandingCaseById")]
    [ProducesResponseType<TargetLandingCase>(StatusCodes.Status200OK)]
    public ActionResult<TargetLandingCase> GetById(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetTargetLandingCaseById");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty target-landing-case UUID is required."));
        return manager_.GetById(id) is { } value ? Ok(value) : NotFound(Error("not_found", "The target landing case does not exist."));
    }

    [HttpGet("{id}/EditData", Name = "GetTargetLandingCaseEditData")]
    [ProducesResponseType<TargetLandingCase>(StatusCodes.Status200OK)]
    public ActionResult<TargetLandingCase> GetEditData(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetTargetLandingCaseEditData");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty target-landing-case UUID is required."));
        return manager_.GetEditById(id) is { } value
            ? new JsonResult(value, CompactResponseJson)
            : NotFound(Error("not_found", "The target landing case does not exist."));
    }

    [HttpGet("{id}/DisplayData", Name = "GetTargetLandingCaseDisplayData")]
    [ProducesResponseType<TargetLandingCase>(StatusCodes.Status200OK)]
    public ActionResult<TargetLandingCase> GetDisplayData(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetTargetLandingCaseDisplayData");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty target-landing-case UUID is required."));
        return manager_.GetDisplayById(id) is { } value
            ? new JsonResult(value, CompactResponseJson)
            : NotFound(Error("not_found", "The target landing case does not exist."));
    }

    [HttpGet("{id}/Status", Name = "GetTargetLandingCaseStatus")]
    [ProducesResponseType<TargetLandingCaseLight>(StatusCodes.Status200OK)]
    public ActionResult<TargetLandingCaseLight> GetStatus(Guid id)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("GetTargetLandingCaseStatus");
        if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty target-landing-case UUID is required."));
        return manager_.GetLightById(id) is { } value ? Ok(value) : NotFound(Error("not_found", "The target landing case does not exist."));
    }

    [HttpPost(Name = "PostTargetLandingCase")]
    [ProducesResponseType<TargetLandingCase>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TargetLandingCase>> Post([FromBody] TargetLandingCase? value)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("PostTargetLandingCase");
        List<string> errors = TargetLandingCalculator.Validate(value);
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        Guid id = value!.MetaInfo!.ID;
        if (manager_.GetById(id) != null) return Conflict(Error("already_exists", "A target landing case with this UUID already exists."));
        if (!await manager_.AddAsync(value)) return Problem("The target landing case could not be saved.");
        worker_.Queue(id, value.LastModificationDate!.Value);
        return AcceptedAtAction(nameof(GetStatus), new { id }, value);
    }

    [HttpPut("{id}", Name = "PutTargetLandingCaseById")]
    [ProducesResponseType<TargetLandingCase>(StatusCodes.Status202Accepted)]
    public async Task<ActionResult<TargetLandingCase>> Put(Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
        [FromBody] TargetLandingCase? value)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("PutTargetLandingCaseById");
        if (id == Guid.Empty || value?.MetaInfo?.ID != id)
            return BadRequest(Error("id_mismatch", "The route and payload UUIDs must be the same non-empty value."));
        List<string> errors = TargetLandingCalculator.Validate(value);
        if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
        TargetLandingCase? current = manager_.GetById(id);
        if (current == null) return NotFound(Error("not_found", "The target landing case does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc)
            return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (!await manager_.UpdateAsync(id, expectedModifiedUtc, value))
        {
            TargetLandingCase? latest = manager_.GetById(id);
            if (latest != null && latest.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = latest.LastModificationDate });
            return Problem("The target landing case could not be saved.");
        }
        worker_.Queue(id, value.LastModificationDate!.Value);
        return AcceptedAtAction(nameof(GetStatus), new { id }, value);
    }

    [HttpDelete("{id}", Name = "DeleteTargetLandingCaseById")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult Delete(Guid id,
        [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
    {
        UsageStatisticsTrajectory.Instance.IncrementOperation("DeleteTargetLandingCaseById");
        TargetLandingCase? current = manager_.GetById(id);
        if (current == null) return NotFound(Error("not_found", "The target landing case does not exist."));
        if (current.LastModificationDate != expectedModifiedUtc)
            return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (manager_.Delete(id, expectedModifiedUtc)) return NoContent();
        TargetLandingCase? latest = manager_.GetById(id);
        if (latest != null && latest.LastModificationDate != expectedModifiedUtc)
            return Conflict(new { error = "stale_write", currentModifiedUtc = latest.LastModificationDate });
        return Problem("The target landing case could not be deleted.");
    }

    private static object Error(string code, string message) => new { error = code, message };

    private static JsonSerializerOptions CreateCompactResponseJson()
    {
        JsonSerializerOptions options = new();
        JsonSettings.ApplyTo(options);
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }
}
