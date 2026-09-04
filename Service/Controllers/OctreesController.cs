using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OSDC.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class OctreesController : ControllerBase
    {
        private readonly ILogger<TrajectoryManager> _loggerTrajectory;
        private readonly ILogger<OctreeManager> _loggerOctree;
        private readonly TrajectoryManager _trajectoryManager;
        private readonly OctreeManager _octreeManager;


        public OctreesController(ILogger<TrajectoryManager> loggerTrajectory, ILogger<OctreeManager> loggerOctree,
            Managers.SqlConnectionManager connectionManagerTrajectory, OctreeManager octreeManager)
        {
            _loggerTrajectory = loggerTrajectory;
            _trajectoryManager = TrajectoryManager.GetInstance(_loggerTrajectory, connectionManagerTrajectory, octreeManager);
            
            _loggerOctree = loggerOctree;
            _octreeManager = octreeManager;
        }

        // GET api/Octrees
        [HttpGet]
        public IEnumerable<Guid> Get()
        {
            var ids = _octreeManager.GetIDs();
            return ids;
        }
        // GET api/Octrees/id
        [HttpGet("{id}")]
        public List<OctreeCodeLong> Get(Guid id)
        {
            return _octreeManager.Get(id);
        }
        // POST api/Octrees
        [HttpPost("{id}")]
        public ActionResult Post(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            if (_octreeManager.Contains(id)) return Conflict();

            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(id);
            if (trajectory == null) return NotFound();
            if (_octreeManager.Rebuild(trajectory))
            {
                return Ok();
            }
            return UnprocessableEntity(new { error = "trajectory_has_no_indexable_uncertainty_envelope" });
        }
        // PUT api/Octrees/id
        [HttpPut("{id}")]
        public ActionResult Put(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(id);
            if (trajectory == null) return NotFound();
            if (_octreeManager.Rebuild(trajectory))
            {
                return Ok();
            }
            _octreeManager.Delete(id);
            return UnprocessableEntity(new { error = "trajectory_has_no_indexable_uncertainty_envelope" });
        }
        // DELETE api/Octrees/id
        [HttpDelete("{id}")]
        public ActionResult Delete(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            if (!_octreeManager.Contains(id)) return NotFound();
            return _octreeManager.Delete(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
