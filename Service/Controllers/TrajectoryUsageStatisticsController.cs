using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Trajectory.Model;

namespace NORCE.Drilling.Trajectory.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class TrajectoryUsageStatisticsController : ControllerBase
    {
        private readonly ILogger _logger;

        public TrajectoryUsageStatisticsController(ILogger<TrajectoryUsageStatisticsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the usage statistics present in the microservice database at endpoint Trajectory/api/TrajectoryUsageStatistics
        /// </summary>
        [HttpGet(Name = "GetTrajectoryUsageStatistics")]
        public ActionResult<UsageStatisticsTrajectory> GetTrajectoryUsageStatistics()
        {
            if (UsageStatisticsTrajectory.Instance != null)
            {
                return Ok(UsageStatisticsTrajectory.Instance);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
