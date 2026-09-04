using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
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
    public class SurveyRunController : ControllerBase
    {
        private readonly ILogger<SurveyRunManager> _logger;
        private readonly SurveyRunManager _manager;
        private readonly TrajectoryAssignmentValidator _assignmentValidator;

        public SurveyRunController(ILogger<SurveyRunManager> logger, SqlConnectionManager connectionManager, TrajectoryAssignmentValidator assignmentValidator)
        {
            _logger = logger;
            _manager = SurveyRunManager.GetInstance(logger, connectionManager);
            _assignmentValidator = assignmentValidator;
        }

        [HttpGet(Name = "GetAllSurveyRunId")]
        public ActionResult<IEnumerable<Guid>> GetAllSurveyRunId()
        {
            List<Guid>? values = _manager.GetAllSurveyRunId();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllSurveyRunMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllSurveyRunMetaInfo()
        {
            List<MetaInfo?>? values = _manager.GetAllSurveyRunMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("LightData", Name = "GetAllSurveyRunLight")]
        public ActionResult<IEnumerable<SurveyRunLight>> GetAllSurveyRunLight([FromQuery] Guid? fieldId = null, [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null, [FromQuery] Guid? wellBoreId = null, [FromQuery] Guid? surveyInstrumentId = null, [FromQuery] SurveyRunType? surveyRunType = null)
        {
            List<SurveyRunLight>? values = _manager.GetAllSurveyRunLight(fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType);
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        /// <summary>Returns one deterministic bounded page of lightweight survey runs.</summary>
        [HttpGet("Search", Name = "SearchSurveyRun")]
        public ActionResult<SurveyRunSearchResult> SearchSurveyRun(
            [FromQuery] string? query = null, [FromQuery] Guid? fieldId = null,
            [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null,
            [FromQuery] Guid? wellBoreId = null, [FromQuery] Guid? surveyInstrumentId = null,
            [FromQuery] SurveyRunType? surveyRunType = null, [FromQuery] int offset = 0,
            [FromQuery] int limit = 100)
        {
            if (offset < 0 || limit is < 1 or > 500)
                return BadRequest(new { error = "invalid_page", message = "offset must be non-negative and limit must be between 1 and 500." });
            List<SurveyRunLight>? values = _manager.GetAllSurveyRunLight(
                fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType);
            if (values == null) return StatusCode(StatusCodes.Status500InternalServerError);
            IEnumerable<SurveyRunLight> matches = values.Where(value => string.IsNullOrWhiteSpace(query) ||
                $"{value.Name} {value.Description} {value.MetaInfo?.ID}".Contains(query, StringComparison.OrdinalIgnoreCase));
            List<SurveyRunLight> ordered = matches
                .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.MetaInfo?.ID)
                .ToList();
            return Ok(new SurveyRunSearchResult
            {
                Offset = offset, Limit = limit, TotalCount = ordered.Count,
                Items = ordered.Skip(offset).Take(limit).ToList()
            });
        }

        [HttpGet("HeavyData", Name = "GetAllSurveyRun")]
        public ActionResult<IEnumerable<SurveyRun>> GetAllSurveyRun([FromQuery] Guid? fieldId = null, [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null, [FromQuery] Guid? wellBoreId = null, [FromQuery] Guid? surveyInstrumentId = null, [FromQuery] SurveyRunType? surveyRunType = null)
        {
            List<SurveyRun>? values = _manager.GetAllSurveyRun(fieldId, clusterId, wellId, wellBoreId, surveyInstrumentId, surveyRunType);
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetSurveyRunById")]
        public ActionResult<SurveyRun?> GetSurveyRunById(Guid id, [FromQuery] bool includeMeasurements = false, [FromQuery] bool includeCalculatedStations = false)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            SurveyRun? value = _manager.GetSurveyRunById(id, includeMeasurements, includeCalculatedStations);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("{id}/SurveyStations/ChunkCount", Name = "GetSurveyRunSurveyStationChunkCount")]
        public ActionResult<int> GetSurveyStationChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetSurveyStationChunkCount(id));
        }

        [HttpGet("{id}/SurveyStations/Chunks/{chunkIndex}", Name = "GetSurveyRunSurveyStationChunk")]
        public ActionResult<SurveyStationChunk?> GetSurveyStationChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            SurveyStationChunk? value = _manager.GetSurveyStationChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpGet("{id}/SurveyMeasurements/ChunkCount", Name = "GetSurveyRunSurveyMeasurementChunkCount")]
        public ActionResult<int> GetSurveyMeasurementChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_manager.GetSurveyMeasurementChunkCount(id));
        }

        [HttpGet("{id}/SurveyMeasurements/Chunks/{chunkIndex}", Name = "GetSurveyRunSurveyMeasurementChunk")]
        public ActionResult<SurveyMeasurementChunk?> GetSurveyMeasurementChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            SurveyMeasurementChunk? value = _manager.GetSurveyMeasurementChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpPut("{id}/SurveyMeasurements/Chunks/{chunkIndex}", Name = "PutSurveyRunSurveyMeasurementChunk")]
        public ActionResult PutSurveyMeasurementChunk(Guid id, int chunkIndex, [FromBody] SurveyMeasurementChunk? chunk)
        {
            if (id == Guid.Empty || chunkIndex < 0 || chunk?.SurveyRunID != id || chunk.ChunkIndex != chunkIndex)
            {
                return BadRequest();
            }

            return _manager.PutSurveyMeasurementChunk(id, chunkIndex, chunk) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}/SurveyMeasurements/Chunks", Name = "DeleteSurveyRunSurveyMeasurementChunks")]
        public ActionResult DeleteSurveyMeasurementChunks(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return _manager.DeleteSurveyMeasurementChunks(id) ? Ok() : NotFound();
        }

        [HttpPost("{id}/SurveyMeasurements/Commit", Name = "CommitSurveyRunSurveyMeasurementChunks")]
        public async Task<ActionResult> CommitSurveyMeasurementChunks(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return await _manager.CommitSurveyMeasurementChunks(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpPost(Name = "PostSurveyRun")]
        public async Task<ActionResult> PostSurveyRun([FromBody] SurveyRun? data)
        {
            if (data == null || !_assignmentValidator.Validate(data))
            {
                return BadRequest(new { error = "invalid_identity_or_feature_assignment" });
            }
            if (data?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            {
                if (_manager.GetSurveyRunById(id) == null)
                {
                    return await _manager.AddSurveyRun(data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }

                _logger.LogWarning("The given SurveyRun already exists and will not be added");
                return StatusCode(StatusCodes.Status409Conflict);
            }

            _logger.LogWarning("The given SurveyRun is null, badly formed, or its ID is empty");
            return BadRequest();
        }

        [HttpPut("{id}", Name = "PutSurveyRunById")]
        public async Task<ActionResult> PutSurveyRunById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] SurveyRun? data)
        {
            if (data == null || !_assignmentValidator.Validate(data))
            {
                return BadRequest(new { error = "invalid_identity_or_feature_assignment" });
            }
            if (data?.MetaInfo?.ID == id)
            {
                SurveyRun? current = _manager.GetSurveyRunById(id);
                if (current != null)
                {
                    if (current.LastModificationDate != expectedModifiedUtc)
                        return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
                    return await _manager.UpdateSurveyRunById(id, data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }

                _logger.LogWarning("The given SurveyRun has not been found in the database");
                return NotFound();
            }

            _logger.LogWarning("The given SurveyRun is null, badly formed, or does not match the ID to update");
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteSurveyRunById")]
        public ActionResult DeleteSurveyRunById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            SurveyRun? current = _manager.GetSurveyRunById(id);
            if (current != null)
            {
                if (current.LastModificationDate != expectedModifiedUtc)
                    return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
                return _manager.DeleteSurveyRunById(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogWarning("The SurveyRun of given ID does not exist");
            return NotFound();
        }
    }
}
