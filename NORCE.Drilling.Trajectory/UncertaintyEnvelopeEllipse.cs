using System;
using System.Collections.Generic;
using System.Text;
using NORCE.General.Math;

namespace NORCE.Drilling.Trajectory
{
    public class UncertaintyEnvelopeEllipse
    {
        public double? test { get; set; }
        /// <summary>
        /// Measured depth associated with the survey station
        /// </summary>
        public double? MD { get; set; }
        /// <summary>
        ///North associated with the survey station
        /// </summary>
        public double? X { get; set; }
        /// <summary>
        /// East associated with the survey station
        /// </summary>
        public double? Y { get; set; }
        /// <summary>
        /// Depth associated with the survey station
        /// </summary>
        public double? Z { get; set; }
        /// <summary>
        /// Inclination associated with survey station
        /// </summary>
        public double? Inclination { get; set; }

        /// <summary>
        /// Azimuth associated with survey station
        /// </summary>
        public double? Azimuth { get; set; }

        /// <summary>
        /// Ellipse radiuses of uncertainty envelope associated with survey station
        /// </summary>
        public Vector2D EllipseRadius { get; set; }
        public double PerpendicularDirection { get; set; }
        /// <summary>
        /// Ellipse N, E, TVD coordinates of uncertainty envelope associated with survey station
        /// </summary>
        public List<Point3D> EllipseCoordinates { get; set; }
        /// <summary>
        /// Ellipse N, E, TVD coordinates of area covered by a uncertainty ellipse of the uncertainty envelope associated with survey station
        /// </summary>
        public List<Point3D> EllipseAreaCoordinates { get; set; }

        /// <summary>
        /// default constructor
        /// </summary>
        public UncertaintyEnvelopeEllipse()
        {
            MD = null;
            Inclination = null;
            Azimuth = null;
        }
    }
}
