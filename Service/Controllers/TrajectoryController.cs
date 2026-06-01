using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using NORCE.Drilling.Trajectory.Model;
using NORCE.Drilling.Trajectory.Service.Managers;
using System.Threading.Tasks;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryController : ControllerBase
    {
        private readonly ILogger<TrajectoryManager> _logger;
        private readonly TrajectoryManager _trajectoryManager;

        public TrajectoryController(ILogger<TrajectoryManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _trajectoryManager = TrajectoryManager.GetInstance(_logger, connectionManager);
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
        public async Task<ActionResult> PutTrajectoryById(Guid id, [FromBody] Model.Trajectory? data)
        {
            UsageStatisticsTrajectory.Instance.IncrementPutTrajectoryByIdPerDay();
            // Check if Trajectory is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _trajectoryManager.GetTrajectoryById(id);
                if (existingData != null)
                {
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
        public ActionResult DeleteTrajectoryById(Guid id)
        {
            UsageStatisticsTrajectory.Instance.IncrementDeleteTrajectoryByIdPerDay();
            if (_trajectoryManager.GetTrajectoryById(id) != null)
            {
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
