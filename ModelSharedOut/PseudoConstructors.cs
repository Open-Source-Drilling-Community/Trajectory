namespace NORCE.Drilling.Trajectory.ModelShared
{
	public class PseudoConstructors
	{
		public static MetaInfo ConstructMetaInfo()
			{
				return new MetaInfo 
				{
					ID = Guid.NewGuid(),
					HttpHostName = "https://dev.digiwells.no/",
					HttpHostBasePath = "Trajectory/api/",
					HttpEndPoint = "Trajectory/",
				};
			}

		public static MetaInfo ConstructMetaInfo(Guid id)
			{
				return new MetaInfo 
				{
					ID = id,
					HttpHostName = "https://dev.digiwells.no/",
					HttpHostBasePath = "Trajectory/api/",
					HttpEndPoint = "Trajectory/",
				};
			}
		public static Trajectory ConstructTrajectory()
		{
			return new Trajectory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				WellBoreID = new Guid(),
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
				InterpolatedTrajectory = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
				MDStep = 0.0, 
			};
		}
		public static ErrorSource ConstructErrorSource()
		{
			return new ErrorSource
			{
				MetaInfo = ConstructMetaInfo(),
				ErrorCode = (ErrorCode)0,
				Description = "Default Description",
				Index = 0, 
				IsSystematic = false, 
				IsRandom = false, 
				IsGlobal = false, 
				SingularIssues = false, 
				IsContinuous = false, 
				IsStationary = false, 
				KOperatorImposed = false, 
				Magnitude = null, 
				MagnitudeQuantity = "Default MagnitudeQuantity",
				UseInclinationInterval = false, 
				StartInclination = null, 
				EndInclination = null, 
				InitInclination = null, 
			};
		}
		public static SurveyInstrument ConstructSurveyInstrument()
		{
			return new SurveyInstrument
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				ModelType = (SurveyInstrumentModelType)0,
				ErrorSourceList = new List<ErrorSource>
					{
						ConstructErrorSource(),
					},
				Dip = 0.0, 
				Declination = 0.0, 
				Gravity = 0.0, 
				BField = 0.0, 
				Convergence = 0.0, 
				Latitude = 0.0, 
				EarthRotRate = 0.0, 
				CantAngle = 0.0, 
				GyroRunningSpeed = null, 
				ExtRefInitInc = null, 
				GyroSwitching = null, 
				GyroMinDist = null, 
				GyroNoiseRed = null, 
				UseRelDepthError = false, 
				RelDepthError = null, 
				UseMisalignment = false, 
				Misalignment = null, 
				UseTrueInclination = false, 
				TrueInclination = null, 
				UseReferenceError = false, 
				ReferenceError = null, 
				UseDrillStringMag = false, 
				DrillStringMag = null, 
				UseGyroCompassError = false, 
				GyroCompassError = null, 
			};
		}
		public static SurveyPoint ConstructSurveyPoint()
		{
			return new SurveyPoint
			{
				Z = null, 
				Abscissa = null, 
				MD = null, 
				Azimuth = null, 
				Inclination = null, 
				X = null, 
				Y = null, 
				TVD = null, 
				RiemannianNorth = null, 
				RiemannianEast = null, 
				Latitude = null, 
				Longitude = null, 
				Curvature = null, 
				Toolface = null, 
				BUR = null, 
				TUR = null, 
			};
		}
		public static SurveyStation ConstructSurveyStation()
		{
			return new SurveyStation
			{
				Z = null, 
				Abscissa = null, 
				MD = null, 
				Azimuth = null, 
				Inclination = null, 
				X = null, 
				Y = null, 
				TVD = null, 
				RiemannianNorth = null, 
				RiemannianEast = null, 
				Latitude = null, 
				Longitude = null, 
				Curvature = null, 
				Toolface = null, 
				BUR = null, 
				TUR = null, 
				Covariance = ConstructSymmetricMatrix3x3(),
				EigenVectors = ConstructMatrix3x3(),
				EigenValues = ConstructVector3D(),
				Bias = ConstructVector3D(),
				SurveyTool = ConstructSurveyInstrument(),
				BoreholeRadius = null, 
			};
		}
		public static Matrix3x3 ConstructMatrix3x3()
		{
			return new Matrix3x3
			{
				RowCount = 0, 
				ColumnCount = 0, 
			};
		}
		public static SymmetricMatrix3x3 ConstructSymmetricMatrix3x3()
		{
			return new SymmetricMatrix3x3
			{
				ColumnCount = 0, 
				RowCount = 0, 
			};
		}
		public static Vector3D ConstructVector3D()
		{
			return new Vector3D
			{
				X = null, 
				Y = null, 
				Z = null, 
				Dim = 0, 
			};
		}
		public static Cluster ConstructCluster()
		{
			return new Cluster
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				FieldID = null, 
				IsSingleWell = false, 
				RigID = null, 
				IsFixedPlatform = false, 
				ReferenceLatitude = ConstructGaussianDrillingProperty(),
				ReferenceLongitude = ConstructGaussianDrillingProperty(),
				ReferenceDepth = ConstructGaussianDrillingProperty(),
				GroundMudLineDepth = ConstructGaussianDrillingProperty(),
				TopWaterDepth = ConstructGaussianDrillingProperty(),
				Slots = new Dictionary<string,Slot>
					{
						{ "", ConstructSlot() }
					},
			};
		}
		public static CountPerDay ConstructCountPerDay()
		{
			return new CountPerDay
			{
				Date = DateTimeOffset.UtcNow,
				Count = 0, 
			};
		}
		public static History ConstructHistory()
		{
			return new History
			{
				Data = new List<CountPerDay>
					{
						ConstructCountPerDay(),
					},
			};
		}
		public static Slot ConstructSlot()
		{
			return new Slot
			{
				ID = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				Latitude = ConstructGaussianDrillingProperty(),
				Longitude = ConstructGaussianDrillingProperty(),
			};
		}
		public static UsageStatisticsCluster ConstructUsageStatisticsCluster()
		{
			return new UsageStatisticsCluster
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllClusterIdPerDay = ConstructHistory(),
				GetAllClusterMetaInfoPerDay = ConstructHistory(),
				GetClusterByIdPerDay = ConstructHistory(),
				GetAllClusterPerDay = ConstructHistory(),
				PostClusterPerDay = ConstructHistory(),
				PutClusterByIdPerDay = ConstructHistory(),
				DeleteClusterByIdPerDay = ConstructHistory(),
			};
		}
		public static GaussianDrillingProperty ConstructGaussianDrillingProperty()
		{
			return new GaussianDrillingProperty
			{
				GaussianValue = ConstructGaussianDistribution(),
			};
		}
		public static GaussianDistribution ConstructGaussianDistribution()
		{
			return new GaussianDistribution
			{
				MinValue = 0.0, 
				MaxValue = 0.0, 
				Mean = null, 
				StandardDeviation = null, 
			};
		}
		public static UsageStatisticsCartographicProjection ConstructUsageStatisticsCartographicProjection()
		{
			return new UsageStatisticsCartographicProjection
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllCartographicProjectionTypeIdPerDay = ConstructHistory(),
				GetCartographicProjectionTypeByIdPerDay = ConstructHistory(),
				GetAllCartographicProjectionTypePerDay = ConstructHistory(),
				GetAllCartographicProjectionIdPerDay = ConstructHistory(),
				GetAllCartographicProjectionMetaInfoPerDay = ConstructHistory(),
				GetCartographicProjectionByIdPerDay = ConstructHistory(),
				GetAllCartographicProjectionLightPerDay = ConstructHistory(),
				GetAllCartographicProjectionPerDay = ConstructHistory(),
				PostCartographicProjectionPerDay = ConstructHistory(),
				PutCartographicProjectionByIdPerDay = ConstructHistory(),
				DeleteCartographicProjectionByIdPerDay = ConstructHistory(),
				GetAllCartographicConversionSetIdPerDay = ConstructHistory(),
				GetAllCartographicConversionSetMetaInfoPerDay = ConstructHistory(),
				GetCartographicConversionSetByIdPerDay = ConstructHistory(),
				GetAllCartographicConversionSetLightPerDay = ConstructHistory(),
				GetAllCartographicConversionSetPerDay = ConstructHistory(),
				PostCartographicConversionSetPerDay = ConstructHistory(),
				PutCartographicConversionSetByIdPerDay = ConstructHistory(),
				DeleteCartographicConversionSetByIdPerDay = ConstructHistory(),
			};
		}
		public static CartographicConversionSet ConstructCartographicConversionSet()
		{
			return new CartographicConversionSet
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				CartographicProjectionID = null, 
				CartographicCoordinateList = new List<CartographicCoordinate>
					{
						ConstructCartographicCoordinate(),
					},
			};
		}
		public static CartographicCoordinate ConstructCartographicCoordinate()
		{
			return new CartographicCoordinate
			{
				Northing = null, 
				Easting = null, 
				VerticalDepth = null, 
				GeodeticCoordinate = ConstructGeodeticCoordinate(),
				GridConvergenceDatum = null, 
			};
		}
		public static CartographicProjection ConstructCartographicProjection()
		{
			return new CartographicProjection
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				ProjectionType = (ProjectionType)0,
				GeodeticDatumID = null, 
				LatitudeOrigin = null, 
				Latitude1 = null, 
				Latitude2 = null, 
				LatitudeTrueScale = null, 
				LongitudeOrigin = null, 
				Scaling = null, 
				FalseEasting = null, 
				FalseNorthing = null, 
				Zone = 0, 
				IsSouth = false, 
				IsHyperbolic = false, 
				ProjectionHeight = null, 
				HeightViewPoint = null, 
				Sweep = (AxisType)0,
				AzimuthCentralLine = null, 
				Weight = null, 
				Landsat = null, 
				Path = null, 
				Alpha = null, 
				Gamma = null, 
				Longitude1 = null, 
				Longitude2 = null, 
				LongitudeCentralPoint = null, 
				NoOffset = false, 
				NoRotation = false, 
				AreaNormalizationTransform = (AreaNormalizationTransformType)0,
				PegLatitude = null, 
				PegLongitude = null, 
				PegHeading = null, 
				N = null, 
				Q = null, 
			};
		}
		public static CartographicProjectionType ConstructCartographicProjectionType()
		{
			return new CartographicProjectionType
			{
				Projection = (ProjectionType)0,
				UseLatitudeOrigin = false, 
				UseLatitude1 = false, 
				UseLatitude2 = false, 
				UseLatitudeTrueScale = false, 
				UseLongitudeOrigin = false, 
				UseScaling = false, 
				UseFalseEastingNorthing = false, 
				UseZone = false, 
				UseSouth = false, 
				UseHyperbolic = false, 
				UseProjectionHeight = false, 
				UseHeightViewPoint = false, 
				UseSweep = false, 
				UseAzimuthCentralLine = false, 
				UseWeight = false, 
				UseLandsat = false, 
				UsePath = false, 
				UseAlpha = false, 
				UseGamma = false, 
				UseLongitude1 = false, 
				UseLongitude2 = false, 
				UseLongitudeCentralPoint = false, 
				UseNoOffset = false, 
				UseNoRotation = false, 
				UseAreaNormalizationTransform = false, 
				UsePegLatitude = false, 
				UsePegLongitude = false, 
				UsePegHeading = false, 
				UseN = false, 
				UseQ = false, 
			};
		}
		public static GeodeticCoordinate ConstructGeodeticCoordinate()
		{
			return new GeodeticCoordinate
			{
				LatitudeWGS84 = null, 
				LongitudeWGS84 = null, 
				VerticalDepthWGS84 = null, 
				LatitudeDatum = null, 
				LongitudeDatum = null, 
				VerticalDepthDatum = null, 
				OctreeDepth = 0, 
				OctreeCode = ConstructOctreeCodeLong(),
			};
		}
		public static OctreeCodeLong ConstructOctreeCodeLong()
		{
			return new OctreeCodeLong
			{
				Depth = 0, 
				CodeHigh = 0, 
				CodeLow = 0, 
			};
		}
		public static UsageStatisticsGeodeticDatum ConstructUsageStatisticsGeodeticDatum()
		{
			return new UsageStatisticsGeodeticDatum
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllSpheroidIdPerDay = ConstructHistory(),
				GetAllSpheroidMetaInfoPerDay = ConstructHistory(),
				GetSpheroidByIdPerDay = ConstructHistory(),
				GetAllSpheroidPerDay = ConstructHistory(),
				PostSpheroidPerDay = ConstructHistory(),
				PutSpheroidByIdPerDay = ConstructHistory(),
				DeleteSpheroidByIdPerDay = ConstructHistory(),
				GetAllGeodeticDatumIdPerDay = ConstructHistory(),
				GetAllGeodeticDatumMetaInfoPerDay = ConstructHistory(),
				GetGeodeticDatumByIdPerDay = ConstructHistory(),
				GetAllGeodeticDatumLightPerDay = ConstructHistory(),
				GetAllGeodeticDatumPerDay = ConstructHistory(),
				PostGeodeticDatumPerDay = ConstructHistory(),
				PutGeodeticDatumByIdPerDay = ConstructHistory(),
				DeleteGeodeticDatumByIdPerDay = ConstructHistory(),
				GetAllGeodeticConversionSetIdPerDay = ConstructHistory(),
				GetAllGeodeticConversionSetMetaInfoPerDay = ConstructHistory(),
				GetGeodeticConversionSetByIdPerDay = ConstructHistory(),
				GetAllGeodeticConversionSetLightPerDay = ConstructHistory(),
				GetAllGeodeticConversionSetPerDay = ConstructHistory(),
				PostGeodeticConversionSetPerDay = ConstructHistory(),
				PutGeodeticConversionSetByIdPerDay = ConstructHistory(),
				DeleteGeodeticConversionSetByIdPerDay = ConstructHistory(),
			};
		}
		public static GeodeticConversionSet ConstructGeodeticConversionSet()
		{
			return new GeodeticConversionSet
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				GeodeticDatum = ConstructGeodeticDatum(),
				OctreeBounds = ConstructBounds(),
				GeodeticCoordinates = new List<GeodeticCoordinate>
					{
						ConstructGeodeticCoordinate(),
					},
			};
		}
		public static GeodeticDatum ConstructGeodeticDatum()
		{
			return new GeodeticDatum
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				IsDefault = false, 
				Spheroid = ConstructSpheroid(),
				DeltaX = ConstructScalarDrillingProperty(),
				DeltaY = ConstructScalarDrillingProperty(),
				DeltaZ = ConstructScalarDrillingProperty(),
				RotationX = ConstructScalarDrillingProperty(),
				RotationY = ConstructScalarDrillingProperty(),
				RotationZ = ConstructScalarDrillingProperty(),
				ScaleFactor = ConstructScalarDrillingProperty(),
			};
		}
		public static Spheroid ConstructSpheroid()
		{
			return new Spheroid
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				IsDefault = false, 
				SemiMajorAxis = ConstructScalarDrillingProperty(),
				IsSemiMajorAxisSet = false, 
				SemiMinorAxis = ConstructScalarDrillingProperty(),
				IsSemiMinorAxisSet = false, 
				Eccentricity = ConstructScalarDrillingProperty(),
				IsEccentricitySet = false, 
				SquaredEccentricity = ConstructScalarDrillingProperty(),
				IsSquaredEccentricitySet = false, 
				Flattening = ConstructScalarDrillingProperty(),
				IsFlatteningSet = false, 
				InverseFlattening = ConstructScalarDrillingProperty(),
				IsInverseFlatteningSet = false, 
			};
		}
		public static ScalarDrillingProperty ConstructScalarDrillingProperty()
		{
			return new ScalarDrillingProperty
			{
				DiracDistributionValue = ConstructDiracDistribution(),
			};
		}
		public static Point3D ConstructPoint3D()
		{
			return new Point3D
			{
				X = null, 
				Y = null, 
				Z = null, 
			};
		}
		public static Bounds ConstructBounds()
		{
			return new Bounds
			{
				MinX = null, 
				MaxX = null, 
				MinY = null, 
				MaxY = null, 
				MinZ = null, 
				MaxZ = null, 
				MiddleX = null, 
				MiddleY = null, 
				MiddleZ = null, 
				IntervalX = null, 
				IntervalY = null, 
				IntervalZ = null, 
				Center = ConstructPoint3D(),
			};
		}
		public static DiracDistribution ConstructDiracDistribution()
		{
			return new DiracDistribution
			{
				MinValue = 0.0, 
				MaxValue = 0.0, 
				Value = null, 
			};
		}
		public static Field ConstructField()
		{
			return new Field
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				CartographicProjectionID = null, 
			};
		}
		public static FieldCartographicConversionSet ConstructFieldCartographicConversionSet()
		{
			return new FieldCartographicConversionSet
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				FieldID = null, 
				CartographicCoordinateList = new List<CartographicCoordinate>
					{
						ConstructCartographicCoordinate(),
					},
			};
		}
		public static UsageStatisticsField ConstructUsageStatisticsField()
		{
			return new UsageStatisticsField
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllFieldIdPerDay = ConstructHistory(),
				GetAllFieldMetaInfoPerDay = ConstructHistory(),
				GetFieldByIdPerDay = ConstructHistory(),
				GetAllFieldLightPerDay = ConstructHistory(),
				GetAllFieldPerDay = ConstructHistory(),
				PostFieldPerDay = ConstructHistory(),
				PutFieldByIdPerDay = ConstructHistory(),
				DeleteFieldByIdPerDay = ConstructHistory(),
				GetAllFieldCartographicConversionSetIdPerDay = ConstructHistory(),
				GetAllFieldCartographicConversionSetMetaInfoPerDay = ConstructHistory(),
				GetFieldCartographicConversionSetByIdPerDay = ConstructHistory(),
				GetAllFieldCartographicConversionSetLightPerDay = ConstructHistory(),
				GetAllFieldCartographicConversionSetPerDay = ConstructHistory(),
				PostFieldCartographicConversionSetPerDay = ConstructHistory(),
				PutFieldCartographicConversionSetByIdPerDay = ConstructHistory(),
				DeleteFieldCartographicConversionSetByIdPerDay = ConstructHistory(),
			};
		}
		public static UsageStatisticsWellBore ConstructUsageStatisticsWellBore()
		{
			return new UsageStatisticsWellBore
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllWellBoreIdPerDay = ConstructHistory(),
				GetAllWellBoreMetaInfoPerDay = ConstructHistory(),
				GetWellBoreByIdPerDay = ConstructHistory(),
				GetAllWellBorePerDay = ConstructHistory(),
				PostWellBorePerDay = ConstructHistory(),
				PutWellBoreByIdPerDay = ConstructHistory(),
				DeleteWellBoreByIdPerDay = ConstructHistory(),
			};
		}
		public static WellBore ConstructWellBore()
		{
			return new WellBore
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				WellID = null, 
				RigID = null, 
				IsSidetrack = false, 
				ParentWellBoreID = null, 
				TieInPointAlongHoleDepth = ConstructGaussianDrillingProperty(),
				SidetrackType = (SidetrackType)0,
			};
		}
		public static UsageStatisticsWell ConstructUsageStatisticsWell()
		{
			return new UsageStatisticsWell
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllWellIdPerDay = ConstructHistory(),
				GetAllWellMetaInfoPerDay = ConstructHistory(),
				GetWellByIdPerDay = ConstructHistory(),
				GetAllWellPerDay = ConstructHistory(),
				PostWellPerDay = ConstructHistory(),
				PutWellByIdPerDay = ConstructHistory(),
				DeleteWellByIdPerDay = ConstructHistory(),
			};
		}
		public static Well ConstructWell()
		{
			return new Well
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				SlotID = null, 
				ClusterID = null, 
				IsSingleWell = false, 
			};
		}
	}
}