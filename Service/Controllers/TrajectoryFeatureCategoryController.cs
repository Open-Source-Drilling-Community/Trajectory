using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json"), Route("[controller]"), ApiController]
public class TrajectoryFeatureCategoryController : ControllerBase
{
    private readonly TrajectoryFeatureCategoryManager manager;
    public TrajectoryFeatureCategoryController(TrajectoryFeatureCategoryManager manager) => this.manager = manager;

    [HttpGet(Name = "GetAllTrajectoryFeatureCategoryId")]
    public ActionResult<IEnumerable<Guid>> GetAllIds() => Ok(manager.GetAll().Select(value => value.MetaInfo!.ID));
    [HttpGet("MetaInfo", Name = "GetAllTrajectoryFeatureCategoryMetaInfo")]
    public ActionResult<IEnumerable<MetaInfo?>> GetAllMetaInfo() => Ok(manager.GetAll().Select(value => value.MetaInfo));
    [HttpGet("HeavyData", Name = "GetAllTrajectoryFeatureCategory")]
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
