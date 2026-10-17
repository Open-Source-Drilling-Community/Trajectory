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
    public class TrajectoryMinimumDistanceCalculationController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover TrajectoryMinimumDistanceCalculation resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverTrajectoryMinimumDistanceCalculation")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.TrajectoryMinimumDistanceCalculationDiscoveryPage> DiscoverTrajectoryMinimumDistanceCalculation([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null, [FromQuery] System.Guid? referenceTrajectoryId = null, [FromQuery] System.Guid? globalMinimumComparisonTrajectoryId = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200 || referenceTrajectoryId == System.Guid.Empty || globalMinimumComparisonTrajectoryId == System.Guid.Empty)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.TrajectoryMinimumDistanceCalculationLight>(_discoveryConnections.GetConnection(), "TrajectoryMinimumDistanceCalculationTable", "TrajectoryMinimumDistanceCalculation", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> { ["ReferenceTrajectoryID"] = referenceTrajectoryId, ["GlobalMinimumComparisonTrajectoryID"] = globalMinimumComparisonTrajectoryId }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.TrajectoryMinimumDistanceCalculationDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<TrajectoryMinimumDistanceCalculationManager> _logger;
        private readonly TrajectoryMinimumDistanceCalculationManager _manager;

        public TrajectoryMinimumDistanceCalculationController(ILogger<TrajectoryMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
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

        [NonAction]
        public ActionResult<IEnumerable<Model.TrajectoryMinimumDistanceCalculationLight>> GetAllTrajectoryMinimumDistanceCalculationLight()
        {
            List<Model.TrajectoryMinimumDistanceCalculationLight>? values = _manager.GetAllTrajectoryMinimumDistanceCalculationLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
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
        public async Task<ActionResult> PutTrajectoryMinimumDistanceCalculationById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.TrajectoryMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            Model.TrajectoryMinimumDistanceCalculation? current = _manager.GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return await _manager.UpdateTrajectoryMinimumDistanceCalculationById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryMinimumDistanceCalculationById")]
        public ActionResult DeleteTrajectoryMinimumDistanceCalculationById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            Model.TrajectoryMinimumDistanceCalculation? current = _manager.GetTrajectoryMinimumDistanceCalculationById(id, includeResults: false);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return _manager.DeleteTrajectoryMinimumDistanceCalculationById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
