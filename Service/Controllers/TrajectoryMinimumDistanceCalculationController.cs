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
    public class TrajectoryMinimumDistanceCalculationController : ControllerBase
    {
        private readonly ILogger<TrajectoryMinimumDistanceCalculationManager> _logger;
        private readonly TrajectoryMinimumDistanceCalculationManager _manager;

        public TrajectoryMinimumDistanceCalculationController(ILogger<TrajectoryMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = TrajectoryMinimumDistanceCalculationManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllTrajectoryMinimumDistanceCalculationId")]
        public ActionResult<IEnumerable<Guid>> GetAllTrajectoryMinimumDistanceCalculationId()
        {
            List<Guid>? ids = _manager.GetAllTrajectoryMinimumDistanceCalculationId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllTrajectoryMinimumDistanceCalculationMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllTrajectoryMinimumDistanceCalculationMetaInfo()
        {
            List<MetaInfo?>? values = _manager.GetAllTrajectoryMinimumDistanceCalculationMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetTrajectoryMinimumDistanceCalculationById")]
        public ActionResult<Model.TrajectoryMinimumDistanceCalculation?> GetTrajectoryMinimumDistanceCalculationById(Guid id, [FromQuery] bool includeResults = false)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            Model.TrajectoryMinimumDistanceCalculation? value = _manager.GetTrajectoryMinimumDistanceCalculationById(id, includeResults);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("{id}/Results/ChunkCount", Name = "GetTrajectoryMinimumDistanceCalculationResultChunkCount")]
        public ActionResult<int> GetResultChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetResultChunkCount(id));
        }

        [HttpGet("{id}/Results/Chunks/{chunkIndex}", Name = "GetTrajectoryMinimumDistanceCalculationResultChunk")]
        public ActionResult<Model.TrajectoryMinimumDistanceResultChunk?> GetResultChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            Model.TrajectoryMinimumDistanceResultChunk? value = _manager.GetResultChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("LightData", Name = "GetAllTrajectoryMinimumDistanceCalculationLight")]
        public ActionResult<IEnumerable<Model.TrajectoryMinimumDistanceCalculationLight>> GetAllTrajectoryMinimumDistanceCalculationLight()
        {
            List<Model.TrajectoryMinimumDistanceCalculationLight>? values = _manager.GetAllTrajectoryMinimumDistanceCalculationLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllTrajectoryMinimumDistanceCalculation")]
        public ActionResult<IEnumerable<Model.TrajectoryMinimumDistanceCalculation?>> GetAllTrajectoryMinimumDistanceCalculation()
        {
            List<Model.TrajectoryMinimumDistanceCalculation?>? values = _manager.GetAllTrajectoryMinimumDistanceCalculation();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostTrajectoryMinimumDistanceCalculation")]
        public async Task<ActionResult> PostTrajectoryMinimumDistanceCalculation([FromBody] Model.TrajectoryMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The given TrajectoryMinimumDistanceCalculation is null, badly formed, or its ID is empty");
                return BadRequest();
            }

            if (_manager.GetTrajectoryMinimumDistanceCalculationById(data.MetaInfo.ID, includeResults: false) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return await _manager.AddTrajectoryMinimumDistanceCalculation(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutTrajectoryMinimumDistanceCalculationById")]
        public async Task<ActionResult> PutTrajectoryMinimumDistanceCalculationById(Guid id, [FromBody] Model.TrajectoryMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return await _manager.UpdateTrajectoryMinimumDistanceCalculationById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryMinimumDistanceCalculationById")]
        public ActionResult DeleteTrajectoryMinimumDistanceCalculationById(Guid id)
        {
            if (_manager.GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return _manager.DeleteTrajectoryMinimumDistanceCalculationById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
