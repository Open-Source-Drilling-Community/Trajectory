using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class InterpolatedTrajectoryController : ControllerBase
    {
        private readonly ILogger<InterpolatedTrajectoryManager> _logger;
        private readonly InterpolatedTrajectoryManager _manager;

        public InterpolatedTrajectoryController(ILogger<InterpolatedTrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = InterpolatedTrajectoryManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllInterpolatedTrajectoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllInterpolatedTrajectoryId()
        {
            var ids = _manager.GetAllInterpolatedTrajectoryId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllInterpolatedTrajectoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllInterpolatedTrajectoryMetaInfo()
        {
            var vals = _manager.GetAllInterpolatedTrajectoryMetaInfo();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetInterpolatedTrajectoryById")]
        public ActionResult<Model.InterpolatedTrajectory?> GetInterpolatedTrajectoryById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var val = _manager.GetInterpolatedTrajectoryById(id);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("Trajectory/{trajectoryId}", Name = "GetInterpolatedTrajectoryByTrajectoryId")]
        public ActionResult<Model.InterpolatedTrajectory?> GetInterpolatedTrajectoryByTrajectoryId(Guid trajectoryId)
        {
            if (trajectoryId == Guid.Empty)
            {
                return BadRequest();
            }

            var val = _manager.GetInterpolatedTrajectoryByTrajectoryId(trajectoryId);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("LightData", Name = "GetAllInterpolatedTrajectoryLight")]
        public ActionResult<IEnumerable<Model.InterpolatedTrajectoryLight>> GetAllInterpolatedTrajectoryLight()
        {
            var vals = _manager.GetAllInterpolatedTrajectoryLight();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllInterpolatedTrajectory")]
        public ActionResult<IEnumerable<Model.InterpolatedTrajectory?>> GetAllInterpolatedTrajectory()
        {
            var vals = _manager.GetAllInterpolatedTrajectory();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostInterpolatedTrajectory")]
        public async Task<ActionResult> PostInterpolatedTrajectory([FromBody] Model.InterpolatedTrajectory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The given InterpolatedTrajectory is null, badly formed, or its ID is empty");
                return BadRequest();
            }

            if (_manager.GetInterpolatedTrajectoryById(data.MetaInfo.ID) != null)
            {
                _logger.LogWarning("The given InterpolatedTrajectory already exists and will not be added");
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return await _manager.AddInterpolatedTrajectory(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutInterpolatedTrajectoryById")]
        public async Task<ActionResult> PutInterpolatedTrajectoryById(Guid id, [FromBody] Model.InterpolatedTrajectory? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                _logger.LogWarning("The given InterpolatedTrajectory is null, badly formed, or does not match the ID to update");
                return BadRequest();
            }

            if (_manager.GetInterpolatedTrajectoryById(id) == null)
            {
                _logger.LogWarning("The given InterpolatedTrajectory has not been found in the database");
                return NotFound();
            }

            return await _manager.UpdateInterpolatedTrajectoryById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteInterpolatedTrajectoryById")]
        public ActionResult DeleteInterpolatedTrajectoryById(Guid id)
        {
            if (_manager.GetInterpolatedTrajectoryById(id) == null)
            {
                _logger.LogWarning("The InterpolatedTrajectory of given ID does not exist");
                return NotFound();
            }

            return _manager.DeleteInterpolatedTrajectoryById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
