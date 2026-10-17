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
    public class SurveyRunMinimumDistanceCalculationController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover SurveyRunMinimumDistanceCalculation resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverSurveyRunMinimumDistanceCalculation")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.SurveyRunMinimumDistanceCalculationDiscoveryPage> DiscoverSurveyRunMinimumDistanceCalculation([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null, [FromQuery] System.Guid? referenceSurveyRunId = null, [FromQuery] System.Guid? globalMinimumComparisonSurveyRunId = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200 || referenceSurveyRunId == System.Guid.Empty || globalMinimumComparisonSurveyRunId == System.Guid.Empty)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.SurveyRunMinimumDistanceCalculationLight>(_discoveryConnections.GetConnection(), "SurveyRunMinimumDistanceCalculationTable", "SurveyRunMinimumDistanceCalculation", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> { ["ReferenceSurveyRunID"] = referenceSurveyRunId, ["GlobalMinimumComparisonSurveyRunID"] = globalMinimumComparisonSurveyRunId }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.SurveyRunMinimumDistanceCalculationDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<SurveyRunMinimumDistanceCalculationManager> _logger;
        private readonly SurveyRunMinimumDistanceCalculationManager _manager;

        public SurveyRunMinimumDistanceCalculationController(ILogger<SurveyRunMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _manager = SurveyRunMinimumDistanceCalculationManager.GetInstance(_logger, connectionManager);
        }

        [HttpGet(Name = "GetAllSurveyRunMinimumDistanceCalculationId")]
        public ActionResult<IEnumerable<Guid>> GetAllSurveyRunMinimumDistanceCalculationId()
        {
            List<Guid>? ids = _manager.GetAllSurveyRunMinimumDistanceCalculationId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllSurveyRunMinimumDistanceCalculationMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllSurveyRunMinimumDistanceCalculationMetaInfo()
        {
            List<MetaInfo?>? values = _manager.GetAllSurveyRunMinimumDistanceCalculationMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetSurveyRunMinimumDistanceCalculationById")]
        public ActionResult<Model.SurveyRunMinimumDistanceCalculation?> GetSurveyRunMinimumDistanceCalculationById(Guid id, [FromQuery] bool includeResults = false)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            Model.SurveyRunMinimumDistanceCalculation? value = _manager.GetSurveyRunMinimumDistanceCalculationById(id, includeResults);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("{id}/Results/ChunkCount", Name = "GetSurveyRunMinimumDistanceCalculationResultChunkCount")]
        public ActionResult<int> GetResultChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetResultChunkCount(id));
        }

        [HttpGet("{id}/Results/Chunks/{chunkIndex}", Name = "GetSurveyRunMinimumDistanceCalculationResultChunk")]
        public ActionResult<Model.SurveyRunMinimumDistanceResultChunk?> GetResultChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            Model.SurveyRunMinimumDistanceResultChunk? value = _manager.GetResultChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.SurveyRunMinimumDistanceCalculationLight>> GetAllSurveyRunMinimumDistanceCalculationLight()
        {
            List<Model.SurveyRunMinimumDistanceCalculationLight>? values = _manager.GetAllSurveyRunMinimumDistanceCalculationLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
        public ActionResult<IEnumerable<Model.SurveyRunMinimumDistanceCalculation?>> GetAllSurveyRunMinimumDistanceCalculation()
        {
            List<Model.SurveyRunMinimumDistanceCalculation?>? values = _manager.GetAllSurveyRunMinimumDistanceCalculation();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostSurveyRunMinimumDistanceCalculation")]
        public async Task<ActionResult> PostSurveyRunMinimumDistanceCalculation([FromBody] Model.SurveyRunMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID == Guid.Empty)
            {
                _logger.LogWarning("The given SurveyRunMinimumDistanceCalculation is null, badly formed, or its ID is empty");
                return BadRequest();
            }

            if (_manager.GetSurveyRunMinimumDistanceCalculationById(data.MetaInfo.ID, includeResults: false) != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            return await _manager.AddSurveyRunMinimumDistanceCalculation(data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPut("{id}", Name = "PutSurveyRunMinimumDistanceCalculationById")]
        public async Task<ActionResult> PutSurveyRunMinimumDistanceCalculationById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.SurveyRunMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            Model.SurveyRunMinimumDistanceCalculation? current = _manager.GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return await _manager.UpdateSurveyRunMinimumDistanceCalculationById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteSurveyRunMinimumDistanceCalculationById")]
        public ActionResult DeleteSurveyRunMinimumDistanceCalculationById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            Model.SurveyRunMinimumDistanceCalculation? current = _manager.GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false);
            if (current == null)
            {
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return _manager.DeleteSurveyRunMinimumDistanceCalculationById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
