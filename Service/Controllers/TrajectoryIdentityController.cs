using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json"), Route("[controller]"), ApiController]
public class TrajectoryIdentityController : ControllerBase
{
    private readonly TrajectoryIdentityManager manager;
    public TrajectoryIdentityController(TrajectoryIdentityManager manager) => this.manager = manager;

    [HttpGet(Name = "GetAllTrajectoryIdentityId")]
    public ActionResult<IEnumerable<Guid>> GetAllIds() => Ok(manager.GetAll().Select(value => value.MetaInfo!.ID));
    [HttpGet("MetaInfo", Name = "GetAllTrajectoryIdentityMetaInfo")]
    public ActionResult<IEnumerable<MetaInfo?>> GetAllMetaInfo() => Ok(manager.GetAll().Select(value => value.MetaInfo));
    [HttpGet("HeavyData", Name = "GetAllTrajectoryIdentity")]
    public ActionResult<IEnumerable<TrajectoryIdentity>> GetAll() => Ok(manager.GetAll());
    [HttpGet("{id}", Name = "GetTrajectoryIdentityById")]
    public ActionResult<TrajectoryIdentity> Get(Guid id) => manager.Get(id) is { } value ? Ok(value) : NotFound();
    [HttpPost(Name = "PostTrajectoryIdentity")]
    public ActionResult Post([FromBody] TrajectoryIdentity? value) => value?.MetaInfo?.ID is Guid id && id != Guid.Empty
        ? manager.Add(value) ? Ok(value) : Conflict() : BadRequest();
    [HttpPut("{id}", Name = "PutTrajectoryIdentityById")]
    public ActionResult Put(Guid id, [FromQuery] DateTimeOffset expectedModifiedUtc, [FromBody] TrajectoryIdentity? value)
    {
        TrajectoryIdentity? current = manager.Get(id);
        if (value?.MetaInfo?.ID != id) return BadRequest();
        if (current == null) return NotFound();
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        return manager.Update(id, value) ? Ok(value) : Conflict();
    }
    [HttpDelete("{id}", Name = "DeleteTrajectoryIdentityById")]
    public ActionResult Delete(Guid id, [FromQuery] DateTimeOffset expectedModifiedUtc)
    {
        TrajectoryIdentity? current = manager.Get(id);
        if (current == null) return NotFound();
        if (current.LastModificationDate != expectedModifiedUtc) return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
        if (manager.IsReferenced(id)) return Conflict(new { error = "catalog_in_use" });
        return manager.Delete(id) ? Ok() : StatusCode(500);
    }
}
