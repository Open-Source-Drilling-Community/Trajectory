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
    public class TrajectoryRealizationCaseController : ControllerBase
    {
        private readonly ILogger<TrajectoryRealizationCaseManager> _logger;
        private readonly TrajectoryRealizationCaseManager _manager;

        public TrajectoryRealizationCaseController(ILogger<TrajectoryRealizationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = TrajectoryRealizationCaseManager.GetInstance(logger, connectionManager);
        }

        [HttpGet(Name = "GetAllTrajectoryRealizationCaseId")]
        public ActionResult<IEnumerable<Guid>> GetAllTrajectoryRealizationCaseId()
        {
            List<Guid>? ids = _manager.GetAllTrajectoryRealizationCaseId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllTrajectoryRealizationCaseMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllTrajectoryRealizationCaseMetaInfo()
        {
            List<MetaInfo?>? values = _manager.GetAllTrajectoryRealizationCaseMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetTrajectoryRealizationCaseById")]
        public ActionResult<Model.TrajectoryRealizationCase?> GetTrajectoryRealizationCaseById(Guid id, [FromQuery] bool includeRealizations = false)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            Model.TrajectoryRealizationCase? value = _manager.GetTrajectoryRealizationCaseById(id, includeRealizations);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("LightData", Name = "GetAllTrajectoryRealizationCaseLight")]
        public ActionResult<IEnumerable<Model.TrajectoryRealizationCaseLight>> GetAllTrajectoryRealizationCaseLight()
        {
            List<Model.TrajectoryRealizationCaseLight>? values = _manager.GetAllTrajectoryRealizationCaseLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllTrajectoryRealizationCase")]
        public ActionResult<IEnumerable<Model.TrajectoryRealizationCase?>> GetAllTrajectoryRealizationCase()
        {
            List<Model.TrajectoryRealizationCase?>? values = _manager.GetAllTrajectoryRealizationCase();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}/Realizations/ChunkCount", Name = "GetTrajectoryRealizationChunkCount")]
        public ActionResult<int> GetTrajectoryRealizationChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetTrajectoryRealizationChunkCount(id));
        }

        [HttpGet("{id}/Realizations/Chunks/{chunkIndex}", Name = "GetTrajectoryRealizationChunk")]
        public ActionResult<Model.TrajectoryRealizationChunk?> GetTrajectoryRealizationChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            Model.TrajectoryRealizationChunk? value = _manager.GetTrajectoryRealizationChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpPost(Name = "PostTrajectoryRealizationCase")]
        public async Task<ActionResult> PostTrajectoryRealizationCase([FromBody] Model.TrajectoryRealizationCase? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The given TrajectoryRealizationCase is null, badly formed, or its ID is empty");
                return BadRequest();
            }

            if (_manager.GetTrajectoryRealizationCaseById(data.MetaInfo.ID) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return await _manager.AddTrajectoryRealizationCase(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutTrajectoryRealizationCaseById")]
        public async Task<ActionResult> PutTrajectoryRealizationCaseById(Guid id, [FromBody] Model.TrajectoryRealizationCase? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetTrajectoryRealizationCaseById(id) == null)
            {
                return NotFound();
            }

            return await _manager.UpdateTrajectoryRealizationCaseById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryRealizationCaseById")]
        public ActionResult DeleteTrajectoryRealizationCaseById(Guid id)
        {
            if (_manager.GetTrajectoryRealizationCaseById(id) == null)
            {
                return NotFound();
            }

            return _manager.DeleteTrajectoryRealizationCaseById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
