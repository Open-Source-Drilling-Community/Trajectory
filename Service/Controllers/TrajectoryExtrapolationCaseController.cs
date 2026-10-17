using OSDC.Drilling.Trajectory.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Controllers
{
    /// <summary>
    /// Stores and calculates trajectory extrapolation cases. Calculations are queued after a successful
    /// create or update; clients can poll LightData or Status and retrieve station chunks when completed.
    /// </summary>
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryExtrapolationCaseController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover TrajectoryExtrapolationCase resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverTrajectoryExtrapolationCase")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.TrajectoryExtrapolationCaseDiscoveryPage> DiscoverTrajectoryExtrapolationCase([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null, [FromQuery] System.Guid? sourceTrajectoryId = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200 || sourceTrajectoryId == System.Guid.Empty)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.TrajectoryExtrapolationCaseLight>(_discoveryConnections.GetConnection(), "TrajectoryExtrapolationCaseTable", "TrajectoryExtrapolationCase", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> { ["SourceTrajectoryID"] = sourceTrajectoryId }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.TrajectoryExtrapolationCaseDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<TrajectoryExtrapolationCaseManager> logger_;
        private readonly TrajectoryExtrapolationCaseManager manager_;

        public TrajectoryExtrapolationCaseController(
            ILogger<TrajectoryExtrapolationCaseManager> logger,
            SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            logger_ = logger;
            manager_ = TrajectoryExtrapolationCaseManager.GetInstance(logger, connectionManager);
        }

        [HttpGet(Name = "GetAllTrajectoryExtrapolationCaseId")]
        [ProducesResponseType<IEnumerable<Guid>>(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<Guid>> GetAllIds()
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetAllTrajectoryExtrapolationCaseId");
            List<Guid>? values = manager_.GetAllIds();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllTrajectoryExtrapolationCaseMetaInfo")]
        [ProducesResponseType<IEnumerable<MetaInfo>>(StatusCodes.Status200OK)]
        public ActionResult<IEnumerable<MetaInfo>> GetAllMetaInfo()
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetAllTrajectoryExtrapolationCaseMetaInfo");
            List<MetaInfo>? values = manager_.GetAllMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
        public ActionResult<IEnumerable<TrajectoryExtrapolationCaseLight>> GetAllLight()
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetAllTrajectoryExtrapolationCaseLight");
            List<TrajectoryExtrapolationCaseLight>? values = manager_.GetAllLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
        public ActionResult<IEnumerable<TrajectoryExtrapolationCase>> GetAll()
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetAllTrajectoryExtrapolationCase");
            List<TrajectoryExtrapolationCase>? values = manager_.GetAll();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetTrajectoryExtrapolationCaseById")]
        [ProducesResponseType<TrajectoryExtrapolationCase>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<TrajectoryExtrapolationCase> GetById(Guid id, [FromQuery] bool includeResults = false)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetTrajectoryExtrapolationCaseById");
            if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty extrapolation-case UUID is required."));
            TrajectoryExtrapolationCase? value = manager_.GetById(id, includeResults);
            return value != null ? Ok(value) : NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
        }

        [HttpGet("{id}/Status", Name = "GetTrajectoryExtrapolationCaseStatus")]
        [ProducesResponseType<TrajectoryExtrapolationCaseLight>(StatusCodes.Status200OK)]
        public ActionResult<TrajectoryExtrapolationCaseLight> GetStatus(Guid id)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetTrajectoryExtrapolationCaseStatus");
            if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty extrapolation-case UUID is required."));
            TrajectoryExtrapolationCaseLight? value = manager_.GetLightById(id);
            return value != null ? Ok(value) : NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
        }

        [HttpGet("{id}/SurveyStations/ChunkCount", Name = "GetTrajectoryExtrapolationSurveyStationChunkCount")]
        public ActionResult<int> GetSurveyStationChunkCount(Guid id)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetTrajectoryExtrapolationSurveyStationChunkCount");
            if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty extrapolation-case UUID is required."));
            if (manager_.GetById(id) == null) return NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
            return Ok(manager_.GetSurveyStationChunkCount(id));
        }

        [HttpGet("{id}/SurveyStations/Chunks/{chunkIndex}", Name = "GetTrajectoryExtrapolationSurveyStationChunk")]
        public ActionResult<SurveyStationChunk> GetSurveyStationChunk(Guid id, int chunkIndex)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("GetTrajectoryExtrapolationSurveyStationChunk");
            if (id == Guid.Empty || chunkIndex < 0)
                return BadRequest(Error("invalid_chunk", "A non-empty case UUID and non-negative chunk index are required."));
            if (manager_.GetById(id) == null) return NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
            SurveyStationChunk? value = manager_.GetSurveyStationChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound(Error("chunk_not_found", "The requested result chunk does not exist."));
        }

        [HttpPost(Name = "PostTrajectoryExtrapolationCase")]
        [ProducesResponseType<TrajectoryExtrapolationCase>(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TrajectoryExtrapolationCase>> Post([FromBody] TrajectoryExtrapolationCase? value)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("PostTrajectoryExtrapolationCase");
            List<string> errors = TrajectoryExtrapolationValidation.Validate(value);
            if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
            Guid id = value!.MetaInfo!.ID;
            if (manager_.GetById(id) != null)
                return Conflict(Error("already_exists", "A trajectory extrapolation case with this UUID already exists."));
            if (!await manager_.AddAsync(value))
                return StatusCode(StatusCodes.Status500InternalServerError, Error("persistence_failed", "The trajectory extrapolation case could not be saved."));
            return AcceptedAtAction(nameof(GetStatus), new { id }, value);
        }

        [HttpPut("{id}", Name = "PutTrajectoryExtrapolationCaseById")]
        [ProducesResponseType<TrajectoryExtrapolationCase>(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<TrajectoryExtrapolationCase>> Put(
            Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] TrajectoryExtrapolationCase? value)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("PutTrajectoryExtrapolationCaseById");
            if (id == Guid.Empty || value?.MetaInfo?.ID != id)
                return BadRequest(Error("id_mismatch", "The route and payload UUIDs must be the same non-empty value."));
            List<string> errors = TrajectoryExtrapolationValidation.Validate(value);
            if (errors.Count > 0) return BadRequest(new { error = "validation_failed", errors });
            TrajectoryExtrapolationCase? current = manager_.GetById(id);
            if (current == null) return NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
            if (!await manager_.UpdateAsync(id, expectedModifiedUtc, value))
            {
                TrajectoryExtrapolationCase? latest = manager_.GetById(id);
                if (latest != null && latest.LastModificationDate != expectedModifiedUtc)
                    return Conflict(new { error = "stale_write", currentModifiedUtc = latest.LastModificationDate });
                return StatusCode(StatusCodes.Status500InternalServerError, Error("persistence_failed", "The trajectory extrapolation case could not be saved."));
            }
            return AcceptedAtAction(nameof(GetStatus), new { id }, value);
        }

        [HttpDelete("{id}", Name = "DeleteTrajectoryExtrapolationCaseById")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult Delete(
            Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            UsageStatisticsTrajectory.Instance.IncrementTrajectoryExtrapolationOperation("DeleteTrajectoryExtrapolationCaseById");
            if (id == Guid.Empty) return BadRequest(Error("invalid_id", "A non-empty extrapolation-case UUID is required."));
            TrajectoryExtrapolationCase? current = manager_.GetById(id);
            if (current == null) return NotFound(Error("not_found", "The trajectory extrapolation case does not exist."));
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
            if (!manager_.Delete(id, expectedModifiedUtc))
            {
                TrajectoryExtrapolationCase? latest = manager_.GetById(id);
                if (latest != null && latest.LastModificationDate != expectedModifiedUtc)
                    return Conflict(new { error = "stale_write", currentModifiedUtc = latest.LastModificationDate });
                logger_.LogError("Unable to delete trajectory extrapolation case {CaseId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, Error("persistence_failed", "The trajectory extrapolation case could not be deleted."));
            }
            return NoContent();
        }

        private static object Error(string code, string message) => new { error = code, message };
    }
}
