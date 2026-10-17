using OSDC.Drilling.Trajectory.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class SurveyRunBatchImportController : ControllerBase
    {
        private readonly SqlConnectionManager _discoveryConnections;

        /// <summary>Discover SurveyRunBatchImport resources as bounded light records; retrieve selected full records by ID.</summary>
        [HttpGet("LightData", Name = "DiscoverSurveyRunBatchImport")]
        public ActionResult<OSDC.Drilling.Trajectory.Model.SurveyRunBatchImportDiscoveryPage> DiscoverSurveyRunBatchImport([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null, [FromQuery] System.Guid? selectedFieldId = null, [FromQuery] System.Guid? selectedClusterId = null, [FromQuery] System.Guid? selectedWellId = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit < 1 || limit > 200 || name?.Length > 200 || selectedFieldId == System.Guid.Empty || selectedClusterId == System.Guid.Empty || selectedWellId == System.Guid.Empty)
                return BadRequest(new { error = "invalid_discovery_query", message = "Offset must be non-negative, limit 1–200, name at most 200 characters, and UUID filters non-empty." });
            try
            {
                var records = DiscoveryRecords.ReadPage<OSDC.Drilling.Trajectory.Model.SurveyRunBatchImportLight>(_discoveryConnections.GetConnection(), "SurveyRunBatchImportTable", "SurveyRunBatchImport", offset, limit, name,
                    new System.Collections.Generic.Dictionary<string, object?> { ["SelectedFieldId"] = selectedFieldId, ["SelectedClusterId"] = selectedClusterId, ["SelectedWellId"] = selectedWellId }, out int total);
                return Ok(new OSDC.Drilling.Trajectory.Model.SurveyRunBatchImportDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                    NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
            }
            catch (System.Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or System.InvalidOperationException)
            {
                return StatusCode(500, new { error = "discovery_failed" });
            }
        }

        private readonly ILogger<SurveyRunBatchImportManager> _logger;
        private readonly SurveyRunBatchImportManager _manager;

        public SurveyRunBatchImportController(ILogger<SurveyRunBatchImportManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _manager = SurveyRunBatchImportManager.GetInstance(logger, connectionManager);
        }

        [HttpGet(Name = "GetAllSurveyRunBatchImportId")]
        public ActionResult<IEnumerable<Guid>> GetAllSurveyRunBatchImportId()
        {
            List<Guid>? values = _manager.GetAllSurveyRunBatchImportId();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllSurveyRunBatchImportMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllSurveyRunBatchImportMetaInfo()
        {
            List<MetaInfo?>? values = _manager.GetAllSurveyRunBatchImportMetaInfo();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
        public ActionResult<IEnumerable<SurveyRunBatchImportLight>> GetAllSurveyRunBatchImportLight()
        {
            List<SurveyRunBatchImportLight>? values = _manager.GetAllSurveyRunBatchImportLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [NonAction]
        public ActionResult<IEnumerable<SurveyRunBatchImport>> GetAllSurveyRunBatchImport()
        {
            List<SurveyRunBatchImport>? values = _manager.GetAllSurveyRunBatchImport();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetSurveyRunBatchImportById")]
        public ActionResult<SurveyRunBatchImport?> GetSurveyRunBatchImportById(Guid id)
        {
            if (id == Guid.Empty)
                return BadRequest();
            SurveyRunBatchImport? value = _manager.GetSurveyRunBatchImportById(id);
            return value != null ? Ok(value) : NotFound();
        }

        [HttpPost(Name = "PostSurveyRunBatchImport")]
        public ActionResult PostSurveyRunBatchImport([FromBody] SurveyRunBatchImport? data)
        {
            if (data?.MetaInfo?.ID is Guid id && id != Guid.Empty)
            {
                if (_manager.GetSurveyRunBatchImportById(id) == null)
                    return _manager.AddSurveyRunBatchImport(data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                _logger.LogWarning("The given SurveyRunBatchImport already exists and will not be added");
                return StatusCode(StatusCodes.Status409Conflict);
            }

            _logger.LogWarning("The given SurveyRunBatchImport is null, badly formed, or its ID is empty");
            return BadRequest();
        }

        [HttpPut("{id}", Name = "PutSurveyRunBatchImportById")]
        public ActionResult PutSurveyRunBatchImportById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] SurveyRunBatchImport? data)
        {
            if (data?.MetaInfo?.ID == id)
            {
                SurveyRunBatchImport? current = _manager.GetSurveyRunBatchImportById(id);
                if (current != null)
                {
                    if (current.LastModificationDate != expectedModifiedUtc)
                        return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
                    return _manager.UpdateSurveyRunBatchImportById(id, data) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
                }
                _logger.LogWarning("The given SurveyRunBatchImport has not been found in the database");
                return NotFound();
            }

            _logger.LogWarning("The given SurveyRunBatchImport is null, badly formed, or does not match the ID to update");
            return BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteSurveyRunBatchImportById")]
        public ActionResult DeleteSurveyRunBatchImportById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            SurveyRunBatchImport? current = _manager.GetSurveyRunBatchImportById(id);
            if (current != null)
            {
                if (current.LastModificationDate != expectedModifiedUtc)
                    return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
                return _manager.DeleteSurveyRunBatchImportById(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogWarning("The SurveyRunBatchImport of given ID does not exist");
            return NotFound();
        }
    }
}
