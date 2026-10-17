using OSDC.Drilling.Trajectory.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryRealizationCaseController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover TrajectoryRealizationCase resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverTrajectoryRealizationCase")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.TrajectoryRealizationCaseDiscoveryPage> DiscoverTrajectoryRealizationCase([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null, [FromQuery] System.Guid? trajectoryId = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200 || trajectoryId == System.Guid.Empty)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.TrajectoryRealizationCaseLight>(_discoveryConnections.GetConnection(), "TrajectoryRealizationCaseTable", "TrajectoryRealizationCase", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> { ["TrajectoryID"] = trajectoryId }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.TrajectoryRealizationCaseDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<TrajectoryRealizationCaseManager> _logger;
        private readonly TrajectoryRealizationCaseManager _manager;

        public TrajectoryRealizationCaseController(ILogger<TrajectoryRealizationCaseManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
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

        [NonAction]
        public ActionResult<IEnumerable<Model.TrajectoryRealizationCaseLight>> GetAllTrajectoryRealizationCaseLight()
        {
            List<Model.TrajectoryRealizationCaseLight>? values = _manager.GetAllTrajectoryRealizationCaseLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
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
        public async Task<ActionResult> PutTrajectoryRealizationCaseById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.TrajectoryRealizationCase? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            Model.TrajectoryRealizationCase? current = _manager.GetTrajectoryRealizationCaseById(id);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return await _manager.UpdateTrajectoryRealizationCaseById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryRealizationCaseById")]
        public ActionResult DeleteTrajectoryRealizationCaseById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            Model.TrajectoryRealizationCase? current = _manager.GetTrajectoryRealizationCaseById(id);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return _manager.DeleteTrajectoryRealizationCaseById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
