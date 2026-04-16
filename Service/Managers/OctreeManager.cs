using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.Drilling.Surveying;
using OSDC.DotnetLibraries.General.Common;
using OSDC.DotnetLibraries.General.Octree;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Trajectory.Service.Managers
{
    /// <summary>
    /// A manager for GlobalAntiCollision. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class OctreeManager
    {
        public object lock_ = new object();
        private static OctreeManager? _instance = null;
        private readonly ILogger<OctreeManager> _logger;
        private readonly SqlConnectionManagerOctree _connectionManager;

        #region Octree settings
        private int octreeDepthCache_ = SqlConnectionManagerOctree.OctreeDepthCache;
        public int OctreeDepthDetails { get; } = 23; // Corresponds to 40 000 000 m / 2^23 ~ 4.8 m

        private double minX_ = -Numeric.PI / 2.0;
        private double minY_ = -Numeric.PI;
        private double minZ_ = -6000000.0; // The radius of the earth is around 6000 km.
        private double maxX_ = Numeric.PI / 2.0;
        private double maxY_ = Numeric.PI;
        private double maxZ_ = 34000000.0; // We want the resolution in z to be of the same order of magnitude as for the other directions in the relevant region (circumference of the earth is ca 40 000 km)
        #endregion

        #region Octree settings for debugging against octree database from the summer demo containing 16 duplicates of Ullrigg wells
        /*
        private int octreeDepthCache_ = 7;
        private int octreeDepthDetails_ = 10;

        private double minX_ = -710.55;
        private double minY_ = -133.79;
        private double minZ_ = 0;
        private double maxX_ = 2544.7699999999995;
        private double maxY_ = 4292.45;
        private double maxZ_ = 6707.2;
        */
        #endregion

        private OctreeManager(ILogger<OctreeManager> logger, SqlConnectionManagerOctree connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static OctreeManager GetInstance(ILogger<OctreeManager> logger, SqlConnectionManagerOctree connectionManager)
        {
            _instance ??= new OctreeManager(logger, connectionManager);
            return _instance;
        }

        public bool Clear()
        {
            return _connectionManager.CleanContent();
        }

        public bool Contains(Guid id)
        {
            return _connectionManager.Contains(id);
        }

        internal List<OctreeCodeLong> GetLeavesFromSurveyList(List<SurveyStation>? surveyList, UncertaintyEnvelope.ErrorModelType errorModelType = UncertaintyEnvelope.ErrorModelType.WolffAndDeWardt)
        {
            List<OctreeCodeLong> leaves = new List<OctreeCodeLong>();
            if (surveyList is { Count: >= 2 })
            {
                #region Calculate the uncertainty envelope at confidencefactor 0.999 and scalingFactor = 1.0 with 0.1m spacing between intermediate ellipses and 720 point for each ellipse
                double confidencefactor = 0.999;
                double scalingFactor = 1.0;

                UncertaintyEnvelope uncertaintyEnvelope = new()
                {
                    ErrorModel = errorModelType,
                    SurveyStationList = surveyList,
                    MeshSectorCount = 720,
                    MeshLongitudinalLength = 0.1,
                };
                uncertaintyEnvelope.ConfidenceFactor = confidencefactor;
                uncertaintyEnvelope.ScalingFactor = scalingFactor;
                bool ok = uncertaintyEnvelope.Calculate();
                List<UncertaintyEllipse>? ellipses = ok ? uncertaintyEnvelope.MeshedEllipseList : null;

                // Note that TVD is positive downwards, but we correct for that when we convert to Point3D which are being plotted. We also add some additional margins to make sure we can plot the lower part of the envelope
                Octree<OctreeCodeLong> octree = new Octree<OctreeCodeLong>(minX_, maxX_, minY_, maxY_, minZ_, maxZ_);
                if (ellipses is { Count: > 2 })
                {
                    foreach (UncertaintyEllipse ellipse in ellipses)
                    {
                        // We allow for zero ellipse radius here since that is typical for the first ellipse at MD = 0
                        List<SurveyPoint>? ellipseVertices = ellipse.EllipseVertices;
                        if (ellipse.EllipseRadii?[0] is not double ellipseRadius ||
                            !Numeric.GE(ellipseRadius, 0.0) ||
                            ellipseVertices == null)
                        {
                            continue;
                        }

                        // Fill the ellipse coordinates for each well into the corresponding octree
                        foreach (SurveyPoint sp in ellipseVertices) // Previously surveyList.UncertaintyEnvelope[n].EllipseCoordinates)
                        {
                            if (sp.Latitude is double latitude &&
                                sp.Longitude is double longitude &&
                                sp.TVD is double tvd)
                            {
                                octree.Add(latitude, longitude, tvd, OctreeDepthDetails);
                            }
                        }
                    }
                }

                // Extract the leaves of each octree
                List<OctreeCodeLong>? octreeLeaves = octree.GetLeaves(OctreeDepthDetails);
                leaves = octreeLeaves ?? [];
                // Now we don't need the octree anymore
                octree.DeleteRootNodes();
                #endregion
            }
            return leaves ?? [];
        }

        public List<Guid> GetIDs()
        {
            return _connectionManager.GetAllTrajectoryIDs(false, true, true) ?? [];
        }

        public List<OctreeCodeLong> Get(Guid ID)
        {
            return _connectionManager.GetDetails(ID) ?? [];
        }

        public bool AddDetails(Guid ID, List<OctreeCodeLong>? code)
        {
            return _connectionManager.AddDetails(code, ID, false, true, true);
        }

        public bool AddInCache(byte[] octreeCode)
        {
            return _connectionManager.AddInCache(octreeCode);
        }

        public bool Remove(Guid ID)
        {
            if (!ID.Equals(Guid.Empty))
            {
                return _connectionManager.DeleteDetails(ID);
            }
            return false;
        }

        public bool Update(Guid ID, List<OctreeCodeLong>? code)
        {
            if (!ID.Equals(Guid.Empty) && code != null)
            {
                return _connectionManager.Add(code, ID, false, true, true);
            }
            return false;
        }

        public bool Delete(Guid trajectoryID)
        {
            if (!trajectoryID.Equals(Guid.Empty))
            {
                return _connectionManager.Delete(trajectoryID);
            }
            return false;
        }

        public bool Add(List<OctreeCodeLong> codes, Guid trajectoryID, bool isPlanned, bool isMeasured, bool isDefinitive)
        {
            if (!trajectoryID.Equals(Guid.Empty) && codes != null)
            {
                return _connectionManager.Add(codes, trajectoryID, false, true, true);
            }
            return false;
        }

        public List<Guid> Search(List<OctreeCodeLong>? codes, bool isPlanned, bool isMeasured, bool isDefinitive, Guid? investigatedTrajectoryID = null)
        { 
            return _connectionManager.Search(codes, isPlanned, isMeasured, isDefinitive, investigatedTrajectoryID) ?? [];
        }
    }
}
