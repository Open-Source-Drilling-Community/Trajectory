using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OSDC.Drilling.Trajectory.Model;

namespace OSDC.Drilling.Trajectory.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class UncertaintyEnvelopeController : ControllerBase
{
    /// <summary>Project a circular borehole cross-section into the requested uncertainty plane and return a conservative enclosing ellipse.</summary>
    [HttpPost("Evaluate", Name = "EvaluatePhysicalUncertaintyEnvelope")]
    [ProducesResponseType<PhysicalUncertaintyEnvelopeEvaluation>(StatusCodes.Status200OK)]
    public ActionResult<PhysicalUncertaintyEnvelopeEvaluation> Evaluate([FromBody] PhysicalUncertaintyEnvelopeRequest? request) =>
        PhysicalUncertaintyEnvelopeEvaluator.TryEvaluate(request, out var result, out var error)
            ? Ok(result)
            : BadRequest(new { error = "invalid_physical_uncertainty_envelope", message = error });
}
