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
    public class SurveyRunMinimumDistanceCalculationController : ControllerBase
    {
        private readonly ILogger<SurveyRunMinimumDistanceCalculationManager> _logger;
        private readonly SurveyRunMinimumDistanceCalculationManager _manager;

        public SurveyRunMinimumDistanceCalculationController(ILogger<SurveyRunMinimumDistanceCalculationManager> logger, SqlConnectionManager connectionManager)
        {
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

        [HttpGet("LightData", Name = "GetAllSurveyRunMinimumDistanceCalculationLight")]
        public ActionResult<IEnumerable<Model.SurveyRunMinimumDistanceCalculationLight>> GetAllSurveyRunMinimumDistanceCalculationLight()
        {
            List<Model.SurveyRunMinimumDistanceCalculationLight>? values = _manager.GetAllSurveyRunMinimumDistanceCalculationLight();
            return values != null ? Ok(values) : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpGet("HeavyData", Name = "GetAllSurveyRunMinimumDistanceCalculation")]
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
        public async Task<ActionResult> PutSurveyRunMinimumDistanceCalculationById(Guid id, [FromBody] Model.SurveyRunMinimumDistanceCalculation? data)
        {
            if (data?.MetaInfo == null || data.MetaInfo.ID != id)
            {
                return BadRequest();
            }

            if (_manager.GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return await _manager.UpdateSurveyRunMinimumDistanceCalculationById(id, data)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        [HttpDelete("{id}", Name = "DeleteSurveyRunMinimumDistanceCalculationById")]
        public ActionResult DeleteSurveyRunMinimumDistanceCalculationById(Guid id)
        {
            if (_manager.GetSurveyRunMinimumDistanceCalculationById(id, includeResults: false) == null)
            {
                return NotFound();
            }

            return _manager.DeleteSurveyRunMinimumDistanceCalculationById(id)
                ? Ok()
                : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
