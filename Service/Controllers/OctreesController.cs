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
        private readonly OctreeSearchJobWorker _searchJobWorker;


        public OctreesController(ILogger<TrajectoryManager> loggerTrajectory, ILogger<OctreeManager> loggerOctree,
            Managers.SqlConnectionManager connectionManagerTrajectory, OctreeManager octreeManager,
            OctreeSearchJobWorker searchJobWorker)
        {
            _loggerTrajectory = loggerTrajectory;
            _trajectoryManager = TrajectoryManager.GetInstance(_loggerTrajectory, connectionManagerTrajectory, octreeManager);
            
            _loggerOctree = loggerOctree;
            _octreeManager = octreeManager;
            _searchJobWorker = searchJobWorker;
        }

        // GET api/Octrees
        [HttpGet]
        public IEnumerable<Guid> Get([FromQuery] Model.TrajectoryType? trajectoryType = null,
            [FromQuery] bool? isDefinitive = null)
        {
            var ids = _octreeManager.GetIDs(trajectoryType, isDefinitive);
            return ids;
        }
        // GET api/Octrees/id
        [HttpGet("{id}")]
        public ActionResult<List<OctreeCodeLong>> Get(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            return _octreeManager.Contains(id) ? Ok(_octreeManager.Get(id)) : NotFound();
        }

        // GET api/Octrees/id/Status
        [HttpGet("{id}/Status", Name = "GetOctreeIndexStatus")]
        public ActionResult<Model.OctreeIndexStatus> GetStatus(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(id);
            return trajectory == null ? NotFound() : Ok(_octreeManager.GetStatus(trajectory));
        }

        // POST api/Octrees/SearchJobs
        [HttpPost("SearchJobs", Name = "QueueOctreeSearch")]
        public ActionResult<Model.OctreeSearchJobStatus> QueueSearch([FromBody] Model.OctreeSearchJobRequest request)
        {
            if (request == null || request.ReferenceTrajectoryID == Guid.Empty ||
                !request.IncludePlanned && !request.IncludeActual)
            {
                return BadRequest(new { error = "invalid_octree_search" });
            }

            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(request.ReferenceTrajectoryID);
            if (trajectory == null)
            {
                return NotFound();
            }

            Model.OctreeIndexStatus status = _octreeManager.GetStatus(trajectory);
            if (!status.IsCurrent)
            {
                return Conflict(new { error = "octree_index_not_current", state = status.State.ToString() });
            }

            Model.OctreeSearchJobStatus? job = _searchJobWorker.Queue(request);
            return job == null
                ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "octree_search_queue_full" })
                : Ok(job);
        }

        // GET api/Octrees/SearchJobs/jobId/Status
        [HttpGet("SearchJobs/{jobId}/Status", Name = "GetOctreeSearchStatus")]
        public ActionResult<Model.OctreeSearchJobStatus> GetSearchStatus(Guid jobId)
        {
            if (jobId == Guid.Empty) return BadRequest();
            Model.OctreeSearchJobStatus? status = _searchJobWorker.GetStatus(jobId);
            return status == null ? NotFound() : Ok(status);
        }

        // GET api/Octrees/SearchJobs/jobId/Result
        [HttpGet("SearchJobs/{jobId}/Result", Name = "GetOctreeSearchResult")]
        public ActionResult<Model.OctreeSearchJobResult> GetSearchResult(Guid jobId)
        {
            if (jobId == Guid.Empty) return BadRequest();
            if (!_searchJobWorker.Contains(jobId)) return NotFound();
            Model.OctreeSearchJobResult? result = _searchJobWorker.GetResult(jobId);
            return result == null
                ? Conflict(new { error = "octree_search_not_completed" })
                : Ok(result);
        }

        // DELETE api/Octrees/SearchJobs/jobId
        [HttpDelete("SearchJobs/{jobId}", Name = "DeleteOctreeSearch")]
        public ActionResult DeleteSearch(Guid jobId)
        {
            if (jobId == Guid.Empty) return BadRequest();
            return _searchJobWorker.Delete(jobId) ? NoContent() : NotFound();
        }
        // POST api/Octrees
        [HttpPost("{id}")]
        public ActionResult<Model.OctreeIndexStatus> Post(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            if (_octreeManager.Contains(id)) return Conflict(new { error = "octree_index_already_exists" });

            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(id);
            if (trajectory == null) return NotFound();
            if (_octreeManager.Rebuild(trajectory))
            {
                return Ok(_octreeManager.GetStatus(trajectory));
            }
            return UnprocessableEntity(new { error = "trajectory_has_no_indexable_uncertainty_envelope" });
        }
        // PUT api/Octrees/id
        [HttpPut("{id}")]
        public ActionResult<Model.OctreeIndexStatus> Put(Guid id)
        {
            if (id == Guid.Empty) return BadRequest();
            Model.Trajectory? trajectory = _trajectoryManager.GetTrajectoryById(id);
            if (trajectory == null) return NotFound();
            if (_octreeManager.Rebuild(trajectory))
            {
                return Ok(_octreeManager.GetStatus(trajectory));
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
            return _octreeManager.Delete(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "octree_index_delete_failed" });
        }
    }
}
