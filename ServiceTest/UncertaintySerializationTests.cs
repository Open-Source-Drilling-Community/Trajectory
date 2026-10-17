extern alias ModelSharedOutAlias;
using System.Text.Json;
using NUnit.Framework;
using OSDC.Drilling.Trajectory.Service;
using OSDC.DotnetLibraries.General.Math;
using OSDC.DotnetLibraries.Drilling.Surveying;
namespace ServiceTest;
public class UncertaintySerializationTests
{
 [Test]
 public void Matrix_and_eigenvectors_round_trip_all_entries_and_legacy_stays_unknown()
 {
  var covariance=new SymmetricMatrix3x3();var vectors=new Matrix3x3();for(int r=0;r<3;r++)for(int c=0;c<3;c++){covariance[c,r]=c==r?4:1;vectors[c,r]=c==r?1:0;}
  var station=new SurveyStation{Covariance=covariance,EigenVectors=vectors};var clone=JsonSerializer.Deserialize<SurveyStation>(JsonSerializer.Serialize(station,JsonSettings.Options),JsonSettings.Options)!;
  for(int r=0;r<3;r++)for(int c=0;c<3;c++){Assert.That(clone.Covariance![c,r],Is.EqualTo(covariance[c,r]));Assert.That(clone.EigenVectors![c,r],Is.EqualTo(vectors[c,r]));}
  var clientStation=JsonSerializer.Deserialize<ModelSharedOutAlias::OSDC.Drilling.Trajectory.ModelShared.SurveyStation>(JsonSerializer.Serialize(station,JsonSettings.Options))!;
  var clientOptions=new JsonSerializerOptions{DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull};clientOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
  var fromClient=JsonSerializer.Deserialize<SurveyStation>(JsonSerializer.Serialize(clientStation,clientOptions),JsonSettings.Options)!;
  Assert.That(fromClient.Covariance![0,0],Is.EqualTo(4));
  var legacy=JsonSerializer.Deserialize<SymmetricMatrix3x3>("{\"ColumnCount\":3,\"RowCount\":3}",JsonSettings.Options)!;Assert.That(legacy[0,0],Is.Null);
 }
 [TestCase("{\"FormatVersion\":2,\"Values\":[1,0,0,0,1,0,0,0,1]}")]
 [TestCase("{\"FormatVersion\":1,\"Values\":[1,0]}")]
 [TestCase("{\"FormatVersion\":1,\"Values\":[1,2,0,0,1,0,0,0,1]}")]
 public void Malformed_or_asymmetric_matrix_is_rejected(string json)=>Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SymmetricMatrix3x3>(json,JsonSettings.Options));
 [Test]
 public void Reference_covariance_is_added_once_to_relative_station_covariance()
 {
  var reference=new SymmetricMatrix3x3();var relative=new SymmetricMatrix3x3();for(int r=0;r<3;r++)for(int c=0;c<3;c++){reference[c,r]=c==r?100:0;relative[c,r]=c==r?4:0;}
  var station=new SurveyStation{Covariance=relative};Assert.That(UncertaintyPropagation.AddIndependentReference([station],reference),Is.True);Assert.That(station.Covariance![0,0],Is.EqualTo(104));Assert.That(reference[0,0],Is.EqualTo(100));Assert.That(relative[0,0],Is.EqualTo(4));
  var invalid=new SymmetricMatrix3x3();invalid[0,0]=-1;Assert.That(UncertaintyPropagation.IsUsable(invalid),Is.False);
 }
}
