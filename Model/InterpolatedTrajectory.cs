using NORCE.Drilling.Trajectory.ModelShared;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using System.Collections.Generic;
using System.Linq;

namespace NORCE.Drilling.Trajectory.Model
{
    /// <summary>
    /// Full interpolated trajectory payload with interpolation settings and computed survey stations.
    /// </summary>
    public class InterpolatedTrajectory : InterpolatedTrajectoryLight
    {
        /// <summary>
        /// The list of survey stations resulting from the interpolation.
        /// </summary>
        public List<SurveyStation>? SurveyStationList { get; set; }

        /// <summary>
        /// The interpolation step along the abscissa.
        /// </summary>
        public double? InterpolationStep { get; set; }

        /// <summary>
        /// The depth reference to use for the interpolation step.
        /// </summary>
        public double? InterpolationReferenceDepth { get; set; }

        /// <summary>
        /// The maximum accepted distance between the chord and the arc.
        /// </summary>
        public double? MaximumChordArcDistance { get; set; }

        /// <summary>
        /// Flag indicating whether the first survey station of the source trajectory shall be included in the interpolation output.
        /// </summary>
        public bool IncludeFirstSurvey { get; set; }

        /// <summary>
        /// Flag indicating whether the last survey station of the source trajectory shall be included in the interpolation output.
        /// </summary>
        public bool IncludeLastSurvey { get; set; }

        /// <summary>
        /// Flag indicating whether interpolation shall be performed at casing and liner shoe depths.
        /// </summary>
        public bool InterpolateAtCasingAndLinerShoeDepths { get; set; }

        /// <summary>
        /// Flag indicating whether interpolation shall be performed at liner hanger depths.
        /// </summary>
        public bool InterpolateAtLinerHangerDepths { get; set; }

        /// <summary>
        /// Flag indicating whether interpolation shall be performed at casing change of diameter depths.
        /// </summary>
        public bool InterpolateAtCasingChangeOfDiameter { get; set; }

        /// <summary>
        /// Additional abscissas, possibly associated with an annotation, where interpolation shall also be performed.
        /// </summary>
        public List<AnnotatedAbscissa>? AdditionalAbscissaList { get; set; }

        /// <summary>
        /// Internally generated abscissas, possibly associated with an annotation, where interpolation shall also be performed.
        /// </summary>
        public List<AnnotatedAbscissa>? InternalAdditionalAbscissaList { get; set; }

        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public InterpolatedTrajectory() : base()
        {
        }

        /// <summary>
        /// Calculates the interpolated trajectory from a trajectory definition.
        /// </summary>
        /// <param name="trajectory">The source trajectory containing the survey stations and calculation method.</param>
        /// <returns>true if the interpolation succeeded, false otherwise.</returns>
        public bool Calculate(Trajectory trajectory)
        {
            if (trajectory?.SurveyStationList is not { Count: > 1 } surveyList)
            {
                SurveyStationList = null;
                return false;
            }

            List<(double, string)> abscissas = BuildMergedInterpolationAbscissaList();
            double? mdStep = null;
            if (InterpolationStep is { } interpolationStep &&
                Numeric.IsDefined(interpolationStep) &&
                Numeric.GT(interpolationStep, 0.0))
            {
                mdStep = interpolationStep;
                InterpolationStep = interpolationStep;
            }
            else
            {
                InterpolationStep = null;
            }

            if (mdStep == null && abscissas.Count == 0)
            {
                SurveyStationList = [];
                return true;
            }

            List<SurveyPoint>? interpolated = SurveyPoint.Interpolate(
                surveyList,
                mdStep,
                InterpolationReferenceDepth,
                trajectory.CalculationType,
                MaximumChordArcDistance,
                abscissas.Count > 0 ? abscissas : null);

            if (interpolated == null)
            {
                SurveyStationList = null;
                return false;
            }

            SurveyStationList = interpolated
                .Select(point =>
                {
                    SurveyStation station = new(point);
                    station.Annotation = point.Annotation;
                    return station;
                })
                .ToList();

            return true;
        }

        private List<(double, string)> BuildMergedInterpolationAbscissaList()
        {
            List<AnnotatedAbscissa> abscissas = [];

            if (AdditionalAbscissaList is { Count: > 0 })
            {
                foreach (AnnotatedAbscissa value in AdditionalAbscissaList)
                {
                    AddIfDefined(abscissas, value.Abscissa, value.Annotation);
                }
            }

            if (InternalAdditionalAbscissaList is { Count: > 0 })
            {
                foreach (AnnotatedAbscissa value in InternalAdditionalAbscissaList)
                {
                    AddIfDefined(abscissas, value.Abscissa, value.Annotation);
                }
            }

            return SortAndUnique(abscissas);
        }

        private static void AddIfDefined(List<AnnotatedAbscissa> abscissas, double? value, string? annotation = null)
        {
            if (value is { } defined && Numeric.IsDefined(defined))
            {
                AnnotatedAbscissa? existing = abscissas.FirstOrDefault(item => Numeric.EQ(item.Abscissa, defined));
                if (existing == null)
                {
                    abscissas.Add(new AnnotatedAbscissa { Abscissa = defined, Annotation = annotation });
                }
                else if (string.IsNullOrWhiteSpace(existing.Annotation) && !string.IsNullOrWhiteSpace(annotation))
                {
                    existing.Annotation = annotation;
                }
            }
        }

        private static List<(double, string)> SortAndUnique(List<AnnotatedAbscissa> abscissas)
        {
            return abscissas
                .OrderBy(item => item.Abscissa)
                .Select(item => (item.Abscissa, item.Annotation ?? string.Empty))
                .ToList();
        }
    }
}
