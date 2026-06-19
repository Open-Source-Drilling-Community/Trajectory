using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// A survey run is an imported measured survey dataset acquired with one survey instrument.
    /// </summary>
    public class SurveyRun : SurveyRunLight
    {
        /// <summary>
        /// The calculated tie-in station for this survey run.
        /// </summary>
        public SurveyStation? TieInPoint { get; set; }
        /// <summary>
        /// The compact editable survey measurements imported for this survey run.
        /// </summary>
        public List<SurveyMeasurement>? SurveyMeasurementList { get; set; }
        /// <summary>
        /// The calculated survey stations for this survey run.
        /// </summary>
        public List<SurveyStation>? SurveyStationList { get; set; }

        public SurveyRun() : base()
        {
        }

        /// <summary>
        /// Calculates station coordinates from the measured survey and the resolved tie-in point.
        /// </summary>
        public bool Calculate()
        {
            List<SurveyStation> calculatedList = BuildCalculationInput();
            if (calculatedList.Count < 2)
            {
                return false;
            }

            if (TieInPoint is { } tieInPoint && (tieInPoint.MD ?? tieInPoint.Abscissa) is { } tieInAbscissa)
            {
                List<SurveyStation> updatedList = calculatedList
                    .Where(station => (station?.MD ?? station?.Abscissa) is { } abscissa && Numeric.GE(abscissa, tieInAbscissa))
                    .ToList();

                if (updatedList.Count > 0)
                {
                    SurveyStation tieInStation = new(tieInPoint)
                    {
                        VerticalSection = tieInPoint.VerticalSection ?? 0
                    };

                    if ((updatedList[0].MD ?? updatedList[0].Abscissa) is { } firstAbscissa && Numeric.EQ(firstAbscissa, tieInAbscissa))
                    {
                        updatedList[0] = tieInStation;
                    }
                    else
                    {
                        updatedList.Insert(0, tieInStation);
                    }

                    calculatedList = updatedList;
                }
            }

            SurveyStationList = calculatedList;
            return SurveyPoint.CompleteSurvey(SurveyStationList, CalculationType);
        }

        private List<SurveyStation> BuildCalculationInput()
        {
            if (SurveyMeasurementList is { Count: > 0 })
            {
                return SurveyMeasurementList
                    .Where(measurement => measurement != null)
                    .Select(measurement => measurement.ToSurveyStation())
                    .ToList();
            }

            return SurveyStationList?
                .Where(station => station != null)
                .Select(station => new SurveyStation
                {
                    MD = station.MD ?? station.Abscissa,
                    Abscissa = station.MD ?? station.Abscissa,
                    Inclination = station.Inclination,
                    Azimuth = station.Azimuth,
                    Annotation = station.Annotation
                })
                .ToList() ?? [];
        }
    }
}
