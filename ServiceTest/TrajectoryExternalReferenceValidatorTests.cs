using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Trajectory.Model;
using OSDC.Drilling.Trajectory.Service;

namespace ServiceTest;

[TestFixture]
public sealed class TrajectoryExternalReferenceValidatorTests
{
    [Test]
    public async Task Existing_trajectory_references_are_valid_and_each_distinct_reference_is_read_once()
    {
        Guid fieldId = Guid.NewGuid(), clusterId = Guid.NewGuid(), wellId = Guid.NewGuid(), wellBoreId = Guid.NewGuid();
        HashSet<Guid> expected = [fieldId, clusterId, wellId, wellBoreId];
        var handler = new StubHandler(request => Resource(expected.Single(id => request.RequestUri!.AbsolutePath.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))));
        TrajectoryExternalReferenceValidator validator = CreateValidator(handler);
        var value = new TrajectoryLight
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, FieldID = fieldId, ClusterID = clusterId,
            WellID = wellId, WellBoreID = wellBoreId
        };

        IReadOnlyList<TrajectoryExternalReferenceValidation> results =
            await validator.ValidateTrajectoriesAsync([value, value], CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(results.All(result => result.Status == ExternalReferenceValidationStatus.Valid), Is.True);
            Assert.That(results.All(result => result.FieldExists == true && result.ClusterExists == true &&
                                              result.WellExists == true && result.WellBoreExists == true), Is.True);
            Assert.That(handler.CallCount, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task Missing_survey_instrument_is_invalid()
    {
        Guid fieldId = Guid.NewGuid(), clusterId = Guid.NewGuid(), wellId = Guid.NewGuid(), wellBoreId = Guid.NewGuid();
        Guid instrumentId = Guid.NewGuid();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.Contains("/SurveyInstrument/", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : Resource(new[] { fieldId, clusterId, wellId, wellBoreId }
                .Single(id => request.RequestUri.AbsolutePath.Contains(id.ToString(), StringComparison.OrdinalIgnoreCase))));
        var value = new SurveyRunLight
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, FieldID = fieldId, ClusterID = clusterId,
            WellID = wellId, WellBoreID = wellBoreId, SurveyInstrumentID = instrumentId
        };

        SurveyRunExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateSurveyRunsAsync([value], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ExternalReferenceValidationStatus.Invalid));
            Assert.That(result.SurveyInstrumentExists, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code), Does.Contain("survey_instrument_not_found"));
        });
    }

    [Test]
    public async Task Dependency_failure_is_unavailable_and_not_invalid()
    {
        Guid wellBoreId = Guid.NewGuid();
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var value = new TrajectoryLight { MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, WellBoreID = wellBoreId };

        TrajectoryExternalReferenceValidation result = (await CreateValidator(handler)
            .ValidateTrajectoriesAsync([value], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(ExternalReferenceValidationStatus.Unavailable));
            Assert.That(result.WellBoreExists, Is.Null);
            Assert.That(result.Issues.Select(issue => issue.Code), Does.Contain("wellbore_service_error"));
        });
    }

    [Test]
    public async Task Optional_unlinked_references_are_valid_but_empty_required_references_are_invalid_without_http()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("HTTP must not be called."));
        var trajectory = new TrajectoryLight { MetaInfo = new MetaInfo { ID = Guid.NewGuid() } };
        var surveyRun = new SurveyRunLight { MetaInfo = new MetaInfo { ID = Guid.NewGuid() } };
        TrajectoryExternalReferenceValidator validator = CreateValidator(handler);

        TrajectoryExternalReferenceValidation trajectoryResult =
            (await validator.ValidateTrajectoriesAsync([trajectory], CancellationToken.None)).Single();
        SurveyRunExternalReferenceValidation surveyRunResult =
            (await validator.ValidateSurveyRunsAsync([surveyRun], CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(trajectoryResult.Status, Is.EqualTo(ExternalReferenceValidationStatus.Invalid));
            Assert.That(trajectoryResult.Issues.Select(issue => issue.Property), Does.Contain("WellBoreID"));
            Assert.That(surveyRunResult.Status, Is.EqualTo(ExternalReferenceValidationStatus.Invalid));
            Assert.That(surveyRunResult.Issues.Select(issue => issue.Property), Does.Contain("WellBoreID"));
            Assert.That(surveyRunResult.Issues.Select(issue => issue.Property), Does.Contain("SurveyInstrumentID"));
            Assert.That(handler.CallCount, Is.Zero);
        });
    }

    private static TrajectoryExternalReferenceValidator CreateValidator(HttpMessageHandler handler)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FieldHostURL"] = "https://field.test/",
            ["ClusterHostURL"] = "https://cluster.test/",
            ["WellHostURL"] = "https://well.test/",
            ["WellBoreHostURL"] = "https://wellbore.test/",
            ["SurveyInstrumentHostURL"] = "https://instrument.test/"
        }).Build();
        return new TrajectoryExternalReferenceValidator(new StubClientFactory(handler), configuration);
    }

    private static HttpResponseMessage Resource(Guid id) => new(HttpStatusCode.OK)
    {
        Content = new StringContent($"{{\"MetaInfo\":{{\"ID\":\"{id}\"}}}}", Encoding.UTF8, "application/json")
    };

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(response(request));
        }
    }
}
