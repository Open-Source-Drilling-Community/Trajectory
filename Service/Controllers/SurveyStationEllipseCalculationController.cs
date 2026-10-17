using OSDC.Drilling.Trajectory.Service;
using OSDC.Drilling.Trajectory.Model;
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
    public class SurveyStationEllipseCalculationController : ControllerBase
    {
        /// <summary>Returns one bounded page of light records, retaining single-record and result reads.</summary>
        [HttpGet("LightData", Name = "DiscoverSurveyStationEllipseCalculation")]
        public ActionResult<SurveyStationEllipseCalculationDiscoveryPage> DiscoverSurveyStationEllipseCalculation([FromQuery] int offset = 0, [FromQuery] int limit = 50, [FromQuery] string? name = null)
        {
            OSDC.Drilling.Trajectory.Model.UsageStatisticsTrajectory.Instance.IncrementDiscoveryRequestsPerDay();
            if (offset < 0 || limit is < 1 or > 200 || name?.Length > 200) return BadRequest(new { error = "invalid_discovery_query" });
            List<SurveyStationEllipseCalculationLight> records;
            int total;
            try { records = DiscoveryRecords.ReadPage<SurveyStationEllipseCalculationLight>(_discoveryConnections.GetConnection(), "SurveyStationEllipseCalculationTable", "SurveyStationEllipseCalculation", offset, limit, name, new Dictionary<string, object?> {  }, out total); }
            catch (Exception ex) when (ex is Microsoft.Data.Sqlite.SqliteException or System.Text.Json.JsonException or InvalidOperationException)
            { return StatusCode(500, new { error = "discovery_failed" }); }
            return Ok(new SurveyStationEllipseCalculationDiscoveryPage { Items = records, Offset = offset, Limit = limit, TotalCount = total,
                NextOffset = offset < total && records.Count < total - offset ? offset + records.Count : null });
        }

        private readonly SqlConnectionManager _discoveryConnections;

        private readonly ILogger<SurveyStationEllipseCalculationManager> _logger;
        private readonly SurveyStationEllipseCalculationManager _manager;

        public SurveyStationEllipseCalculationController(ILogger<SurveyStationEllipseCalculationManager> logger, SqlConnectionManager connectionManager)
        {
            _discoveryConnections = connectionManager;
            _logger = logger;
            _manager = SurveyStationEllipseCalculationManager.GetInstance(logger, connectionManager);
        }

        [HttpGet(Name = "GetAllSurveyStationEllipseCalculationId")]
        public ActionResult<IEnumerable<Guid>> GetAllSurveyStationEllipseCalculationId()
        {
            List<Guid>? ids = _manager.GetAllSurveyStationEllipseCalculationId();
            return ids != null ? Ok(ids) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("MetaInfo", Name = "GetAllSurveyStationEllipseCalculationMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllSurveyStationEllipseCalculationMetaInfo()
        {
            List<MetaInfo?>? metaInfos = _manager.GetAllSurveyStationEllipseCalculationMetaInfo();
            return metaInfos != null ? Ok(metaInfos) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("{id}", Name = "GetSurveyStationEllipseCalculationById")]
        public ActionResult<SurveyStationEllipseCalculation?> GetSurveyStationEllipseCalculationById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            SurveyStationEllipseCalculation? calculation = _manager.GetSurveyStationEllipseCalculationById(id);
            return calculation != null ? Ok(calculation) : NotFound();
        }

        [HttpPost(Name = "PostSurveyStationEllipseCalculation")]
        public async Task<ActionResult<SurveyStationEllipseCalculation>> PostSurveyStationEllipseCalculation([FromBody] SurveyStationEllipseCalculation? data)
        {
            SurveyStationEllipseCalculation? calculation = await _manager.AddSurveyStationEllipseCalculationAsync(data);
            return ToActionResult(calculation, data);
        }

        [HttpPost("SurveyRun/{surveyRunId}", Name = "PostSurveyRunSurveyStationEllipseCalculation")]
        public async Task<ActionResult<SurveyStationEllipseCalculation>> PostSurveyRunSurveyStationEllipseCalculation(
            Guid surveyRunId,
            [FromBody] SurveyStationEllipseCalculation? data)
        {
            if (surveyRunId == Guid.Empty)
            {
                return BadRequest(new { error = "invalid_source", message = "A source SurveyRun UUID is required." });
            }

            SurveyStationEllipseCalculation? calculation =
                await _manager.AddSurveyStationEllipseCalculationAsync(data, surveyRunId, null);
            return ToActionResult(calculation, data);
        }

        [HttpPost("Trajectory/{trajectoryId}", Name = "PostTrajectorySurveyStationEllipseCalculation")]
        public async Task<ActionResult<SurveyStationEllipseCalculation>> PostTrajectorySurveyStationEllipseCalculation(
            Guid trajectoryId,
            [FromBody] SurveyStationEllipseCalculation? data)
        {
            if (trajectoryId == Guid.Empty)
            {
                return BadRequest(new { error = "invalid_source", message = "A source Trajectory UUID is required." });
            }

            SurveyStationEllipseCalculation? calculation =
                await _manager.AddSurveyStationEllipseCalculationAsync(data, null, trajectoryId);
            return ToActionResult(calculation, data);
        }

        private ActionResult<SurveyStationEllipseCalculation> ToActionResult(
            SurveyStationEllipseCalculation? calculation,
            SurveyStationEllipseCalculation? request)
        {
            return calculation != null
                ? Ok(calculation)
                : BadRequest(new
                {
                    error = "ellipse_calculation_failed",
                    message = request?.CalculationMessage ?? "The uncertainty ellipse calculation could not be completed."
                });
        }

        [HttpDelete("{id}", Name = "DeleteSurveyStationEllipseCalculationById")]
        public ActionResult DeleteSurveyStationEllipseCalculationById(Guid id,
            [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            Model.SurveyStationEllipseCalculation? current = _manager.GetSurveyStationEllipseCalculationById(id);
            if (current == null)
            {
                _logger.LogWarning("The SurveyStationEllipseCalculation of given ID does not exist");
                return NotFound();
            }
            if (current.LastModificationDate != expectedModifiedUtc)
                return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });

            return _manager.DeleteSurveyStationEllipseCalculationById(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
