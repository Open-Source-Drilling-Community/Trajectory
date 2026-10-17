using OSDC.Drilling.Trajectory.Service;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json"), Route("[controller]"), ApiController]
public class TrajectoryFeatureCategoryController : ControllerBase
{
        /// <summary>Discover TrajectoryFeatureCategory resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverTrajectoryFeatureCategory")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.TrajectoryFeatureCategoryDiscoveryPage> DiscoverTrajectoryFeatureCategory([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.TrajectoryFeatureCategoryLight>(manager.DiscoveryConnections.GetConnection(), "TrajectoryFeatureCategoryTable", "TrajectoryFeatureCategory", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> {  }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.TrajectoryFeatureCategoryDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

    private readonly TrajectoryFeatureCategoryManager manager;
    public TrajectoryFeatureCategoryController(TrajectoryFeatureCategoryManager manager) => this.manager = manager;

    [HttpGet(Name = "GetAllTrajectoryFeatureCategoryId")]
    public ActionResult<IEnumerable<Guid>> GetAllIds() => Ok(manager.GetAll().Select(value => value.MetaInfo!.ID));
    [HttpGet("MetaInfo", Name = "GetAllTrajectoryFeatureCategoryMetaInfo")]
    public ActionResult<IEnumerable<MetaInfo?>> GetAllMetaInfo() => Ok(manager.GetAll().Select(value => value.MetaInfo));
    [NonAction]
        public ActionResult<IEnumerable<TrajectoryFeatureCategory>> GetAll() => Ok(manager.GetAll());
    [HttpGet("{id}", Name = "GetTrajectoryFeatureCategoryById")]
    public ActionResult<TrajectoryFeatureCategory> Get(Guid id) => manager.Get(id) is { } value ? Ok(value) : NotFound();
    [HttpPost(Name = "PostTrajectoryFeatureCategory")]
    public ActionResult Post([FromBody] TrajectoryFeatureCategory? value) => value?.MetaInfo?.ID is Guid id && id != Guid.Empty
        ? manager.Add(value) ? Ok(value) : Conflict() : BadRequest();
    [HttpPut("{id}", Name = "PutTrajectoryFeatureCategoryById")]
    public ActionResult Put(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] TrajectoryFeatureCategory? value)
    {
        TrajectoryFeatureCategory? current = manager.Get(id);
        if (value?.MetaInfo?.ID != id) return BadRequest();
        if (current == null) return NotFound();
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        return manager.Update(id, value) ? Ok(value) : Conflict(new { error = "catalog_in_use_or_invalid" });
    }
    [HttpDelete("{id}", Name = "DeleteTrajectoryFeatureCategoryById")]
    public ActionResult Delete(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
    {
        TrajectoryFeatureCategory? current = manager.Get(id);
        if (current == null) return NotFound();
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (manager.IsReferenced(id)) return Conflict(new { error = "catalog_in_use" });
        return manager.Delete(id) ? Ok() : StatusCode(500);
    }
}
