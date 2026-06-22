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
    public class TrajectoryAggregationCaseController : ControllerBase
    {
        private readonly ILogger<TrajectoryAggregationCaseManager> _logger;
        private readonly TrajectoryAggregationCaseManager _manager;

        public TrajectoryAggregationCaseController(ILogger<TrajectoryAggregationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _manager = TrajectoryAggregationCaseManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllTrajectoryAggregationCaseId")]
        public ActionResult<IEnumerable<Guid>> GetAllTrajectoryAggregationCaseId()
        {
            var ids = _manager.GetAllTrajectoryAggregationCaseId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllTrajectoryAggregationCaseMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllTrajectoryAggregationCaseMetaInfo()
        {
            var vals = _manager.GetAllTrajectoryAggregationCaseMetaInfo();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetTrajectoryAggregationCaseById")]
        public ActionResult<Model.TrajectoryAggregationCase?> GetTrajectoryAggregationCaseById(Guid id, [FromQuery] bool includeResults = false)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            var val = _manager.GetTrajectoryAggregationCaseById(id, includeResults);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}", Name = "GetTrajectoryAggregationByCaseAndTrajectoryId")]
        public ActionResult<Model.TrajectoryAggregation?> GetTrajectoryAggregationByCaseAndTrajectoryId(Guid caseId, Guid trajectoryId, [FromQuery] bool includeResults = false)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty)
            {
                return BadRequest();
            }

            var val = _manager.GetTrajectoryAggregation(caseId, trajectoryId, includeResults);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/AggregatedSurveyPoints/ChunkCount", Name = "GetTrajectoryAggregationAggregatedSurveyPointChunkCount")]
        public ActionResult<int> GetAggregatedSurveyPointChunkCount(Guid caseId, Guid trajectoryId)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetAggregatedSurveyPointChunkCount(caseId, trajectoryId));
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/AggregatedSurveyPoints/Chunks/{chunkIndex}", Name = "GetTrajectoryAggregationAggregatedSurveyPointChunk")]
        public ActionResult<Model.SurveyPointChunk?> GetAggregatedSurveyPointChunk(Guid caseId, Guid trajectoryId, int chunkIndex)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            var val = _manager.GetAggregatedSurveyPointChunk(caseId, trajectoryId, chunkIndex);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/CoarsenedReferencePoints/ChunkCount", Name = "GetTrajectoryAggregationCoarsenedReferencePointChunkCount")]
        public ActionResult<int> GetCoarsenedReferencePointChunkCount(Guid caseId, Guid trajectoryId)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetCoarsenedReferencePointChunkCount(caseId, trajectoryId));
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/CoarsenedReferencePoints/Chunks/{chunkIndex}", Name = "GetTrajectoryAggregationCoarsenedReferencePointChunk")]
        public ActionResult<Model.SurveyPointChunk?> GetCoarsenedReferencePointChunk(Guid caseId, Guid trajectoryId, int chunkIndex)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            var val = _manager.GetCoarsenedReferencePointChunk(caseId, trajectoryId, chunkIndex);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/DistanceResults/ChunkCount", Name = "GetTrajectoryAggregationDistanceResultChunkCount")]
        public ActionResult<int> GetDistanceResultChunkCount(Guid caseId, Guid trajectoryId)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetDistanceResultChunkCount(caseId, trajectoryId));
        }

        [HttpGet("{caseId}/Trajectories/{trajectoryId}/DistanceResults/Chunks/{chunkIndex}", Name = "GetTrajectoryAggregationDistanceResultChunk")]
        public ActionResult<Model.TrajectoryAggregationDistanceResultChunk?> GetDistanceResultChunk(Guid caseId, Guid trajectoryId, int chunkIndex)
        {
            if (caseId == Guid.Empty || trajectoryId == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            var val = _manager.GetDistanceResultChunk(caseId, trajectoryId, chunkIndex);
            return val != null ? Ok(val) : NotFound();
        }

        [HttpGet("LightData", Name = "GetAllTrajectoryAggregationCaseLight")]
        public ActionResult<IEnumerable<Model.TrajectoryAggregationCaseLight>> GetAllTrajectoryAggregationCaseLight()
        {
            var vals = _manager.GetAllTrajectoryAggregationCaseLight();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllTrajectoryAggregationCase")]
        public ActionResult<IEnumerable<Model.TrajectoryAggregationCase?>> GetAllTrajectoryAggregationCase()
        {
            var vals = _manager.GetAllTrajectoryAggregationCase();
            return vals != null ? Ok(vals) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostTrajectoryAggregationCase")]
        public async Task<ActionResult> PostTrajectoryAggregationCase([FromBody] Model.TrajectoryAggregationCase? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The given TrajectoryAggregationCase is null, badly formed, or its ID is empty");
                return BadRequest();
            }

            if (_manager.GetTrajectoryAggregationCaseById(data.MetaInfo.ID, includeResults: false) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return await _manager.AddTrajectoryAggregationCase(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutTrajectoryAggregationCaseById")]
        public async Task<ActionResult> PutTrajectoryAggregationCaseById(Guid id, [FromBody] Model.TrajectoryAggregationCase? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetTrajectoryAggregationCaseById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return await _manager.UpdateTrajectoryAggregationCaseById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryAggregationCaseById")]
        public ActionResult DeleteTrajectoryAggregationCaseById(Guid id)
        {
            if (_manager.GetTrajectoryAggregationCaseById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return _manager.DeleteTrajectoryAggregationCaseById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
