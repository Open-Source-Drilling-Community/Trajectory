using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;
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
    public class SurveyStationEllipseCalculationController : ControllerBase
    {
        private readonly ILogger<SurveyStationEllipseCalculationManager> _logger;
        private readonly SurveyStationEllipseCalculationManager _manager;

        public SurveyStationEllipseCalculationController(ILogger<SurveyStationEllipseCalculationManager> logger, SqlConnectionManager connectionManager)
        {
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
            return calculation != null ? Ok(calculation) : BadRequest();
        }

        [HttpDelete("{id}", Name = "DeleteSurveyStationEllipseCalculationById")]
        public ActionResult DeleteSurveyStationEllipseCalculationById(Guid id)
        {
            if (_manager.GetSurveyStationEllipseCalculationById(id) == null)
            {
                _logger.LogWarning("The SurveyStationEllipseCalculation of given ID does not exist");
                return NotFound();
            }

            return _manager.DeleteSurveyStationEllipseCalculationById(id) ? Ok() : StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
