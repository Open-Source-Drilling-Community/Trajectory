using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryBatchImportController : ControllerBase
    {
        private readonly ILogger<TrajectoryBatchImportManager> _logger;
        private readonly TrajectoryBatchImportManager _manager;

        public TrajectoryBatchImportController(ILogger<TrajectoryBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = TrajectoryBatchImportManager.GetInstance(logger, connectionManager);
        }

        [HttpGet(Name = "GetAllTrajectoryBatchImportId")]
        public ActionResult<IEnumerable<Guid>> GetAllTrajectoryBatchImportId()
        {
            var values = _manager.GetAllTrajectoryBatchImportId();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllTrajectoryBatchImportMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllTrajectoryBatchImportMetaInfo()
        {
            var values = _manager.GetAllTrajectoryBatchImportMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("LightData", Name = "GetAllTrajectoryBatchImportLight")]
        public ActionResult<IEnumerable<TrajectoryBatchImportLight>> GetAllTrajectoryBatchImportLight()
        {
            var values = _manager.GetAllTrajectoryBatchImportLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllTrajectoryBatchImport")]
        public ActionResult<IEnumerable<TrajectoryBatchImport>> GetAllTrajectoryBatchImport()
        {
            var values = _manager.GetAllTrajectoryBatchImport();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetTrajectoryBatchImportById")]
        public ActionResult<TrajectoryBatchImport?> GetTrajectoryBatchImportById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var value = _manager.GetTrajectoryBatchImportById(id);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpPost(Name = "PostTrajectoryBatchImport")]
        public ActionResult PostTrajectoryBatchImport([FromBody] TrajectoryBatchImport? data)
        {
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                if (_manager.GetTrajectoryBatchImportById(data.MetaInfo.ID) == null)
                {
                    return _manager.AddTrajectoryBatchImport(data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }

                _logger.LogWarning("The given TrajectoryBatchImport already exists and will not be added");
                return StatusCode(StatusCodes.Status409Conflict);
            }

            _logger.LogWarning("The given TrajectoryBatchImport is null, badly formed, or its ID is empty");
            return BadRequest();
        }

        [HttpPut("{id}", Name = "PutTrajectoryBatchImportById")]
        public ActionResult PutTrajectoryBatchImportById(Guid id, [FromBody] TrajectoryBatchImport? data)
        {
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID == id)
            {
                if (_manager.GetTrajectoryBatchImportById(id) != null)
                {
                    return _manager.UpdateTrajectoryBatchImportById(id, data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }

                _logger.LogWarning("The given TrajectoryBatchImport has not been found in the database");
                return NotFound();
            }

            _logger.LogWarning("The given TrajectoryBatchImport is null, badly formed, or does not match the ID to update");
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryBatchImportById")]
        public ActionResult DeleteTrajectoryBatchImportById(Guid id)
        {
            if (_manager.GetTrajectoryBatchImportById(id) != null)
            {
                return _manager.DeleteTrajectoryBatchImportById(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogWarning("The TrajectoryBatchImport of given ID does not exist");
            return NotFound();
        }
    }
}
