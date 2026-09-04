using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service.Managers;
using System.Threading.Tasks;

namespace OSDC.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryController : ControllerBase
    {
        private readonly ILogger<TrajectoryManager> _logger;
        private readonly TrajectoryManager _trajectoryManager;
        private readonly TrajectoryAssignmentValidator _assignmentValidator;
        private readonly TrajectoryBatchService _batchService;

        public TrajectoryController(ILogger<TrajectoryManager> logger, SqlConnectionManager connectionManager, OctreeManager octreeManager,
            TrajectoryAssignmentValidator assignmentValidator, TrajectoryBatchService batchService)
        {
            _logger = logger;
            _trajectoryManager = TrajectoryManager.GetInstance(_logger, connectionManager, octreeManager);
            _assignmentValidator = assignmentValidator;
            _batchService = batchService;
        }

        /// <summary>
        /// Exports all survey runs and trajectories, or an explicit selection. The server automatically
        /// includes parent survey runs and every survey run referenced by a selected trajectory.
        /// </summary>
        [HttpPost("BatchExport", Name = "BatchExportTrajectoryData")]
        [ProducesResponseType<TrajectoryBatchExportDocument>(StatusCodes.Status200OK)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<TrajectoryBatchExportDocument> BatchExport([FromBody] TrajectoryBatchExportRequest? request)
        {
            TrajectoryBatchExportOutcome outcome = _batchService.Export(request);
            if (outcome.IsSuccess) return Ok(outcome.Document);
            return outcome.FailureKind switch
            {
                TrajectoryBatchFailureKind.InvalidRequest => BadRequest(outcome.Error),
                TrajectoryBatchFailureKind.NotFound => NotFound(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>
        /// Validates and restores a complete dependency-closed backup. Survey runs are restored before
        /// trajectories, and record changes are committed atomically without triggering recalculation.
        /// </summary>
        [HttpPost("BatchRestore", Name = "BatchRestoreTrajectoryData")]
        [ProducesResponseType<TrajectoryBatchRestoreResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<TrajectoryBatchErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<TrajectoryBatchRestoreResponse> BatchRestore([FromBody] TrajectoryBatchRestoreRequest? request)
        {
            TrajectoryBatchRestoreOutcome outcome = _batchService.Restore(request);
            if (outcome.IsSuccess) return Ok(outcome.Response);
            return outcome.FailureKind switch
            {
                TrajectoryBatchFailureKind.InvalidRequest => BadRequest(outcome.Error),
                TrajectoryBatchFailureKind.Conflict => Conflict(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        /// <summary>
        /// Returns the list of Guid of all Trajectory present in the microservice database at endpoint Trajectory/api/Trajectory
        /// </summary>
        /// <returns>the list of Guid of all Trajectory present in the microservice database at endpoint Trajectory/api/Trajectory</returns>
        [HttpGet(Name = "GetAllTrajectoryId")]
        public ActionResult<IEnumerable<Guid>> GetAllTrajectoryId()
        {
            UsageStatisticsTrajectory.Instance.IncrementGetAllTrajectoryIdPerDay();
            var ids = _trajectoryManager.GetAllTrajectoryId();
            if (ids != null)
            {
                return Ok(ids);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Trajectory present in the microservice database, at endpoint Trajectory/api/Trajectory/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all Trajectory present in the microservice database, at endpoint Trajectory/api/Trajectory/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllTrajectoryMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllTrajectoryMetaInfo()
        {
            UsageStatisticsTrajectory.Instance.IncrementGetAllTrajectoryMetaInfoPerDay();
            var vals = _trajectoryManager.GetAllTrajectoryMetaInfo();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the Trajectory identified by its Guid from the microservice database, at endpoint Trajectory/api/Trajectory/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Trajectory identified by its Guid from the microservice database, at endpoint Trajectory/api/Trajectory/id</returns>
        [HttpGet("{id}", Name = "GetTrajectoryById")]
        public ActionResult<Model.Trajectory?> GetTrajectoryById(Guid id, [FromQuery] bool includeCalculatedStations = false)
        {
            UsageStatisticsTrajectory.Instance.IncrementGetTrajectoryByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _trajectoryManager.GetTrajectoryById(id, includeCalculatedStations);
                if (val != null)
                {
                    return Ok(val);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest();
            }
        }

        [HttpGet("{id}/SurveyStations/ChunkCount", Name = "GetTrajectorySurveyStationChunkCount")]
        public ActionResult<int> GetSurveyStationChunkCount(Guid id)
        {
            if (id == Guid.Empty)
            {
                return BadRequest();
            }

            return Ok(_trajectoryManager.GetSurveyStationChunkCount(id));
        }

        [HttpGet("{id}/SurveyStations/Chunks/{chunkIndex}", Name = "GetTrajectorySurveyStationChunk")]
        public ActionResult<SurveyStationChunk?> GetSurveyStationChunk(Guid id, int chunkIndex)
        {
            if (id == Guid.Empty || chunkIndex < 0)
            {
                return BadRequest();
            }

            SurveyStationChunk? value = _trajectoryManager.GetSurveyStationChunk(id, chunkIndex);
            return value != null ? Ok(value) : NotFound();
        }

        /// <summary>
        /// Returns the list of all TrajectoryLight present in the microservice database, at endpoint Trajectory/api/Trajectory/LightData
        /// </summary>
        /// <returns>the list of all TrajectoryLight present in the microservice database, at endpoint Trajectory/api/Trajectory/LightData</returns>
        [HttpGet("LightData", Name = "GetAllTrajectoryLight")]
        public ActionResult<IEnumerable<Model.TrajectoryLight>> GetAllTrajectoryLight([FromQuery] Guid? fieldId = null, [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null, [FromQuery] Guid? wellBoreId = null, [FromQuery] TrajectoryType? trajectoryType = null, [FromQuery] bool? isDefinitive = null)
        {
            UsageStatisticsTrajectory.Instance.IncrementGetAllTrajectoryLightPerDay();
            var vals = _trajectoryManager.GetAllTrajectoryLight(fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>Returns one deterministic bounded page of lightweight trajectories.</summary>
        [HttpGet("Search", Name = "SearchTrajectory")]
        public ActionResult<TrajectorySearchResult> SearchTrajectory(
            [FromQuery] string? query = null, [FromQuery] Guid? fieldId = null,
            [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null,
            [FromQuery] Guid? wellBoreId = null, [FromQuery] TrajectoryType? trajectoryType = null,
            [FromQuery] bool? isDefinitive = null, [FromQuery] int offset = 0, [FromQuery] int limit = 100)
        {
            if (offset < 0 || limit is < 1 or > 500)
                return BadRequest(new { error = "invalid_page", message = "offset must be non-negative and limit must be between 1 and 500." });
            List<Model.TrajectoryLight>? values = _trajectoryManager.GetAllTrajectoryLight(
                fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
            if (values == null) return StatusCode(StatusCodes.Status500InternalServerError);
            IEnumerable<Model.TrajectoryLight> matches = values.Where(value => string.IsNullOrWhiteSpace(query) ||
                $"{value.Name} {value.Description} {value.MetaInfo?.ID}".Contains(query, StringComparison.OrdinalIgnoreCase));
            List<Model.TrajectoryLight> ordered = matches
                .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value.MetaInfo?.ID)
                .ToList();
            return Ok(new TrajectorySearchResult
            {
                Offset = offset, Limit = limit, TotalCount = ordered.Count,
                Items = ordered.Skip(offset).Take(limit).ToList()
            });
        }

        /// <summary>
        /// Returns the list of all Trajectory present in the microservice database, at endpoint Trajectory/api/Trajectory/HeavyData
        /// </summary>
        /// <returns>the list of all Trajectory present in the microservice database, at endpoint Trajectory/api/Trajectory/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllTrajectory")]
        public ActionResult<IEnumerable<Model.Trajectory?>> GetAllTrajectory([FromQuery] Guid? fieldId = null, [FromQuery] Guid? clusterId = null, [FromQuery] Guid? wellId = null, [FromQuery] Guid? wellBoreId = null, [FromQuery] TrajectoryType? trajectoryType = null, [FromQuery] bool? isDefinitive = null)
        {
            UsageStatisticsTrajectory.Instance.IncrementGetAllTrajectoryPerDay();
            var vals = _trajectoryManager.GetAllTrajectory(fieldId, clusterId, wellId, wellBoreId, trajectoryType, isDefinitive);
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and adds it to the microservice database, at the endpoint Trajectory/api/Trajectory
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been added successfully to the microservice database, at the endpoint Trajectory/api/Trajectory</returns>
        [HttpPost(Name = "PostTrajectory")]
        public async Task<ActionResult> PostTrajectory([FromBody] Model.Trajectory? data)
        {
            UsageStatisticsTrajectory.Instance.IncrementPostTrajectoryPerDay();
            if (data == null || !_assignmentValidator.Validate(data))
            {
                return BadRequest(new { error = "invalid_identity_or_feature_assignment" });
            }
            // Check if trajectory exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                var existingData = _trajectoryManager.GetTrajectoryById(data.MetaInfo.ID);
                if (existingData == null)
                {   
                    //  If trajectory was not found, call AddTrajectory, where the trajectory.Calculate()
                    // method is called. 
                    if (await _trajectoryManager.AddTrajectory(data))
                    {
                        return Ok(); // status=OK is used rather than status=Created because NSwag auto-generated controllers use 200 (OK) rather than 201 (Created) as return codes
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Trajectory already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given Trajectory is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given Trajectory and updates it in the microservice database, at the endpoint Trajectory/api/Trajectory/id
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns>true if the given Trajectory has been updated successfully to the microservice database, at the endpoint Trajectory/api/Trajectory/id</returns>
        [HttpPut("{id}", Name = "PutTrajectoryById")]
        public async Task<ActionResult> PutTrajectoryById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc,
            [FromBody] Model.Trajectory? data)
        {
            UsageStatisticsTrajectory.Instance.IncrementPutTrajectoryByIdPerDay();
            if (data == null || !_assignmentValidator.Validate(data))
            {
                return BadRequest(new { error = "invalid_identity_or_feature_assignment" });
            }
            // Check if Trajectory is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _trajectoryManager.GetTrajectoryById(id);
                if (existingData != null)
                {
                    if (existingData.LastModificationDate != expectedModifiedUtc)
                        return Conflict(new { error = "stale_write", currentModifiedUtc = existingData.LastModificationDate });
                    if (await _trajectoryManager.UpdateTrajectoryById(id, data))
                    {
                        return Ok();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Trajectory has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given Trajectory is null, badly formed, or its does not match the ID to update");
                return BadRequest();
            }
        }

        /// <summary>
        /// Deletes the Trajectory of given ID from the microservice database, at the endpoint Trajectory/api/Trajectory/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Trajectory was deleted from the microservice database, at the endpoint Trajectory/api/Trajectory/id</returns>
        [HttpDelete("{id}", Name = "DeleteTrajectoryById")]
        public ActionResult DeleteTrajectoryById(Guid id, [FromQuery, Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired] DateTimeOffset expectedModifiedUtc)
        {
            UsageStatisticsTrajectory.Instance.IncrementDeleteTrajectoryByIdPerDay();
            Model.Trajectory? current = _trajectoryManager.GetTrajectoryById(id);
            if (current != null)
            {
                if (current.LastModificationDate != expectedModifiedUtc)
                    return Conflict(new { error = "stale_write", currentModifiedUtc = current.LastModificationDate });
                if (_trajectoryManager.DeleteTrajectoryById(id))
                {
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                _logger.LogWarning("The Trajectory of given ID does not exist");
                return NotFound();
            }
        }
    }
}
