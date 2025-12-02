using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Statistics;
using NORCE.Drilling.Trajectory.Model;

namespace NORCE.Drilling.Trajectory.ModelTest
{
    public class Tests
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
        }

        [Test]
        public void Test_Calculus()
        {
            Guid guid = Guid.NewGuid();
            MetaInfo metaInfo = new() { ID = guid };
            DateTimeOffset creationDate = DateTimeOffset.UtcNow;

            Guid guid2 = Guid.NewGuid();
            MetaInfo metaInfo2 = new() { ID = guid2 };
            DateTimeOffset creationDate2 = DateTimeOffset.UtcNow;
            ScalarDrillingProperty derivedData1Param = new() { DiracDistributionValue = new DiracDistribution() { Value = 2.0 } };
            Model.DerivedData1 derivedData1 = new() { DerivedData1Param = derivedData1Param };
            SurveyPoint surveyPoint = new()
            {
                MetaInfo = metaInfo2,
                Name = "My test SurveyPoint name",
                Description = "My test SurveyPoint for POST",
                CreationDate = creationDate,
                LastModificationDate = creationDate2,
                SurveyPointParam = new ScalarDrillingProperty() { DiracDistributionValue = new DiracDistribution() { Value = 1.0 } },
                DerivedData1 = derivedData1,
                Type = SurveyPointType.DerivedData1
            };
            Model.Trajectory trajectory = new()
            {
                MetaInfo = metaInfo,
                Name = "My test Trajectory",
                Description = "My test Trajectory",
                CreationDate = creationDate,
                LastModificationDate = creationDate,
                SurveyPointList = [surveyPoint],
            };

            Assert.That(trajectory.OutputParam, Is.Null);
            trajectory.Calculate();
            Assert.That(trajectory.OutputParam, Is.EqualTo(3));
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
        }
    }
}