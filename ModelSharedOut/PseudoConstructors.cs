namespace OSDC.Drilling.Trajectory.ModelShared
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
		public static EarthMagneticFieldEvaluationPoint ConstructEarthMagneticFieldEvaluationPoint()
		{
			return new EarthMagneticFieldEvaluationPoint
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				Depth = 0.0, 
				DateTimeUtc = DateTimeOffset.UtcNow,
			};
		}
		public static EarthMagneticFieldSample ConstructEarthMagneticFieldSample()
		{
			return new EarthMagneticFieldSample
			{
				Input = ConstructEarthMagneticFieldEvaluationPoint(),
				North = 0.0, 
				East = 0.0, 
				Down = 0.0, 
				HorizontalIntensity = 0.0, 
				TotalIntensity = 0.0, 
				Declination = null, 
				Inclination = null, 
			};
		}
		public static EarthMagneticFieldServiceInfo ConstructEarthMagneticFieldServiceInfo()
		{
			return new EarthMagneticFieldServiceInfo
			{
				Name = "Default Name",
				Description = "Default Description",
				CoordinateFrame = "Default CoordinateFrame",
				TimeConvention = "Default TimeConvention",
				DepthReference = "Default DepthReference",
				DepthPositiveDirection = "Default DepthPositiveDirection",
				Models = new List<EarthMagneticModelInfo>
					{
						ConstructEarthMagneticModelInfo(),
					},
			};
		}
		public static EarthMagneticFieldValidationError ConstructEarthMagneticFieldValidationError()
		{
			return new EarthMagneticFieldValidationError
			{
				SampleIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static EarthMagneticFieldValidationProblem ConstructEarthMagneticFieldValidationProblem()
		{
			return new EarthMagneticFieldValidationProblem
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<EarthMagneticFieldValidationError>
					{
						ConstructEarthMagneticFieldValidationError(),
					},
			};
		}
		public static EarthMagneticModelInfo ConstructEarthMagneticModelInfo()
		{
			return new EarthMagneticModelInfo
			{
				Model = (EarthMagneticFieldModel)0,
				Name = "Default Name",
				ID = "Default ID",
				Description = "Default Description",
				ReleaseDate = DateTimeOffset.UtcNow,
				MinimumUtc = DateTimeOffset.UtcNow,
				MaximumUtc = DateTimeOffset.UtcNow,
				MinimumDepth = 0.0, 
				MaximumDepth = 0.0, 
				Degree = 0, 
				Order = 0, 
				GeographicLibVersion = "Default GeographicLibVersion",
				ReferenceEllipsoid = "Default ReferenceEllipsoid",
				CoordinateFrame = "Default CoordinateFrame",
				MagneticFluxDensityUnit = "Default MagneticFluxDensityUnit",
				AngleUnit = "Default AngleUnit",
				DepthPositiveDirection = "Default DepthPositiveDirection",
				ConcurrentEvaluationEnabled = false, 
				MetadataSHA256 = "Default MetadataSHA256",
				CoefficientSHA256 = "Default CoefficientSHA256",
			};
		}
		public static EvaluateEarthMagneticFieldRequest ConstructEvaluateEarthMagneticFieldRequest()
		{
			return new EvaluateEarthMagneticFieldRequest
			{
				Model = (EarthMagneticFieldModel)0,
				Samples = new List<EarthMagneticFieldEvaluationPoint>
					{
						ConstructEarthMagneticFieldEvaluationPoint(),
					},
			};
		}
		public static EvaluateEarthMagneticFieldResponse ConstructEvaluateEarthMagneticFieldResponse()
		{
			return new EvaluateEarthMagneticFieldResponse
			{
				Model = ConstructEarthMagneticModelInfo(),
				Samples = new List<EarthMagneticFieldSample>
					{
						ConstructEarthMagneticFieldSample(),
					},
			};
		}
		public static UsageStatisticsEarthMagneticField ConstructUsageStatisticsEarthMagneticField()
		{
			return new UsageStatisticsEarthMagneticField
			{
				StartedAt = DateTimeOffset.UtcNow,
				Scope = "Default Scope",
				RestEvaluations = 0, 
				MCPEvaluations = 0, 
				FailedEvaluations = 0, 
				SamplesEvaluated = 0, 
				ModelInfoRequests = 0, 
				StatisticsRequests = 0, 
			};
		}
		public static EarthGravityEvaluationRequest ConstructEarthGravityEvaluationRequest()
		{
			return new EarthGravityEvaluationRequest
			{
				Positions = new List<EarthGravityPosition>
					{
						ConstructEarthGravityPosition(),
					},
			};
		}
		public static EarthGravityEvaluationResponse ConstructEarthGravityEvaluationResponse()
		{
			return new EarthGravityEvaluationResponse
			{
				Model = ConstructEarthGravityModelInfo(),
				Samples = new List<EarthGravitySample>
					{
						ConstructEarthGravitySample(),
					},
			};
		}
		public static EarthGravityModelInfo ConstructEarthGravityModelInfo()
		{
			return new EarthGravityModelInfo
			{
				Name = "Default Name",
				ID = "Default ID",
				Publisher = "Default Publisher",
				ReleaseDate = "Default ReleaseDate",
				DataVersion = "Default DataVersion",
				Degree = 0, 
				Order = 0, 
				GeographicLibVersion = "Default GeographicLibVersion",
				ReferenceEllipsoid = "Default ReferenceEllipsoid",
				IncludesCentrifugalAcceleration = false, 
				CoefficientSHA256 = "Default CoefficientSHA256",
			};
		}
		public static EarthGravityPosition ConstructEarthGravityPosition()
		{
			return new EarthGravityPosition
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				Depth = 0.0, 
			};
		}
		public static EarthGravitySample ConstructEarthGravitySample()
		{
			return new EarthGravitySample
			{
				Position = ConstructEarthGravityPosition(),
				Gravity = ConstructEarthGravityVector(),
			};
		}
		public static EarthGravityValidationError ConstructEarthGravityValidationError()
		{
			return new EarthGravityValidationError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static EarthGravityValidationProblem ConstructEarthGravityValidationProblem()
		{
			return new EarthGravityValidationProblem
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<EarthGravityValidationError>
					{
						ConstructEarthGravityValidationError(),
					},
			};
		}
		public static EarthGravityVector ConstructEarthGravityVector()
		{
			return new EarthGravityVector
			{
				North = 0.0, 
				East = 0.0, 
				Down = 0.0, 
				Magnitude = 0.0, 
				TotalPotential = 0.0, 
			};
		}
		public static UsageStatisticsEarthGravity ConstructUsageStatisticsEarthGravity()
		{
			return new UsageStatisticsEarthGravity
			{
				StartedAt = DateTimeOffset.UtcNow,
				Scope = "Default Scope",
				RestEvaluations = 0, 
				MCPEvaluations = 0, 
				FailedEvaluations = 0, 
				PositionsEvaluated = 0, 
				ModelInfoRequests = 0, 
				StatisticsRequests = 0, 
			};
		}
		public static EarthVerticalDatumModelInfo ConstructEarthVerticalDatumModelInfo()
		{
			return new EarthVerticalDatumModelInfo
			{
				Name = "Default Name",
				ID = "Default ID",
				Description = "Default Description",
				DataDateTime = DateTimeOffset.UtcNow,
				GridResolutionMinutes = 0.0, 
				Interpolation = "Default Interpolation",
				MaximumInterpolationError = 0.0, 
				RMSInterpolationError = 0.0, 
				GeographicLibVersion = "Default GeographicLibVersion",
				ReferenceEllipsoid = "Default ReferenceEllipsoid",
				SupportedVerticalDatums = new List<string>
					{
						"",
					},
				SupportedConversionDirections = new List<string>
					{
						"",
					},
				DepthPositiveDirection = "Default DepthPositiveDirection",
				IsThreadSafe = false, 
				CoefficientSHA256 = "Default CoefficientSHA256",
			};
		}
		public static EarthVerticalDatumPosition ConstructEarthVerticalDatumPosition()
		{
			return new EarthVerticalDatumPosition
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				MeanSeaLevelDepth = 0.0, 
			};
		}
		public static EarthVerticalDatumSample ConstructEarthVerticalDatumSample()
		{
			return new EarthVerticalDatumSample
			{
				Position = ConstructEarthVerticalDatumPosition(),
				Wgs84EllipsoidalDepth = 0.0, 
				GeoidUndulation = 0.0, 
			};
		}
		public static EarthVerticalDatumValidationError ConstructEarthVerticalDatumValidationError()
		{
			return new EarthVerticalDatumValidationError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static EarthVerticalDatumValidationProblem ConstructEarthVerticalDatumValidationProblem()
		{
			return new EarthVerticalDatumValidationProblem
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<EarthVerticalDatumValidationError>
					{
						ConstructEarthVerticalDatumValidationError(),
					},
			};
		}
		public static MeanSeaLevelToWgs84Request ConstructMeanSeaLevelToWgs84Request()
		{
			return new MeanSeaLevelToWgs84Request
			{
				Positions = new List<EarthVerticalDatumPosition>
					{
						ConstructEarthVerticalDatumPosition(),
					},
			};
		}
		public static MeanSeaLevelToWgs84Response ConstructMeanSeaLevelToWgs84Response()
		{
			return new MeanSeaLevelToWgs84Response
			{
				Model = ConstructEarthVerticalDatumModelInfo(),
				Samples = new List<EarthVerticalDatumSample>
					{
						ConstructEarthVerticalDatumSample(),
					},
			};
		}
		public static UsageStatisticsEarthVerticalDatum ConstructUsageStatisticsEarthVerticalDatum()
		{
			return new UsageStatisticsEarthVerticalDatum
			{
				StartedAt = DateTimeOffset.UtcNow,
				Scope = "Default Scope",
				RestConversions = 0, 
				MCPConversions = 0, 
				FailedConversions = 0, 
				PositionsConverted = 0, 
				ModelInfoRequests = 0, 
				StatisticsRequests = 0, 
			};
		}
		public static Wgs84ToMeanSeaLevelPosition ConstructWgs84ToMeanSeaLevelPosition()
		{
			return new Wgs84ToMeanSeaLevelPosition
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				Wgs84EllipsoidalDepth = 0.0, 
			};
		}
		public static Wgs84ToMeanSeaLevelRequest ConstructWgs84ToMeanSeaLevelRequest()
		{
			return new Wgs84ToMeanSeaLevelRequest
			{
				Positions = new List<Wgs84ToMeanSeaLevelPosition>
					{
						ConstructWgs84ToMeanSeaLevelPosition(),
					},
			};
		}
		public static Wgs84ToMeanSeaLevelResponse ConstructWgs84ToMeanSeaLevelResponse()
		{
			return new Wgs84ToMeanSeaLevelResponse
			{
				Model = ConstructEarthVerticalDatumModelInfo(),
				Samples = new List<Wgs84ToMeanSeaLevelSample>
					{
						ConstructWgs84ToMeanSeaLevelSample(),
					},
			};
		}
		public static Wgs84ToMeanSeaLevelSample ConstructWgs84ToMeanSeaLevelSample()
		{
			return new Wgs84ToMeanSeaLevelSample
			{
				Position = ConstructWgs84ToMeanSeaLevelPosition(),
				MeanSeaLevelDepth = 0.0, 
				GeoidUndulation = 0.0, 
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
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				ReferenceEllipsoidId = new Guid(),
				Identifier = ConstructAuthorityIdentifier(),
				IsBuiltIn = false, 
				IsDefault = false, 
				Provenance = ConstructCatalogProvenance(),
				CreatedUtc = DateTimeOffset.UtcNow,
				ModifiedUtc = DateTimeOffset.UtcNow,
				Aliases = new List<string>
					{
						"",
					},
				ReferenceObjectType = (GeodeticReferenceObjectType)0,
				IsDeprecated = false, 
				IsSuperseded = false, 
				PrimeMeridianName = "Default PrimeMeridianName",
				PrimeMeridianIdentifier = ConstructAuthorityIdentifier(),
				PrimeMeridianLongitude = 0.0, 
				Origin = "Default Origin",
				PublicationDate = "Default PublicationDate",
				RealizationEpoch = "Default RealizationEpoch",
				FrameReferenceEpoch = null, 
				AnchorEpoch = null, 
				ConventionalReferenceSystem = "Default ConventionalReferenceSystem",
				RealizationMethod = "Default RealizationMethod",
				EnsembleAccuracy = null, 
				MemberDatumIds = new List<Guid>
					{
						new Guid(),
					},
				Usage = new List<GeodeticUsage>
					{
						ConstructGeodeticUsage(),
					},
				Remarks = "Default Remarks",
				InformationSource = "Default InformationSource",
				RevisionDate = DateTimeOffset.UtcNow,
				CatalogStatus = (CatalogEntryStatus)0,
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
		public static Accumulator ConstructAccumulator()
		{
			return new Accumulator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				AccumulatorClass = (AccumulatorClass?)0,
				Capacity = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
			};
		}
		public static AutoDriller ConstructAutoDriller()
		{
			return new AutoDriller
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				ControlMode = (AutodrillerControlMode?)0,
				MaxLimitRop = null, 
				MinLimitRop = null, 
				MaxLimitWob = null, 
				MinLimitWob = null, 
				MaxLimitDifferentialPressure = null, 
				MinLimitDifferentialPressure = null, 
				MaxLimitTrq = null, 
				MinLimitTrq = null, 
			};
		}
		public static AuxSolidsControl ConstructAuxSolidsControl()
		{
			return new AuxSolidsControl
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				SolidsControlClass = (SolidsControlClass?)0,
			};
		}
		public static BopLineDefinition ConstructBopLineDefinition()
		{
			return new BopLineDefinition
			{
				BopLinesClass = (BopLineClass?)0,
				LineOd = null, 
				LineId = null, 
				Length = null, 
			};
		}
		public static BopStack ConstructBopStack()
		{
			return new BopStack
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				BopStackClass = (BopStackClass?)0,
				UnitReferences = new List<string>
					{
						"",
					},
				BopControlType = (ControllerType?)0,
				BoreDiameter = null, 
				Height = null, 
				Weight = null, 
				BopComponents = new List<BopStackComponentDefinition>
					{
						ConstructBopStackComponentDefinition(),
					},
				BopLines = new List<BopLineDefinition>
					{
						ConstructBopLineDefinition(),
					},
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MinLimitOperatingPressure = null, 
				BopLineMaxLimitDesignPressure = null, 
				BopLineMaxLimitOperatingPressure = null, 
			};
		}
		public static BopStackComponentDefinition ConstructBopStackComponentDefinition()
		{
			return new BopStackComponentDefinition
			{
				BopStackComponentClass = (BopComponentClass?)0,
				BoreDiameter = null, 
				Height = null, 
			};
		}
		public static CasingDriveSystem ConstructCasingDriveSystem()
		{
			return new CasingDriveSystem
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				CsgDrvClass = (CasingDriveClass?)0,
				HoistingCapacity = null, 
				Length = null, 
				MaxLimitDesignTorque = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitDesignRotationSpeed = null, 
				MaxLimitTorque = null, 
				MaxLimitPressure = null, 
				MaxLimitRotationSpeed = null, 
				MaxLimitPushDown = null, 
			};
		}
		public static CasingRunningTool ConstructCasingRunningTool()
		{
			return new CasingRunningTool
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static CasingTongs ConstructCasingTongs()
		{
			return new CasingTongs
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static CatWalk ConstructCatWalk()
		{
			return new CatWalk
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static CementPump ConstructCementPump()
		{
			return new CementPump
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				PumpClass = (PumpClass?)0,
				PlungerDiameter = null, 
				StrokeLength = null, 
				CementPumpDisplacement = new List<CementPumpDisplacementPoint>
					{
						ConstructCementPumpDisplacementPoint(),
					},
				MaxLimitPressure = null, 
				MaxLimitFlowRate = null, 
			};
		}
		public static CementPumpDisplacementPoint ConstructCementPumpDisplacementPoint()
		{
			return new CementPumpDisplacementPoint
			{
				StrokeRate = null, 
				FlowRate = null, 
				Pressure = null, 
			};
		}
		public static CementUnit ConstructCementUnit()
		{
			return new CementUnit
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				Mounting = (MountingType?)0,
				Capabilities = new List<string>
					{
						"",
					},
				NumberOfPumps = null, 
			};
		}
		public static Centrifuge ConstructCentrifuge()
		{
			return new Centrifuge
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static ChokeCvCurvePoint ConstructChokeCvCurvePoint()
		{
			return new ChokeCvCurvePoint
			{
				Pressure = null, 
				Flow = null, 
			};
		}
		public static ChokeManifold ConstructChokeManifold()
		{
			return new ChokeManifold
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				ChokeControlClass = (ControlClass?)0,
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MinLimitOperatingPressure = null, 
				MaxLimitTestPressure = null, 
				MaxLimitOperatingTemperature = null, 
				MinLimitOperatingTemperature = null, 
			};
		}
		public static CoilDriveSystem ConstructCoilDriveSystem()
		{
			return new CoilDriveSystem
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				CoilDrvClass = (MountingType?)0,
				ReelPayloadCapacity = null, 
				ReelPayloadLength = null, 
				InjectorHeadRadius = null, 
				InjectorHeadMinTubingOd = null, 
				InjHeadDesignPullCapacity = null, 
				InjHeadDesignSnubCapacity = null, 
				InjHeadPullCapacity = null, 
				InjHeadSnubCapacity = null, 
				InjHeadMaxSpeed = null, 
			};
		}
		public static ContinuousCirculationDevice ConstructContinuousCirculationDevice()
		{
			return new ContinuousCirculationDevice
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				CcdControlClass = (ControlClass?)0,
				WorkingPumpPressure = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MaxLimitFlowrate = null, 
				MaxLimitBackflow = null, 
				MaxLimitFluidTemperature = null, 
				MinLimitFluidTemperature = null, 
				MaxLimitMudWeight = null, 
				MaxLimitRotationRate = null, 
			};
		}
		public static CrownBlock ConstructCrownBlock()
		{
			return new CrownBlock
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				SheaveDiameter = null, 
				GrooveDiameter = null, 
				NumberOfSheaves = null, 
				MaxLimitDesignLoad = null, 
				MaxLimitOperatingLoad = null, 
				MaxLimitCompensatorStroke = null, 
			};
		}
		public static CuttingsDryer ConstructCuttingsDryer()
		{
			return new CuttingsDryer
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static CuttingsTransportSystem ConstructCuttingsTransportSystem()
		{
			return new CuttingsTransportSystem
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static Degasser ConstructDegasser()
		{
			return new Degasser
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static Derrick ConstructDerrick()
		{
			return new Derrick
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				DerrickClass = (DerrickClass?)0,
				Height = null, 
				MaxLimitJointsPerStand = null, 
				MaxLimitDesignLoad = null, 
				MaxLimitOperatingLoad = null, 
				MaxLimitWindSpeed = null, 
			};
		}
		public static Desander ConstructDesander()
		{
			return new Desander
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static Desilter ConstructDesilter()
		{
			return new Desilter
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static Drawworks ConstructDrawworks()
		{
			return new Drawworks
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				DrawworksClass = (DrawworksClass?)0,
				MaxLimitDesignLoad = null, 
				MaxLimitOperatingLoad = null, 
				MaxLimitContinuousDrumPower = null, 
				MaxLimitContinuousDrumTorque = null, 
			};
		}
		public static DrillLine ConstructDrillLine()
		{
			return new DrillLine
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				Number = null, 
				Diameter = null, 
				LinearWeight = null, 
				MaxLimitDesignBreakingLoad = null, 
				MaxLimitOperatingBreakingLoad = null, 
			};
		}
		public static DrillingChokeManifold ConstructDrillingChokeManifold()
		{
			return new DrillingChokeManifold
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				ManifoldType = (ManifoldClass?)0,
				TrimSize = null, 
				FlowMeter = "Default FlowMeter",
				FlowMeterSize = null, 
				FlowMeterPressureRating = null, 
				JunkBasket = null, 
				ChokeCount = null, 
				FlowMeterCount = null, 
				PressureSensorVotingNumber = null, 
				ChokeNumber = (ChokeNumber?)0,
				ChokeFunction = (ChokeFunction?)0,
				ChokeCvCurves = new List<ChokeCvCurvePoint>
					{
						ConstructChokeCvCurvePoint(),
					},
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MaxLimitOperatingTemperature = null, 
				MinLimitOperatingTemperature = null, 
				MaxLimitOpeningSpeed = null, 
				MaxLimitBackPressure = null, 
				MinLimitFlowrate = null, 
				MaxLimitFlowrate = null, 
			};
		}
		public static DrillingFluidTypeDescriptor ConstructDrillingFluidTypeDescriptor()
		{
			return new DrillingFluidTypeDescriptor
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				DrillingFluidClass = (DrillingFluidClass?)0,
				DrillingFluidType = (DrillingFluidType?)0,
			};
		}
		public static DrillingMarineRiser ConstructDrillingMarineRiser()
		{
			return new DrillingMarineRiser
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				RiserClass = (RiserClass?)0,
				JointWeight = null, 
				RiserInsideDiameter = null, 
				RiserOuterDiameter = null, 
				RiserJointLength = null, 
				RiserTotalLength = null, 
				MaxLimitTensionLoad = null, 
				MaxLimitOpTensionLoad = null, 
				MaxLimitDesignKillPressure = null, 
				MaxLimitOpKillPressure = null, 
				MaxLimitDesignBoosterPressure = null, 
				MaxLimitBoosterPressure = null, 
				MaxLimitOpTemperature = null, 
				MinLimitOpTemperature = null, 
				MaxLimitAngleRiser = null, 
			};
		}
		public static DrillstringHeaveCompensator ConstructDrillstringHeaveCompensator()
		{
			return new DrillstringHeaveCompensator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				HeaveCompClass = (HeaveCompensatorClass?)0,
				CompensatorCapacity = null, 
				MaxLimitCompensatorStroke = null, 
			};
		}
		public static DriveMode ConstructDriveMode()
		{
			return new DriveMode
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				DriveModeClass = (DriveModeClass?)0,
			};
		}
		public static FloatValve ConstructFloatValve()
		{
			return new FloatValve
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				FloatValveClass = (FloatValveClass?)0,
				Diameter = null, 
				Length = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
			};
		}
		public static FlowRoutingManifold ConstructFlowRoutingManifold()
		{
			return new FlowRoutingManifold
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				ManifoldType = (ManifoldClass?)0,
				FlangeSize = null, 
				ReliefLineDiameter = null, 
				EqualizationLineDiameter = null, 
				PressureReliefValveTrim = null, 
				ManifoldFlowPath = (ManifoldFlowPath?)0,
				ManifoldFlowcurves = new List<RoutingManifoldCurvePoint>
					{
						ConstructRoutingManifoldCurvePoint(),
					},
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MaxLimitOperatingTemperature = null, 
				MinLimitOperatingTemperature = null, 
				MaxLimitFlowrate = null, 
			};
		}
		public static FlowSensor ConstructFlowSensor()
		{
			return new FlowSensor
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				FlowTransducer = (FlowSensorType?)0,
				FlowOutOfBorehole = null, 
			};
		}
		public static Generator ConstructGenerator()
		{
			return new Generator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				GeneratorClass = (GeneratorClass?)0,
				Speed = null, 
				Power = null, 
				Voltage = null, 
				PowerFactor = null, 
				SpeedMode = (SpeedMode?)0,
				EngineModel = (EngineModelType?)0,
				PowerplantGeneratorNumber = null, 
				PowerplantTotalPower = null, 
				StartupTimeCold = null, 
				StartupTimeWarm = null, 
				CoolingMedium = (GeneratorCooling?)0,
				Phases = (GeneratorPhases?)0,
				MaxLimitPower = null, 
				MaxLimitPowerIncrease = null, 
				MaxLimitSpeedIncrease = null, 
				MaxLimitSpeed = null, 
				MaxLimitVoltage = null, 
				MinLimitVoltage = null, 
				MaxLimitFrequency = null, 
				MinLimitFrequency = null, 
			};
		}
		public static HoistingSystem ConstructHoistingSystem()
		{
			return new HoistingSystem
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				HoistingSystemType = (HoistingSystemType?)0,
				Drawworks = ConstructDrawworks(),
				CrownBlock = ConstructCrownBlock(),
				TravellingBlock = ConstructTravellingBlock(),
				DrillLine = ConstructDrillLine(),
			};
		}
		public static IronRoughneck ConstructIronRoughneck()
		{
			return new IronRoughneck
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static Kelly ConstructKelly()
		{
			return new Kelly
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				KellyClass = (KellyClass?)0,
				KellyJointLength = null, 
				MaxLimitDesignRotationSpeed = null, 
				MaxLimitDesignTorque = null, 
				MaxLimitIbopPressure = null, 
				MaxLimitRotationSpeed = null, 
				MaxLimitTorque = null, 
			};
		}
		public static MarineMpdEquipment ConstructMarineMpdEquipment()
		{
			return new MarineMpdEquipment
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				MarineMpdClass = (MarineMpdClass?)0,
				Length = null, 
				Weight = null, 
				ThroughBoreDiameter = null, 
				ControlMeans = (ControllerType?)0,
				ContainsFlowSpool = null, 
				ContainsNonRotatingDevice = null, 
				ContainsDrillstringIsolation = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitDynamicPressure = null, 
				MaxLimitRotatingSpeed = null, 
			};
		}
		public static MeasurementAfm ConstructMeasurementAfm()
		{
			return new MeasurementAfm
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				UpdateRate = null, 
			};
		}
		public static MpdControlDevice ConstructMpdControlDevice()
		{
			return new MpdControlDevice
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				MpdControlDeviceClass = (MpdControlDeviceClass?)0,
				NominalSize = null, 
				ThroughBoreDiameter = null, 
				SealingElementMaterial = "Default SealingElementMaterial",
				ControlDeviceHeight = null, 
				MaxLimitStaticPressure = null, 
				MaxLimitDynamicPressure = null, 
				MaxLimitRotatingSpeed = null, 
				MaxLimitActivationPressure = null, 
			};
		}
		public static MpdController ConstructMpdController()
		{
			return new MpdController
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				MpdGradientMode = (MpdGradientMode?)0,
				PrimaryChokeTrim = null, 
				SecondaryChokeTrim = null, 
				MaxLimitPressure = null, 
				MinLimitMudPumpFlowrate = null, 
			};
		}
		public static MudGasSeparator ConstructMudGasSeparator()
		{
			return new MudGasSeparator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static MudPump ConstructMudPump()
		{
			return new MudPump
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				Type = (MudPumpType)0,
				PumpClass = (PumpClass?)0,
				PumpAction = null, 
				PumpEfficiency = null, 
				Stroke = null, 
				LinerConfigurations = new List<MudPumpLinerConfiguration>
					{
						ConstructMudPumpLinerConfiguration(),
					},
				PulsationDamperPressure = null, 
				PulsationDamperVolume = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPower = null, 
				MaxLimitOperatingSpeed = null, 
			};
		}
		public static MudTank ConstructMudTank()
		{
			return new MudTank
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				TankClass = (TankClass?)0,
				TankFluidType = (TankFluidType?)0,
				MaxLimitOperatingVolume = null, 
			};
		}
		public static MultiPhaseSeparator ConstructMultiPhaseSeparator()
		{
			return new MultiPhaseSeparator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				SeparatorClass = (SeparatorPhaseClass?)0,
				MaximumOperatingPressure = null, 
				MaximumOperatingFlowrate = null, 
				SeparationEfficiency = null, 
				SeparatorMedium = (SeparatorMedium?)0,
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MaxLimitFlowrate = null, 
				MaxLimitOperatingTemperature = null, 
				MinLimitOperatingTemperature = null, 
			};
		}
		public static PipeDeck ConstructPipeDeck()
		{
			return new PipeDeck
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static PipeRack ConstructPipeRack()
		{
			return new PipeRack
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static ReturnFlowLine ConstructReturnFlowLine()
		{
			return new ReturnFlowLine
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static RheometerAfmMeasurement ConstructRheometerAfmMeasurement()
		{
			return new RheometerAfmMeasurement
			{
				AfmViscShearRate = null, 
				AfmViscShearStress = null, 
			};
		}
		public static Rig ConstructRig()
		{
			return new Rig
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				Identification = ConstructRigIdentification(),
				RigType = (RigType?)0,
				OperatingEnvironment = (RigEnvironment?)0,
				MobilityType = (RigMobilityType?)0,
				OperatingEnvelope = ConstructRigOperatingEnvelope(),
				MarineUnitProfile = ConstructMarineUnitProfile(),
				JackUpProfile = ConstructJackUpProfile(),
				StationKeepingSystem = ConstructStationKeepingSystem(),
				StorageCapacities = new List<RigStorageCapacity>
					{
						ConstructRigStorageCapacity(),
					},
				FeatureAssignments = new List<RigFeatureAssignment>(),
				MudPumpList = new List<MudPump>
					{
						ConstructMudPump(),
					},
				CementPumpList = new List<CementPump>
					{
						ConstructCementPump(),
					},
				CementUnit = ConstructCementUnit(),
				DriveMode = ConstructDriveMode(),
				MainRigMast = ConstructRigMast(),
				AuxiliaryRigMast = ConstructRigMast(),
				MudTankList = new List<MudTank>
					{
						ConstructMudTank(),
					},
				GeneratorList = new List<Generator>
					{
						ConstructGenerator(),
					},
				ShaleShakerList = new List<ShaleShaker>
					{
						ConstructShaleShaker(),
					},
				AuxSolidsControl = ConstructAuxSolidsControl(),
				DrillingFluidType = ConstructDrillingFluidTypeDescriptor(),
				FlowSensor = ConstructFlowSensor(),
				MeasurementAfm = ConstructMeasurementAfm(),
				ReturnFlowLine = ConstructReturnFlowLine(),
				MudGasSeparatorList = new List<MudGasSeparator>
					{
						ConstructMudGasSeparator(),
					},
				DesanderList = new List<Desander>
					{
						ConstructDesander(),
					},
				DesilterList = new List<Desilter>
					{
						ConstructDesilter(),
					},
				CentrifugeList = new List<Centrifuge>
					{
						ConstructCentrifuge(),
					},
				DegasserList = new List<Degasser>
					{
						ConstructDegasser(),
					},
				CuttingsTransportSystem = ConstructCuttingsTransportSystem(),
				CuttingsDryerList = new List<CuttingsDryer>
					{
						ConstructCuttingsDryer(),
					},
				PipeDeck = ConstructPipeDeck(),
				Accumulator = ConstructAccumulator(),
				BopStack = ConstructBopStack(),
				FloatValve = ConstructFloatValve(),
				AutoDriller = ConstructAutoDriller(),
				MpdController = ConstructMpdController(),
				MpdControlDevice = ConstructMpdControlDevice(),
				ContinuousCirculationDevice = ConstructContinuousCirculationDevice(),
				DrillingChokeManifold = ConstructDrillingChokeManifold(),
				SurfaceMpdEquipment = ConstructSurfaceMpdEquipment(),
				MarineMpdEquipment = ConstructMarineMpdEquipment(),
				MultiPhaseSeparator = ConstructMultiPhaseSeparator(),
				FlowRoutingManifold = ConstructFlowRoutingManifold(),
				DrillstringHeaveCompensator = ConstructDrillstringHeaveCompensator(),
				DrillingMarineRiser = ConstructDrillingMarineRiser(),
				RiserHeaveCompensator = ConstructRiserHeaveCompensator(),
				DrillFloorElevation = null, 
				IsFixedPlatform = false, 
				ClusterID = null, 
			};
		}
		public static RigChoke ConstructRigChoke()
		{
			return new RigChoke
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static RigMast ConstructRigMast()
		{
			return new RigMast
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				HoistingSystem = ConstructHoistingSystem(),
				CatWalk = ConstructCatWalk(),
				PipeRack = ConstructPipeRack(),
				CasingDriveSystem = ConstructCasingDriveSystem(),
				CoilDriveSystem = ConstructCoilDriveSystem(),
				Derrick = ConstructDerrick(),
				TorqueTurnSub = ConstructTorqueTurnSub(),
				RotaryTable = ConstructRotaryTable(),
				TopDrive = ConstructTopDrive(),
				Kelly = ConstructKelly(),
				IronRoughneck = ConstructIronRoughneck(),
				CasingTongs = ConstructCasingTongs(),
				CasingRunningTool = ConstructCasingRunningTool(),
				StandPipe = ConstructStandPipe(),
				StandPipeManifold = ConstructStandPipeManifold(),
				RotaryHose = ConstructRotaryHose(),
				ChokeManifold = ConstructChokeManifold(),
				RigChokeList = new List<RigChoke>
					{
						ConstructRigChoke(),
					},
				Slips = ConstructSlips(),
			};
		}
		public static RiserHeaveCompensator ConstructRiserHeaveCompensator()
		{
			return new RiserHeaveCompensator
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				RiserCompensatorClass = (RiserCompensatorClass?)0,
				CompensatorCapacity = null, 
				MaxLimitCompensatorStroke = null, 
			};
		}
		public static RotaryHose ConstructRotaryHose()
		{
			return new RotaryHose
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static RotaryTable ConstructRotaryTable()
		{
			return new RotaryTable
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				RotaryTableType = (RotaryTableType?)0,
				TableOpeningDiameter = null, 
				BushingType = (RotaryTableBushingType?)0,
				BushingSize = null, 
				Height = null, 
				Mass = null, 
				MaxLimitOperatingSpeed = null, 
				MaxLimitDesignSpeed = null, 
				MaxLimitOperatingTorque = null, 
				MaxLimitDesignTorque = null, 
				MaxLimitOperatingStringWeight = null, 
				MaxLimitDesignStringWeight = null, 
				MaxLimitPower = null, 
				MaxLimitTemperature = null, 
			};
		}
		public static RoutingManifoldCurvePoint ConstructRoutingManifoldCurvePoint()
		{
			return new RoutingManifoldCurvePoint
			{
				Pressure = null, 
				Flow = null, 
			};
		}
		public static ShakerScreenDefinition ConstructShakerScreenDefinition()
		{
			return new ShakerScreenDefinition
			{
				ScreenDeck = null, 
				MeshSize = "Default MeshSize",
			};
		}
		public static ShaleShaker ConstructShaleShaker()
		{
			return new ShaleShaker
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				ShakerClass = (ShakerClass?)0,
				ShakerScreens = new List<ShakerScreenDefinition>
					{
						ConstructShakerScreenDefinition(),
					},
				MaxLimitOperatingCapacity = null, 
			};
		}
		public static Slips ConstructSlips()
		{
			return new Slips
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static StandPipe ConstructStandPipe()
		{
			return new StandPipe
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				PressureMeasurementElevation = null, 
				MudHoseHangingPointElevation = null, 
			};
		}
		public static StandPipeManifold ConstructStandPipeManifold()
		{
			return new StandPipeManifold
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				PipeDiameter = null, 
				StandpipeSpecLevel = (StandpipeSpecLevel?)0,
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MaxLimitOperatingTemperature = null, 
				MinLimitOperatingTemperature = null, 
			};
		}
		public static SurfaceMpdEquipment ConstructSurfaceMpdEquipment()
		{
			return new SurfaceMpdEquipment
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				SurfaceMpdClass = (SurfaceMpdClass?)0,
				MinimumBoreholeSize = null, 
				MaximumBoreholeSize = null, 
				PressureAccuracy = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitOperatingPressure = null, 
				MinLimitOperatingPressure = null, 
				MaxLimitFlowrate = null, 
				MaxLimitMudWeight = null, 
				MaxLimitPressure = null, 
				MinLimitMudPumpFlowrate = null, 
			};
		}
		public static TopDrive ConstructTopDrive()
		{
			return new TopDrive
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				TopDriveClass = (TopDriveClass?)0,
				TopDriveControllerType = (TopDriveControllerType?)0,
				Orientable = null, 
				Weight = null, 
				MaxLimitIbopPressure = null, 
				MaxLimitRotationSpeed = null, 
				MaxLimitDesignLoad = null, 
				MaxLimitDesignTorque = null, 
				MaxLimitOperatingLoad = null, 
				MaxLimitOperatingTorque = null, 
				MaxLimitMakeupTorque = null, 
				MaxLimitBreakoutTorque = null, 
				RatedPower = null, 
				RatedHoistingCapacity = null, 
				RatedContinuousTorque = null, 
				RatedIntermittentTorque = null, 
				MotorCount = null, 
				MotorType = "Default MotorType",
				IbopConfiguration = "Default IbopConfiguration",
				AutomationSystemCompatibility = "Default AutomationSystemCompatibility",
				ProportionalGain = null, 
				IntegralGain = null, 
				TuningFrequency = null, 
				VFDFilterTimeConstant = null, 
				EncoderTimeConstant = null, 
				AccelerationFilterTimeConstant = null, 
				TorqueHighPassFilterTimeConstant = null, 
				TorqueLowPassFilterTimeConstant = null, 
				TuningFactor = null, 
				InertiaCorrectionFactor = null, 
			};
		}
		public static TorqueTurnSub ConstructTorqueTurnSub()
		{
			return new TorqueTurnSub
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				Length = null, 
				OutsideDiameter = null, 
				InsideDiameter = null, 
				Weight = null, 
				BatteryLife = null, 
				MaxLimitDesignLoad = null, 
				MaxLimitDesignTorque = null, 
				MaxLimitDesignPressure = null, 
				MaxLimitLoad = null, 
				MaxLimitTorque = null, 
				MaxLimitPressure = null, 
				MaxLimitTemperature = null, 
				MinLimitTemperature = null, 
			};
		}
		public static TravellingBlock ConstructTravellingBlock()
		{
			return new TravellingBlock
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				AssetTag = "Default AssetTag",
				InstallationDate = DateTimeOffset.UtcNow,
				CommissioningDate = DateTimeOffset.UtcNow,
				LifecycleStatus = (EquipmentLifecycleStatus?)0,
				CertificationReferences = new List<string>
					{
						"",
					},
				MeasurementCapabilities = new List<EquipmentMeasurementCapability>
					{
						ConstructEquipmentMeasurementCapability(),
					},
				Weight = null, 
				NumberOfSheaves = null, 
				GrooveDiameter = null, 
				MaxLimitBlockTravel = null, 
				MaxLimitDesignLoad = null, 
				MaxLimitOperatingLoad = null, 
			};
		}
		public static UsageStatisticsRig ConstructUsageStatisticsRig()
		{
			return new UsageStatisticsRig
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllRigIdPerDay = ConstructHistory(),
				GetAllRigMetaInfoPerDay = ConstructHistory(),
				GetRigByIdPerDay = ConstructHistory(),
				GetAllRigLightPerDay = ConstructHistory(),
				GetAllRigPerDay = ConstructHistory(),
				PostRigPerDay = ConstructHistory(),
				PutRigByIdPerDay = ConstructHistory(),
				DeleteRigByIdPerDay = ConstructHistory(),
				BatchExportRigsPerDay = ConstructHistory(),
				BatchRestoreRigsPerDay = ConstructHistory(),
			};
		}
		public static VerticalDatum ConstructVerticalDatum()
		{
			return new VerticalDatum
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				DatumSet = new List<VerticalDatumSet>
					{
						ConstructVerticalDatumSet(),
					},
				ConversionFrom = (VerticalDatumConversion)0,
				Type = (VerticalDatumType)0,
			};
		}
		public static VerticalDatumOrder ConstructVerticalDatumOrder()
		{
			return new VerticalDatumOrder
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				VerticalDatum = ConstructVerticalDatum(),
			};
		}
		public static VerticalDatumSet ConstructVerticalDatumSet()
		{
			return new VerticalDatumSet
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				VerticalDatumWGS64 = null, 
				GenericVerticalDatum = 0.0, 
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
				GetAllWellBySlotIdPerDay = ConstructHistory(),
				GetAllWellByClusterIdPerDay = ConstructHistory(),
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
				WellIdentityAssignments = new List<WellIdentityAssignment>(),
				WellFeatureAssignments = new List<WellFeatureAssignment>(),
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
				GetAllWellBoreByWellIDPerDay = ConstructHistory(),
				GetAllWellBoreByRigIDPerDay = ConstructHistory(),
				GetAllWellBoreByParentIDPerDay = ConstructHistory(),
				GetAllSidetrackedWellBorePerDay = ConstructHistory(),
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
		public static BoreHoleSize ConstructBoreHoleSize()
		{
			return new BoreHoleSize
			{
				HoleSize = ConstructGaussianDrillingProperty(),
				Length = ConstructGaussianDrillingProperty(),
			};
		}
		public static CasingSection ConstructCasingSection()
		{
			return new CasingSection
			{
				TopDepth = ConstructGaussianDrillingProperty(),
				Length = ConstructGaussianDrillingProperty(),
				TopCementDepth = ConstructGaussianDrillingProperty(),
				CasingSectionElements = new List<CasingSectionElement>
					{
						ConstructCasingSectionElement(),
					},
				CasingSectionSizeTable = new List<BoreHoleSize>
					{
						ConstructBoreHoleSize(),
					},
				OpenHoleSection = ConstructOpenHoleSection(),
			};
		}
		public static CasingSectionElement ConstructCasingSectionElement()
		{
			return new CasingSectionElement
			{
				BodyOD = ConstructGaussianDrillingProperty(),
				BodyID = ConstructGaussianDrillingProperty(),
				CollarOD = ConstructGaussianDrillingProperty(),
				JointLength = ConstructGaussianDrillingProperty(),
				SectionLength = ConstructGaussianDrillingProperty(),
				MaxDLS = ConstructScalarDrillingProperty(),
				ConnectionType = "Default ConnectionType",
				Grade = "Default Grade",
				MaterialDensity = ConstructGaussianDrillingProperty(),
				YoungModulus = ConstructGaussianDrillingProperty(),
				LinearWeight = ConstructGaussianDrillingProperty(),
				TensileStrength = ConstructGaussianDrillingProperty(),
				TorsionalStrength = ConstructGaussianDrillingProperty(),
				BurstPressure = ConstructGaussianDrillingProperty(),
				CollapsePressure = ConstructGaussianDrillingProperty(),
				YieldStress = ConstructGaussianDrillingProperty(),
				MakeUpTorqueRecommended = ConstructScalarDrillingProperty(),
			};
		}
		public static ElementConnectivity ConstructElementConnectivity()
		{
			return new ElementConnectivity
			{
				UpstreamElement = ConstructSideElement(),
				DownstreamElement = ConstructSideElement(),
			};
		}
		public static OpenHoleSection ConstructOpenHoleSection()
		{
			return new OpenHoleSection
			{
				HoleSizes = new List<BoreHoleSize>
					{
						ConstructBoreHoleSize(),
					},
			};
		}
		public static SideConnector ConstructSideConnector()
		{
			return new SideConnector
			{
				Position = ConstructGaussianDrillingProperty(),
				VerticalDepth = ConstructGaussianDrillingProperty(),
				FirstSideElement = ConstructSideElement(),
				ElementConnectivities = new List<ElementConnectivity>
					{
						ConstructElementConnectivity(),
					},
			};
		}
		public static SideElement ConstructSideElement()
		{
			return new SideElement
			{
				Name = "Default Name",
				Type = (SideElementType)0,
				Length = ConstructGaussianDrillingProperty(),
				TopVerticalDepth = ConstructGaussianDrillingProperty(),
				OD = ConstructGaussianDrillingProperty(),
				ID = ConstructGaussianDrillingProperty(),
			};
		}
		public static SurfaceSection ConstructSurfaceSection()
		{
			return new SurfaceSection
			{
				Type = (SurfaceSectionType)0,
				SectionLength = ConstructGaussianDrillingProperty(),
				BodyOD = ConstructGaussianDrillingProperty(),
				BodyID = ConstructGaussianDrillingProperty(),
				ConnectionType = "Default ConnectionType",
				Grade = "Default Grade",
				MaterialDensity = ConstructGaussianDrillingProperty(),
				YoungModulus = ConstructGaussianDrillingProperty(),
				LinearWeight = ConstructGaussianDrillingProperty(),
				TensileStrength = ConstructGaussianDrillingProperty(),
				BurstPressure = ConstructGaussianDrillingProperty(),
				CollapsePressure = ConstructGaussianDrillingProperty(),
				YieldStress = ConstructGaussianDrillingProperty(),
				MakeUpTorqueRecommended = ConstructScalarDrillingProperty(),
				SideConnectors = new List<SideConnector>
					{
						ConstructSideConnector(),
					},
			};
		}
		public static UsageStatisticsWellBoreArchitecture ConstructUsageStatisticsWellBoreArchitecture()
		{
			return new UsageStatisticsWellBoreArchitecture
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllWellBoreArchitectureIdPerDay = ConstructHistory(),
				GetAllWellBoreArchitectureMetaInfoPerDay = ConstructHistory(),
				GetWellBoreArchitectureByIdPerDay = ConstructHistory(),
				GetAllWellBoreArchitectureLightPerDay = ConstructHistory(),
				GetAllWellBoreArchitecturePerDay = ConstructHistory(),
				PostWellBoreArchitecturePerDay = ConstructHistory(),
				PutWellBoreArchitectureByIdPerDay = ConstructHistory(),
				DeleteWellBoreArchitectureByIdPerDay = ConstructHistory(),
			};
		}
		public static WellBoreArchitecture ConstructWellBoreArchitecture()
		{
			return new WellBoreArchitecture
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				WellBoreID = null, 
				WellHead = ConstructWellHead(),
				FluidsAboveGroundLevel = new List<WellBoreArchitectureFluid>
					{
						ConstructWellBoreArchitectureFluid(),
					},
				SurfaceSections = new List<SurfaceSection>
					{
						ConstructSurfaceSection(),
					},
				CasingSections = new List<CasingSection>
					{
						ConstructCasingSection(),
					},
			};
		}
		public static WellBoreArchitectureBatchCatalogDependencies ConstructWellBoreArchitectureBatchCatalogDependencies()
		{
			return new WellBoreArchitectureBatchCatalogDependencies
			{
				Identities = new List<WellBoreArchitectureIdentity>
					{
						ConstructWellBoreArchitectureIdentity(),
					},
				FeatureCategories = new List<WellBoreArchitectureFeatureCategory>
					{
						ConstructWellBoreArchitectureFeatureCategory(),
					},
			};
		}
		public static WellBoreArchitectureBatchCatalogMapping ConstructWellBoreArchitectureBatchCatalogMapping()
		{
			return new WellBoreArchitectureBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static WellBoreArchitectureBatchError ConstructWellBoreArchitectureBatchError()
		{
			return new WellBoreArchitectureBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellBoreArchitectureBatchErrorEnvelope ConstructWellBoreArchitectureBatchErrorEnvelope()
		{
			return new WellBoreArchitectureBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<WellBoreArchitectureBatchError>
					{
						ConstructWellBoreArchitectureBatchError(),
					},
			};
		}
		public static WellBoreArchitectureBatchExportDocument ConstructWellBoreArchitectureBatchExportDocument()
		{
			return new WellBoreArchitectureBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructWellBoreArchitectureBatchCatalogDependencies(),
				WellBoreArchitectures = new List<WellBoreArchitecture>
					{
						ConstructWellBoreArchitecture(),
					},
			};
		}
		public static WellBoreArchitectureBatchExportRequest ConstructWellBoreArchitectureBatchExportRequest()
		{
			return new WellBoreArchitectureBatchExportRequest
			{
				Scope = (WellBoreArchitectureBatchExportScope)0,
				WellBoreArchitectureIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBoreArchitectureBatchRestoreRequest ConstructWellBoreArchitectureBatchRestoreRequest()
		{
			return new WellBoreArchitectureBatchRestoreRequest
			{
				ConflictPolicy = (WellBoreArchitectureBatchRestoreConflictPolicy)0,
				CatalogPolicy = (WellBoreArchitectureBatchCatalogRestorePolicy)0,
				AllowNormalizedNameMapping = false, 
				Document = ConstructWellBoreArchitectureBatchExportDocument(),
			};
		}
		public static WellBoreArchitectureBatchRestoreResponse ConstructWellBoreArchitectureBatchRestoreResponse()
		{
			return new WellBoreArchitectureBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<WellBoreArchitectureBatchCatalogMapping>
					{
						ConstructWellBoreArchitectureBatchCatalogMapping(),
					},
				WellBoreArchitectureIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBoreArchitectureExternalReferenceAuditRequest ConstructWellBoreArchitectureExternalReferenceAuditRequest()
		{
			return new WellBoreArchitectureExternalReferenceAuditRequest
			{
				Scope = (WellBoreArchitectureExternalReferenceAuditScope)0,
				WellBoreArchitectureIDs = new List<Guid>
					{
						new Guid(),
					},
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static WellBoreArchitectureExternalReferenceAuditResult ConstructWellBoreArchitectureExternalReferenceAuditResult()
		{
			return new WellBoreArchitectureExternalReferenceAuditResult
			{
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Total = 0, 
				Offset = 0, 
				Limit = 0, 
				ValidCount = 0, 
				InvalidCount = 0, 
				UnavailableCount = 0, 
				Items = new List<WellBoreArchitectureExternalReferenceValidation>
					{
						ConstructWellBoreArchitectureExternalReferenceValidation(),
					},
			};
		}
		public static WellBoreArchitectureExternalReferenceIssue ConstructWellBoreArchitectureExternalReferenceIssue()
		{
			return new WellBoreArchitectureExternalReferenceIssue
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellBoreArchitectureExternalReferenceValidation ConstructWellBoreArchitectureExternalReferenceValidation()
		{
			return new WellBoreArchitectureExternalReferenceValidation
			{
				WellBoreArchitectureID = new Guid(),
				WellBoreID = null, 
				WellBoreExists = null, 
				Status = (WellBoreArchitectureExternalReferenceValidationStatus)0,
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Issues = new List<WellBoreArchitectureExternalReferenceIssue>
					{
						ConstructWellBoreArchitectureExternalReferenceIssue(),
					},
			};
		}
		public static WellBoreArchitectureFeatureAssignment ConstructWellBoreArchitectureFeatureAssignment()
		{
			return new WellBoreArchitectureFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreArchitectureFeatureCategory ConstructWellBoreArchitectureFeatureCategory()
		{
			return new WellBoreArchitectureFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<WellBoreArchitectureFeatureOption>
					{
						ConstructWellBoreArchitectureFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreArchitectureFeatureOption ConstructWellBoreArchitectureFeatureOption()
		{
			return new WellBoreArchitectureFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static WellBoreArchitectureFluid ConstructWellBoreArchitectureFluid()
		{
			return new WellBoreArchitectureFluid
			{
				Fluid = (FluidType)0,
				Depth = ConstructGaussianDrillingProperty(),
			};
		}
		public static WellBoreArchitectureIdentity ConstructWellBoreArchitectureIdentity()
		{
			return new WellBoreArchitectureIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreArchitectureIdentityAssignment ConstructWellBoreArchitectureIdentityAssignment()
		{
			return new WellBoreArchitectureIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static WellHead ConstructWellHead()
		{
			return new WellHead
			{
				MaxOD = ConstructScalarDrillingProperty(),
				MinOD = ConstructScalarDrillingProperty(),
				Depth = ConstructGaussianDrillingProperty(),
				CasingHangerDepth = ConstructScalarDrillingProperty(),
				TubingHangerDepth = ConstructScalarDrillingProperty(),
			};
		}
		public static EarthMagneticData ConstructEarthMagneticData()
		{
			return new EarthMagneticData
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				Depth = 0.0, 
				Year = 0.0, 
				Dip = null, 
				FieldIntensity = null, 
				Declination = null, 
				HorizontalMagneticField = null, 
			};
		}
		public static EarthMagneticField ConstructEarthMagneticField()
		{
			return new EarthMagneticField
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				EarthMagneticFieldData = new List<EarthMagneticData>
					{
						ConstructEarthMagneticData(),
					},
				Type = (EarthMagneticFieldType)0,
			};
		}
		public static EarthMagneticFieldCalculationOrder ConstructEarthMagneticFieldCalculationOrder()
		{
			return new EarthMagneticFieldCalculationOrder
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				CalculationMethod = (EarthMagneticFieldCalculationMethod)0,
				RawEarthMagneticFieldTable = ConstructEarthMagneticField(),
				CompletedEarthMagneticFieldTable = ConstructEarthMagneticField(),
			};
		}
		public static GravitationalData ConstructGravitationalData()
		{
			return new GravitationalData
			{
				Lattitude = 0.0, 
				Longitude = 0.0, 
				Depth = 0.0, 
				GravitatyIntensityX = null, 
				GravitatyIntensityY = null, 
				GravitatyIntensityZ = null, 
			};
		}
		public static GravitationalField ConstructGravitationalField()
		{
			return new GravitationalField
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				Type = (GravitationalFieldType)0,
				GravitationalDataTable = new List<GravitationalData>
					{
						ConstructGravitationalData(),
					},
			};
		}
		public static GravitationalFieldCalculationOrder ConstructGravitationalFieldCalculationOrder()
		{
			return new GravitationalFieldCalculationOrder
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				RawGravitationalField = ConstructGravitationalField(),
				CompletedGravitationalField = ConstructGravitationalField(),
			};
		}
		public static UsageStatisticsSurveyInstrument ConstructUsageStatisticsSurveyInstrument()
		{
			return new UsageStatisticsSurveyInstrument
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllSurveyInstrumentIdPerDay = ConstructHistory(),
				GetAllSurveyInstrumentMetaInfoPerDay = ConstructHistory(),
				GetSurveyInstrumentByIdPerDay = ConstructHistory(),
				GetAllSurveyInstrumentLightPerDay = ConstructHistory(),
				GetAllSurveyInstrumentPerDay = ConstructHistory(),
				PostSurveyInstrumentPerDay = ConstructHistory(),
				PutSurveyInstrumentByIdPerDay = ConstructHistory(),
				DeleteSurveyInstrumentByIdPerDay = ConstructHistory(),
				GetAllErrorSourceIdPerDay = ConstructHistory(),
				GetAllErrorSourceMetaInfoPerDay = ConstructHistory(),
				GetErrorSourceByIdPerDay = ConstructHistory(),
				GetAllErrorSourcePerDay = ConstructHistory(),
				PostErrorSourcePerDay = ConstructHistory(),
				PutErrorSourceByIdPerDay = ConstructHistory(),
				DeleteErrorSourceByIdPerDay = ConstructHistory(),
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
		public static GlobalAntiCollision ConstructGlobalAntiCollision()
		{
			return new GlobalAntiCollision
			{
				ID = "Default ID",
				ConfidenceFactor = 0.0, 
				ReferenceWellPathID = new Guid(),
				ReferenceTrajectoryID = new Guid(),
				ComparisonTrajectoryIDs = new List<Guid>
					{
						new Guid(),
					},
				SeparationFactorResults = new List<SeparationFactorResult>
					{
						ConstructSeparationFactorResult(),
					},
			};
		}
		public static MeasuredDepthRange ConstructMeasuredDepthRange()
		{
			return new MeasuredDepthRange
			{
				StartMD = 0.0, 
				EndMD = 0.0, 
			};
		}
		public static SeparationFactorPoint ConstructSeparationFactorPoint()
		{
			return new SeparationFactorPoint
			{
				ReferenceMD = 0.0, 
				ComparisonMD = 0.0, 
				SeparationFactor = 0.0, 
			};
		}
		public static SeparationFactorResult ConstructSeparationFactorResult()
		{
			return new SeparationFactorResult
			{
				ComparisonTrajectoryID = new Guid(),
				ReferenceMDRange = ConstructMeasuredDepthRange(),
				ComparisonMDRange = ConstructMeasuredDepthRange(),
				SeparationFactorProfile = new List<SeparationFactorPoint>
					{
						ConstructSeparationFactorPoint(),
					},
			};
		}
		public static AnnotatedAbscissa ConstructAnnotatedAbscissa()
		{
			return new AnnotatedAbscissa
			{
				Abscissa = 0.0, 
				Annotation = "Default Annotation",
			};
		}
		public static InterpolatedTrajectory ConstructInterpolatedTrajectory()
		{
			return new InterpolatedTrajectory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				TrajectoryID = new Guid(),
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
				InterpolationStep = null, 
				InterpolationReferenceDepth = null, 
				MaximumChordArcDistance = null, 
				IncludeFirstSurvey = false, 
				IncludeLastSurvey = false, 
				InterpolateAtCasingAndLinerShoeDepths = false, 
				InterpolateAtLinerHangerDepths = false, 
				InterpolateAtCasingChangeOfDiameter = false, 
				AdditionalAbscissaList = new List<AnnotatedAbscissa>
					{
						ConstructAnnotatedAbscissa(),
					},
				InternalAdditionalAbscissaList = new List<AnnotatedAbscissa>
					{
						ConstructAnnotatedAbscissa(),
					},
			};
		}
		public static MinimumDistanceAdaptiveRefinementSettings ConstructMinimumDistanceAdaptiveRefinementSettings()
		{
			return new MinimumDistanceAdaptiveRefinementSettings
			{
				Enabled = false, 
				PolarDeviationTolerance = null, 
				PolarAngularTolerance = null, 
				MinimumMDStep = null, 
				MaximumDepth = 0, 
				MaximumExtraSamplesPerComparison = 0, 
			};
		}
		public static MinimumDistanceReferenceInterval ConstructMinimumDistanceReferenceInterval()
		{
			return new MinimumDistanceReferenceInterval
			{
				ID = new Guid(),
				Name = "Default Name",
				StartMD = null, 
				EndMD = null, 
			};
		}
		public static SurveyImportSettings ConstructSurveyImportSettings()
		{
			return new SurveyImportSettings
			{
				SelectedSurveyImportFormat = "Default SelectedSurveyImportFormat",
				SelectedSurveyImportSeparator = "Default SelectedSurveyImportSeparator",
				SelectedSurveyImportDecimalMarker = "Default SelectedSurveyImportDecimalMarker",
				SelectedSurveyImportMDUnit = "Default SelectedSurveyImportMDUnit",
				SelectedSurveyImportInclinationUnit = "Default SelectedSurveyImportInclinationUnit",
				SelectedSurveyImportAzimuthUnit = "Default SelectedSurveyImportAzimuthUnit",
				SurveyImportMDColumn = 0, 
				SurveyImportInclinationColumn = 0, 
				SurveyImportAzimuthColumn = 0, 
				SurveyImportMDStart = 0, 
				SurveyImportMDWidth = 0, 
				SurveyImportInclinationStart = 0, 
				SurveyImportInclinationWidth = 0, 
				SurveyImportAzimuthStart = 0, 
				SurveyImportAzimuthWidth = 0, 
			};
		}
		public static SurveyMeasurement ConstructSurveyMeasurement()
		{
			return new SurveyMeasurement
			{
				MD = null, 
				Inclination = null, 
				Azimuth = null, 
				Annotation = "Default Annotation",
			};
		}
		public static SurveyMeasurementChunk ConstructSurveyMeasurementChunk()
		{
			return new SurveyMeasurementChunk
			{
				SurveyRunID = new Guid(),
				ChunkIndex = 0, 
				MeasurementCount = 0, 
				StartMD = null, 
				EndMD = null, 
				SurveyMeasurementList = new List<SurveyMeasurement>
					{
						ConstructSurveyMeasurement(),
					},
			};
		}
		public static SurveyPointChunk ConstructSurveyPointChunk()
		{
			return new SurveyPointChunk
			{
				OwnerID = new Guid(),
				OwnerType = "Default OwnerType",
				ChunkIndex = 0, 
				PointCount = 0, 
				StartMD = null, 
				EndMD = null, 
				SurveyPointList = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
			};
		}
		public static SurveyRun ConstructSurveyRun()
		{
			return new SurveyRun
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				FieldID = null, 
				ClusterID = null, 
				WellID = null, 
				WellBoreID = new Guid(),
				SurveyInstrumentID = new Guid(),
				SurveyRunType = (SurveyRunType)0,
				CalculationType = (TrajectoryCalculationType)0,
				ParentSurveyRunID = null, 
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				SurveyRunIdentityAssignments = new List<TrajectoryIdentityAssignment>(),
				SurveyRunFeatureAssignments = new List<TrajectoryFeatureAssignment>(),
				TieInPoint = ConstructSurveyStation(),
				SurveyMeasurementList = new List<SurveyMeasurement>
					{
						ConstructSurveyMeasurement(),
					},
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
			};
		}
		public static SurveyRunBatchImport ConstructSurveyRunBatchImport()
		{
			return new SurveyRunBatchImport
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				SelectedFieldId = null, 
				SelectedClusterId = null, 
				SelectedWellId = null, 
				CommonDepthReference = "Default CommonDepthReference",
				ReplaceExistingTrajectories = false, 
				ReplaceTrajectoriesWithSameName = false, 
				Settings = ConstructSurveyImportSettings(),
				Rows = new List<SurveyRunBatchImportRow>
					{
						ConstructSurveyRunBatchImportRow(),
					},
			};
		}
		public static SurveyRunBatchImportRow ConstructSurveyRunBatchImportRow()
		{
			return new SurveyRunBatchImportRow
			{
				RowId = new Guid(),
				WellBoreId = null, 
				SurveyInstrumentId = null, 
				ParentSurveyRunId = null, 
				DepthReferenceName = "Default DepthReferenceName",
				FileName = "Default FileName",
				FileContentBase64 = "Default FileContentBase64",
			};
		}
		public static SurveyRunMinimumDistanceCalculation ConstructSurveyRunMinimumDistanceCalculation()
		{
			return new SurveyRunMinimumDistanceCalculation
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				ReferenceSurveyRunID = new Guid(),
				ComparisonSurveyRunIDList = new List<Guid>
					{
						new Guid(),
					},
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				ResultCount = 0, 
				IntervalResultCount = 0, 
				MaximumChordArcDistance = null, 
				AccountForBoreholeRadius = false, 
				OctreeMaximumDepth = 0, 
				OctreeMaximumSegmentCountPerLeaf = 0, 
				AdaptiveRefinementSettings = ConstructMinimumDistanceAdaptiveRefinementSettings(),
				GlobalMinimumCenterToCenterDistance = null, 
				GlobalMinimumClearanceDistance = null, 
				GlobalMinimumReferenceMD = null, 
				GlobalMinimumComparisonSurveyRunID = null, 
				GlobalMinimumComparisonMD = null, 
				GlobalMinimumToolface = null, 
				GlobalMinimumIsGravity = false, 
				ReferenceIntervalList = new List<MinimumDistanceReferenceInterval>
					{
						ConstructMinimumDistanceReferenceInterval(),
					},
				ResultList = new List<SurveyRunMinimumDistanceResult>
					{
						ConstructSurveyRunMinimumDistanceResult(),
					},
				IntervalResultList = new List<SurveyRunMinimumDistanceIntervalResult>
					{
						ConstructSurveyRunMinimumDistanceIntervalResult(),
					},
			};
		}
		public static SurveyRunMinimumDistanceIntervalResult ConstructSurveyRunMinimumDistanceIntervalResult()
		{
			return new SurveyRunMinimumDistanceIntervalResult
			{
				IntervalID = new Guid(),
				IntervalName = "Default IntervalName",
				StartMD = null, 
				EndMD = null, 
				ComparisonSurveyRunID = null, 
				SampleCount = 0, 
				AverageCenterToCenterDistance = null, 
				StandardDeviationCenterToCenterDistance = null, 
				AverageClearanceDistance = null, 
				StandardDeviationClearanceDistance = null, 
			};
		}
		public static SurveyRunMinimumDistanceResult ConstructSurveyRunMinimumDistanceResult()
		{
			return new SurveyRunMinimumDistanceResult
			{
				ReferenceMD = null, 
				ReferenceTVD = null, 
				ReferenceNorth = null, 
				ReferenceEast = null, 
				ReferenceBoreholeDiameter = null, 
				ComparisonSurveyRunID = null, 
				ComparisonMD = null, 
				ComparisonTVD = null, 
				ComparisonNorth = null, 
				ComparisonEast = null, 
				ComparisonBoreholeDiameter = null, 
				CenterToCenterDistance = null, 
				ClearanceDistance = null, 
				Toolface = null, 
				IsGravity = false, 
				IsAdaptiveRefinementSample = false, 
				RefinementLevel = 0, 
			};
		}
		public static SurveyRunMinimumDistanceResultChunk ConstructSurveyRunMinimumDistanceResultChunk()
		{
			return new SurveyRunMinimumDistanceResultChunk
			{
				OwnerID = new Guid(),
				ChunkIndex = 0, 
				ResultCount = 0, 
				StartReferenceMD = null, 
				EndReferenceMD = null, 
				ResultList = new List<SurveyRunMinimumDistanceResult>
					{
						ConstructSurveyRunMinimumDistanceResult(),
					},
			};
		}
		public static SurveyStationChunk ConstructSurveyStationChunk()
		{
			return new SurveyStationChunk
			{
				OwnerID = new Guid(),
				OwnerType = "Default OwnerType",
				ChunkIndex = 0, 
				StationCount = 0, 
				StartMD = null, 
				EndMD = null, 
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
			};
		}
		public static SurveyStationEllipse ConstructSurveyStationEllipse()
		{
			return new SurveyStationEllipse
			{
				SemiMajorAxis = null, 
				SemiMinorAxis = null, 
				OrientationAngle = null, 
			};
		}
		public static SurveyStationEllipseCalculation ConstructSurveyStationEllipseCalculation()
		{
			return new SurveyStationEllipseCalculation
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				ConfidenceFactor = 0.0, 
				SurveyInstrumentID = null, 
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
				SurveyStationEllipseResultList = new List<SurveyStationEllipseResult>
					{
						ConstructSurveyStationEllipseResult(),
					},
				HighestTvdSurveyPointList = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
				LowestTvdSurveyPointList = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
				CalculationMessage = "Default CalculationMessage",
			};
		}
		public static SurveyStationEllipseResult ConstructSurveyStationEllipseResult()
		{
			return new SurveyStationEllipseResult
			{
				MD = null, 
				HorizontalEllipse = ConstructSurveyStationEllipse(),
				VerticalEllipse = ConstructSurveyStationEllipse(),
				PerpendicularEllipse = ConstructSurveyStationEllipse(),
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
				FieldID = null, 
				ClusterID = null, 
				WellID = null, 
				WellBoreID = new Guid(),
				TrajectoryType = (TrajectoryType)0,
				IsDefinitive = false, 
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				TrajectoryIdentityAssignments = new List<TrajectoryIdentityAssignment>(),
				TrajectoryFeatureAssignments = new List<TrajectoryFeatureAssignment>(),
				SurveyRunSectionList = new List<TrajectorySurveyRunSection>
					{
						ConstructTrajectorySurveyRunSection(),
					},
				SurveyStationList = new List<SurveyStation>
					{
						ConstructSurveyStation(),
					},
				TieInPoint = ConstructSurveyStation(),
				CalculationType = (TrajectoryCalculationType)0,
				MDStep = 0.0, 
			};
		}
		public static TrajectoryAggregation ConstructTrajectoryAggregation()
		{
			return new TrajectoryAggregation
			{
				ID = new Guid(),
				TrajectoryID = new Guid(),
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				OriginalReferenceStationCount = 0, 
				CoarsenedReferencePointCount = 0, 
				SectionCount = 0, 
				AggregatedSurveyPointCount = 0, 
				DistanceResultCount = 0, 
				SectionList = new List<TrajectoryAggregationSection>
					{
						ConstructTrajectoryAggregationSection(),
					},
				AggregatedSurveyPointList = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
				CoarsenedReferenceTrajectory = new List<SurveyPoint>
					{
						ConstructSurveyPoint(),
					},
				DistanceResultList = new List<TrajectoryAggregationDistanceResult>
					{
						ConstructTrajectoryAggregationDistanceResult(),
					},
			};
		}
		public static TrajectoryAggregationCase ConstructTrajectoryAggregationCase()
		{
			return new TrajectoryAggregationCase
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				EpsilonL = null, 
				EpsilonKappa = null, 
				Alpha = null, 
				InterpolationInterval = null, 
				DistanceReferenceCoarseningThreshold = null, 
				TrajectoryAggregationList = new List<TrajectoryAggregation>
					{
						ConstructTrajectoryAggregation(),
					},
			};
		}
		public static TrajectoryAggregationDistanceResult ConstructTrajectoryAggregationDistanceResult()
		{
			return new TrajectoryAggregationDistanceResult
			{
				ReferenceMD = null, 
				ReferenceTVD = null, 
				ReferenceNorth = null, 
				ReferenceEast = null, 
				ClosestMD = null, 
				ClosestTVD = null, 
				ClosestNorth = null, 
				ClosestEast = null, 
				CenterToCenterDistance = null, 
				ClosestSectionIndex = null, 
				ClosestSectionType = (TrajectoryAggregationSectionType)0,
				SectionParameter = null, 
			};
		}
		public static TrajectoryAggregationDistanceResultChunk ConstructTrajectoryAggregationDistanceResultChunk()
		{
			return new TrajectoryAggregationDistanceResultChunk
			{
				OwnerID = new Guid(),
				ChunkIndex = 0, 
				ResultCount = 0, 
				StartReferenceMD = null, 
				EndReferenceMD = null, 
				ResultList = new List<TrajectoryAggregationDistanceResult>
					{
						ConstructTrajectoryAggregationDistanceResult(),
					},
			};
		}
		public static TrajectoryAggregationSection ConstructTrajectoryAggregationSection()
		{
			return new TrajectoryAggregationSection
			{
				SectionIndex = 0, 
				SectionType = (TrajectoryAggregationSectionType)0,
				StartMD = null, 
				EndMD = null, 
				StartInclination = null, 
				StartAzimuth = null, 
				StartTVD = null, 
				StartNorth = null, 
				StartEast = null, 
				CircularArcCurvature = null, 
				CircularArcStartToolface = null, 
				ConstantCurvature = null, 
				ConstantToolface = null, 
				BuildRate = null, 
				TurnRate = null, 
			};
		}
		public static TrajectoryMinimumDistanceCalculation ConstructTrajectoryMinimumDistanceCalculation()
		{
			return new TrajectoryMinimumDistanceCalculation
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				ReferenceTrajectoryID = new Guid(),
				ComparisonTrajectoryIDList = new List<Guid>
					{
						new Guid(),
					},
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				ResultCount = 0, 
				IntervalResultCount = 0, 
				MaximumChordArcDistance = null, 
				AccountForBoreholeRadius = false, 
				OctreeMaximumDepth = 0, 
				OctreeMaximumSegmentCountPerLeaf = 0, 
				AdaptiveRefinementSettings = ConstructMinimumDistanceAdaptiveRefinementSettings(),
				GlobalMinimumCenterToCenterDistance = null, 
				GlobalMinimumClearanceDistance = null, 
				GlobalMinimumReferenceMD = null, 
				GlobalMinimumComparisonTrajectoryID = null, 
				GlobalMinimumComparisonMD = null, 
				GlobalMinimumToolface = null, 
				GlobalMinimumIsGravity = false, 
				ReferenceIntervalList = new List<MinimumDistanceReferenceInterval>
					{
						ConstructMinimumDistanceReferenceInterval(),
					},
				ResultList = new List<TrajectoryMinimumDistanceResult>
					{
						ConstructTrajectoryMinimumDistanceResult(),
					},
				IntervalResultList = new List<TrajectoryMinimumDistanceIntervalResult>
					{
						ConstructTrajectoryMinimumDistanceIntervalResult(),
					},
			};
		}
		public static TrajectoryMinimumDistanceIntervalResult ConstructTrajectoryMinimumDistanceIntervalResult()
		{
			return new TrajectoryMinimumDistanceIntervalResult
			{
				IntervalID = new Guid(),
				IntervalName = "Default IntervalName",
				StartMD = null, 
				EndMD = null, 
				ComparisonTrajectoryID = null, 
				SampleCount = 0, 
				AverageCenterToCenterDistance = null, 
				StandardDeviationCenterToCenterDistance = null, 
				AverageClearanceDistance = null, 
				StandardDeviationClearanceDistance = null, 
			};
		}
		public static TrajectoryMinimumDistanceResult ConstructTrajectoryMinimumDistanceResult()
		{
			return new TrajectoryMinimumDistanceResult
			{
				ReferenceMD = null, 
				ReferenceTVD = null, 
				ReferenceNorth = null, 
				ReferenceEast = null, 
				ReferenceBoreholeDiameter = null, 
				ComparisonTrajectoryID = null, 
				ComparisonMD = null, 
				ComparisonTVD = null, 
				ComparisonNorth = null, 
				ComparisonEast = null, 
				ComparisonBoreholeDiameter = null, 
				CenterToCenterDistance = null, 
				ClearanceDistance = null, 
				Toolface = null, 
				IsGravity = false, 
				IsAdaptiveRefinementSample = false, 
				RefinementLevel = 0, 
			};
		}
		public static TrajectoryMinimumDistanceResultChunk ConstructTrajectoryMinimumDistanceResultChunk()
		{
			return new TrajectoryMinimumDistanceResultChunk
			{
				OwnerID = new Guid(),
				ChunkIndex = 0, 
				ResultCount = 0, 
				StartReferenceMD = null, 
				EndReferenceMD = null, 
				ResultList = new List<TrajectoryMinimumDistanceResult>
					{
						ConstructTrajectoryMinimumDistanceResult(),
					},
			};
		}
		public static TrajectoryRealizationCase ConstructTrajectoryRealizationCase()
		{
			return new TrajectoryRealizationCase
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				TrajectoryID = new Guid(),
				RealizationCount = 0, 
				CoarseningMaximumDistance = 0.0, 
				RandomSeed = null, 
				ReferenceStationCount = null, 
				CoarsenedStationCount = null, 
				CalculationState = (CalculationState)0,
				CalculationProgress = 0.0, 
				CalculationMessage = "Default CalculationMessage",
				RealizationList = new List<List<SurveyPoint>>
					{
						new List<SurveyPoint>
						{
							ConstructSurveyPoint(),
						}
					},
			};
		}
		public static TrajectoryRealizationChunk ConstructTrajectoryRealizationChunk()
		{
			return new TrajectoryRealizationChunk
			{
				OwnerID = new Guid(),
				ChunkIndex = 0, 
				RealizationCount = 0, 
				SurveyPointCount = 0, 
				StartMD = null, 
				EndMD = null, 
				RealizationList = new List<List<SurveyPoint>>
					{
						new List<SurveyPoint>
						{
							ConstructSurveyPoint(),
						}
					},
			};
		}
		public static TrajectorySurveyRunSection ConstructTrajectorySurveyRunSection()
		{
			return new TrajectorySurveyRunSection
			{
				SurveyRunID = new Guid(),
				StartAbscissa = 0.0, 
			};
		}
		public static UsageStatisticsTrajectory ConstructUsageStatisticsTrajectory()
		{
			return new UsageStatisticsTrajectory
			{
				LastSaved = DateTimeOffset.UtcNow,
				BackUpInterval = "Default BackUpInterval",
				GetAllTrajectoryIdPerDay = ConstructHistory(),
				GetAllTrajectoryMetaInfoPerDay = ConstructHistory(),
				GetTrajectoryByIdPerDay = ConstructHistory(),
				GetAllTrajectoryLightPerDay = ConstructHistory(),
				GetAllTrajectoryPerDay = ConstructHistory(),
				PostTrajectoryPerDay = ConstructHistory(),
				PutTrajectoryByIdPerDay = ConstructHistory(),
				DeleteTrajectoryByIdPerDay = ConstructHistory(),
			};
		}
		public static SurveyPoint ConstructSurveyPoint()
		{
			return new SurveyPoint
			{
				Z = null, 
				Abscissa = null, 
				Inclination = null, 
				Azimuth = null, 
				MD = null, 
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
				VerticalSection = null, 
				Annotation = "Default Annotation",
			};
		}
		public static SurveyStation ConstructSurveyStation()
		{
			return new SurveyStation
			{
				Z = null, 
				Abscissa = null, 
				Inclination = null, 
				Azimuth = null, 
				MD = null, 
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
				VerticalSection = null, 
				Annotation = "Default Annotation",
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
		public static WellBoreBatchCatalogDependencies ConstructWellBoreBatchCatalogDependencies()
		{
			return new WellBoreBatchCatalogDependencies
			{
				Identities = new List<WellBoreIdentity>
					{
						ConstructWellBoreIdentity(),
					},
				FeatureCategories = new List<WellBoreFeatureCategory>
					{
						ConstructWellBoreFeatureCategory(),
					},
			};
		}
		public static WellBoreBatchCatalogMapping ConstructWellBoreBatchCatalogMapping()
		{
			return new WellBoreBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static WellBoreBatchError ConstructWellBoreBatchError()
		{
			return new WellBoreBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellBoreBatchErrorEnvelope ConstructWellBoreBatchErrorEnvelope()
		{
			return new WellBoreBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<WellBoreBatchError>
					{
						ConstructWellBoreBatchError(),
					},
			};
		}
		public static WellBoreBatchExportDocument ConstructWellBoreBatchExportDocument()
		{
			return new WellBoreBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructWellBoreBatchCatalogDependencies(),
				WellBores = new List<WellBore>
					{
						ConstructWellBore(),
					},
			};
		}
		public static WellBoreBatchExportRequest ConstructWellBoreBatchExportRequest()
		{
			return new WellBoreBatchExportRequest
			{
				Scope = (WellBoreBatchExportScope)0,
				WellBoreIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBoreBatchRestoreRequest ConstructWellBoreBatchRestoreRequest()
		{
			return new WellBoreBatchRestoreRequest
			{
				ConflictPolicy = (WellBoreBatchRestoreConflictPolicy)0,
				CatalogPolicy = (WellBoreBatchCatalogRestorePolicy)0,
				Document = ConstructWellBoreBatchExportDocument(),
			};
		}
		public static WellBoreBatchRestoreResponse ConstructWellBoreBatchRestoreResponse()
		{
			return new WellBoreBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<WellBoreBatchCatalogMapping>
					{
						ConstructWellBoreBatchCatalogMapping(),
					},
				WellBoreIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBoreDetailsUpdate ConstructWellBoreDetailsUpdate()
		{
			return new WellBoreDetailsUpdate
			{
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static WellBoreExternalReferenceAuditRequest ConstructWellBoreExternalReferenceAuditRequest()
		{
			return new WellBoreExternalReferenceAuditRequest
			{
				Scope = (WellBoreExternalReferenceAuditScope)0,
				WellBoreIDs = new List<Guid>
					{
						new Guid(),
					},
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static WellBoreExternalReferenceAuditResult ConstructWellBoreExternalReferenceAuditResult()
		{
			return new WellBoreExternalReferenceAuditResult
			{
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Total = 0, 
				Offset = 0, 
				Limit = 0, 
				ValidCount = 0, 
				InvalidCount = 0, 
				UnavailableCount = 0, 
				Items = new List<WellBoreExternalReferenceValidation>
					{
						ConstructWellBoreExternalReferenceValidation(),
					},
			};
		}
		public static WellBoreExternalReferenceIssue ConstructWellBoreExternalReferenceIssue()
		{
			return new WellBoreExternalReferenceIssue
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellBoreExternalReferenceValidation ConstructWellBoreExternalReferenceValidation()
		{
			return new WellBoreExternalReferenceValidation
			{
				WellBoreID = new Guid(),
				WellID = null, 
				RigID = null, 
				WellExists = null, 
				RigExists = null, 
				Status = (WellBoreExternalReferenceValidationStatus)0,
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Issues = new List<WellBoreExternalReferenceIssue>
					{
						ConstructWellBoreExternalReferenceIssue(),
					},
			};
		}
		public static WellBoreFeatureAssignment ConstructWellBoreFeatureAssignment()
		{
			return new WellBoreFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreFeatureCategory ConstructWellBoreFeatureCategory()
		{
			return new WellBoreFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<WellBoreFeatureOption>
					{
						ConstructWellBoreFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreFeatureOption ConstructWellBoreFeatureOption()
		{
			return new WellBoreFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static WellBoreIdentity ConstructWellBoreIdentity()
		{
			return new WellBoreIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellBoreIdentityAssignment ConstructWellBoreIdentityAssignment()
		{
			return new WellBoreIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static WellBoreMutationError ConstructWellBoreMutationError()
		{
			return new WellBoreMutationError
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
				ReferencingWellBoreIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBoreMutationErrorEnvelope ConstructWellBoreMutationErrorEnvelope()
		{
			return new WellBoreMutationErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<WellBoreMutationError>
					{
						ConstructWellBoreMutationError(),
					},
			};
		}
		public static WellBoreSearchResult ConstructWellBoreSearchResult()
		{
			return new WellBoreSearchResult
			{
				Items = new List<WellBore>
					{
						ConstructWellBore(),
					},
				Total = 0, 
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static WellBoreTopologyUpdate ConstructWellBoreTopologyUpdate()
		{
			return new WellBoreTopologyUpdate
			{
				WellID = null, 
				RigID = null, 
				IsSidetrack = false, 
				ParentWellBoreID = null, 
				TieInPointAlongHoleDepth = ConstructGaussianDrillingProperty(),
				SidetrackType = (SidetrackType)0,
			};
		}
		public static Point3DGlobalCoordinates ConstructPoint3DGlobalCoordinates()
		{
			return new Point3DGlobalCoordinates
			{
				X = null, 
				Y = null, 
				Z = null, 
				RiemannianNorth = null, 
				RiemannianEast = null, 
				Latitude = null, 
				Longitude = null, 
				TVD = null, 
			};
		}
		public static ClusterBatchCatalogDependencies ConstructClusterBatchCatalogDependencies()
		{
			return new ClusterBatchCatalogDependencies
			{
				Identities = new List<ClusterIdentity>
					{
						ConstructClusterIdentity(),
					},
				ClusterFeatureCategories = new List<ClusterFeatureCategory>
					{
						ConstructClusterFeatureCategory(),
					},
				SlotFeatureCategories = new List<SlotFeatureCategory>
					{
						ConstructSlotFeatureCategory(),
					},
			};
		}
		public static ClusterBatchCatalogMapping ConstructClusterBatchCatalogMapping()
		{
			return new ClusterBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static ClusterBatchError ConstructClusterBatchError()
		{
			return new ClusterBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static ClusterBatchErrorEnvelope ConstructClusterBatchErrorEnvelope()
		{
			return new ClusterBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<ClusterBatchError>
					{
						ConstructClusterBatchError(),
					},
			};
		}
		public static ClusterBatchExportDocument ConstructClusterBatchExportDocument()
		{
			return new ClusterBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructClusterBatchCatalogDependencies(),
				ExternalReferences = ConstructClusterBatchExternalReferences(),
				Clusters = new List<Cluster>
					{
						ConstructCluster(),
					},
			};
		}
		public static ClusterBatchExportRequest ConstructClusterBatchExportRequest()
		{
			return new ClusterBatchExportRequest
			{
				Scope = (ClusterBatchExportScope)0,
				ClusterIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static ClusterBatchExternalReference ConstructClusterBatchExternalReference()
		{
			return new ClusterBatchExternalReference
			{
				SourceID = new Guid(),
				Name = "Default Name",
			};
		}
		public static ClusterBatchExternalReferenceMapping ConstructClusterBatchExternalReferenceMapping()
		{
			return new ClusterBatchExternalReferenceMapping
			{
				Resource = "Default Resource",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static ClusterBatchExternalReferences ConstructClusterBatchExternalReferences()
		{
			return new ClusterBatchExternalReferences
			{
				Fields = new List<ClusterBatchExternalReference>
					{
						ConstructClusterBatchExternalReference(),
					},
				Rigs = new List<ClusterBatchExternalReference>
					{
						ConstructClusterBatchExternalReference(),
					},
			};
		}
		public static ClusterBatchRestoreRequest ConstructClusterBatchRestoreRequest()
		{
			return new ClusterBatchRestoreRequest
			{
				ConflictPolicy = (ClusterBatchRestoreConflictPolicy)0,
				CatalogPolicy = (ClusterBatchCatalogRestorePolicy)0,
				Document = ConstructClusterBatchExportDocument(),
			};
		}
		public static ClusterBatchRestoreResponse ConstructClusterBatchRestoreResponse()
		{
			return new ClusterBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<ClusterBatchCatalogMapping>
					{
						ConstructClusterBatchCatalogMapping(),
					},
				ExternalReferenceMappings = new List<ClusterBatchExternalReferenceMapping>
					{
						ConstructClusterBatchExternalReferenceMapping(),
					},
				ClusterIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static ClusterFeatureAssignment ConstructClusterFeatureAssignment()
		{
			return new ClusterFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static ClusterFeatureCategory ConstructClusterFeatureCategory()
		{
			return new ClusterFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<ClusterFeatureOption>
					{
						ConstructClusterFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static ClusterFeatureOption ConstructClusterFeatureOption()
		{
			return new ClusterFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static ClusterIdentity ConstructClusterIdentity()
		{
			return new ClusterIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static ClusterIdentityAssignment ConstructClusterIdentityAssignment()
		{
			return new ClusterIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static SlotFeatureAssignment ConstructSlotFeatureAssignment()
		{
			return new SlotFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static SlotFeatureCategory ConstructSlotFeatureCategory()
		{
			return new SlotFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<SlotFeatureOption>
					{
						ConstructSlotFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static SlotFeatureOption ConstructSlotFeatureOption()
		{
			return new SlotFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static FieldBatchCatalogDependencies ConstructFieldBatchCatalogDependencies()
		{
			return new FieldBatchCatalogDependencies
			{
				FeatureCategories = new List<FieldFeatureCategory>
					{
						ConstructFieldFeatureCategory(),
					},
				MembershipCategories = new List<FieldMembershipCategory>
					{
						ConstructFieldMembershipCategory(),
					},
				Identities = new List<FieldIdentity>
					{
						ConstructFieldIdentity(),
					},
				DelineationLineTypes = new List<FieldDelineationLineType>
					{
						ConstructFieldDelineationLineType(),
					},
			};
		}
		public static FieldBatchCatalogMapping ConstructFieldBatchCatalogMapping()
		{
			return new FieldBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static FieldBatchError ConstructFieldBatchError()
		{
			return new FieldBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static FieldBatchErrorEnvelope ConstructFieldBatchErrorEnvelope()
		{
			return new FieldBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<FieldBatchError>
					{
						ConstructFieldBatchError(),
					},
			};
		}
		public static FieldBatchExportDocument ConstructFieldBatchExportDocument()
		{
			return new FieldBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructFieldBatchCatalogDependencies(),
				Fields = new List<Field>
					{
						ConstructField(),
					},
			};
		}
		public static FieldBatchExportRequest ConstructFieldBatchExportRequest()
		{
			return new FieldBatchExportRequest
			{
				Scope = (FieldBatchExportScope)0,
				FieldIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static FieldBatchRestoreRequest ConstructFieldBatchRestoreRequest()
		{
			return new FieldBatchRestoreRequest
			{
				ConflictPolicy = (FieldBatchRestoreConflictPolicy)0,
				CatalogPolicy = (FieldBatchCatalogRestorePolicy)0,
				Document = ConstructFieldBatchExportDocument(),
			};
		}
		public static FieldBatchRestoreResponse ConstructFieldBatchRestoreResponse()
		{
			return new FieldBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<FieldBatchCatalogMapping>
					{
						ConstructFieldBatchCatalogMapping(),
					},
				FieldIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static FieldCatalogReference ConstructFieldCatalogReference()
		{
			return new FieldCatalogReference
			{
				ID = new Guid(),
				Name = "Default Name",
				Authority = "Default Authority",
				Code = "Default Code",
			};
		}
		public static FieldConversionErrorEnvelope ConstructFieldConversionErrorEnvelope()
		{
			return new FieldConversionErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<FieldConversionValidationError>
					{
						ConstructFieldConversionValidationError(),
					},
			};
		}
		public static FieldConversionValidationError ConstructFieldConversionValidationError()
		{
			return new FieldConversionValidationError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static FieldConversionWarning ConstructFieldConversionWarning()
		{
			return new FieldConversionWarning
			{
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static FieldCoordinateConversionPositionResult ConstructFieldCoordinateConversionPositionResult()
		{
			return new FieldCoordinateConversionPositionResult
			{
				PositionIndex = 0, 
				ProjectionDatumGeographicCoordinate = ConstructFieldGeographicCoordinate(),
				Wgs84GeographicCoordinate = ConstructFieldGeographicCoordinate(),
				ProjectedCoordinate = ConstructFieldProjectedCoordinate(),
				ProjectionDatumVerticalDepth = 0.0, 
				Wgs84VerticalDepth = null, 
				CoordinateEpochUtc = DateTimeOffset.UtcNow,
				GridConvergence = null, 
			};
		}
		public static FieldCoordinateConversionResponse ConstructFieldCoordinateConversionResponse()
		{
			return new FieldCoordinateConversionResponse
			{
				FieldID = new Guid(),
				ProjectionDefinition = ConstructFieldCatalogReference(),
				ProjectionDatum = ConstructFieldCatalogReference(),
				Wgs84Datum = ConstructFieldCatalogReference(),
				ApiAxisConvention = "Default ApiAxisConvention",
				Positions = new List<FieldCoordinateConversionPositionResult>
					{
						ConstructFieldCoordinateConversionPositionResult(),
					},
				Warnings = new List<FieldConversionWarning>
					{
						ConstructFieldConversionWarning(),
					},
			};
		}
		public static FieldDelineationBoundaryLine ConstructFieldDelineationBoundaryLine()
		{
			return new FieldDelineationBoundaryLine
			{
				ID = new Guid(),
				IsInteriorBoundary = false, 
				IsClosed = false, 
				Points = new List<Point3DGlobalCoordinates>
					{
						ConstructPoint3DGlobalCoordinates(),
					},
			};
		}
		public static FieldDelineationLine ConstructFieldDelineationLine()
		{
			return new FieldDelineationLine
			{
				ID = new Guid(),
				DelineationLineTypeID = null, 
				Name = "Default Name",
				Description = "Default Description",
				Margin = null, 
				TopDepth = null, 
				BottomDepth = null, 
				Points = new List<Point3DGlobalCoordinates>
					{
						ConstructPoint3DGlobalCoordinates(),
					},
				CalculatedBoundaryLines = new List<FieldDelineationBoundaryLine>
					{
						ConstructFieldDelineationBoundaryLine(),
					},
			};
		}
		public static FieldDelineationLineType ConstructFieldDelineationLineType()
		{
			return new FieldDelineationLineType
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldFeatureAssignment ConstructFieldFeatureAssignment()
		{
			return new FieldFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldFeatureCategory ConstructFieldFeatureCategory()
		{
			return new FieldFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<FieldFeatureOption>
					{
						ConstructFieldFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldFeatureOption ConstructFieldFeatureOption()
		{
			return new FieldFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static FieldForwardConversionPosition ConstructFieldForwardConversionPosition()
		{
			return new FieldForwardConversionPosition
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				VerticalDepth = 0.0, 
				CoordinateEpochUtc = DateTimeOffset.UtcNow,
			};
		}
		public static FieldForwardConversionRequest ConstructFieldForwardConversionRequest()
		{
			return new FieldForwardConversionRequest
			{
				FieldID = new Guid(),
				SourceGeographicReference = (FieldGeographicReference)0,
				ProjectionApplicabilityPolicy = (FieldApplicabilityPolicy)0,
				Transformation = ConstructFieldTransformationOptions(),
				Positions = new List<FieldForwardConversionPosition>
					{
						ConstructFieldForwardConversionPosition(),
					},
			};
		}
		public static FieldGeographicCoordinate ConstructFieldGeographicCoordinate()
		{
			return new FieldGeographicCoordinate
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
			};
		}
		public static FieldIdentity ConstructFieldIdentity()
		{
			return new FieldIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldIdentityAssignment ConstructFieldIdentityAssignment()
		{
			return new FieldIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static FieldInverseConversionPosition ConstructFieldInverseConversionPosition()
		{
			return new FieldInverseConversionPosition
			{
				Easting = 0.0, 
				Northing = 0.0, 
				VerticalDepth = 0.0, 
				CoordinateEpochUtc = DateTimeOffset.UtcNow,
			};
		}
		public static FieldInverseConversionRequest ConstructFieldInverseConversionRequest()
		{
			return new FieldInverseConversionRequest
			{
				FieldID = new Guid(),
				ProjectionApplicabilityPolicy = (FieldApplicabilityPolicy)0,
				Transformation = ConstructFieldTransformationOptions(),
				Positions = new List<FieldInverseConversionPosition>
					{
						ConstructFieldInverseConversionPosition(),
					},
			};
		}
		public static FieldMembershipAssignment ConstructFieldMembershipAssignment()
		{
			return new FieldMembershipAssignment
			{
				ID = new Guid(),
				MembershipCategoryID = null, 
				MembershipOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldMembershipCategory ConstructFieldMembershipCategory()
		{
			return new FieldMembershipCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<FieldMembershipOption>
					{
						ConstructFieldMembershipOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static FieldMembershipOption ConstructFieldMembershipOption()
		{
			return new FieldMembershipOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static FieldMutationError ConstructFieldMutationError()
		{
			return new FieldMutationError
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
				ReferencingFieldIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static FieldMutationErrorEnvelope ConstructFieldMutationErrorEnvelope()
		{
			return new FieldMutationErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<FieldMutationError>
					{
						ConstructFieldMutationError(),
					},
			};
		}
		public static FieldProjectedCoordinate ConstructFieldProjectedCoordinate()
		{
			return new FieldProjectedCoordinate
			{
				Easting = 0.0, 
				Northing = 0.0, 
			};
		}
		public static FieldTransformationOptions ConstructFieldTransformationOptions()
		{
			return new FieldTransformationOptions
			{
				SelectionPolicy = (FieldTransformationSelectionPolicy)0,
				TransformationPathIDs = new List<Guid>
					{
						new Guid(),
					},
				SelectionToken = "Default SelectionToken",
				ApplicabilityPolicy = (FieldApplicabilityPolicy)0,
				DepthPolicy = (FieldDepthTransformationPolicy)0,
			};
		}
		public static EquipmentMeasurementCapability ConstructEquipmentMeasurementCapability()
		{
			return new EquipmentMeasurementCapability
			{
				ID = null, 
				Name = "Default Name",
				Description = "Default Description",
				MeasurementCode = "Default MeasurementCode",
				PhysicalQuantity = "Default PhysicalQuantity",
				SourceKind = (MeasurementSourceKind?)0,
				SourceType = "Default SourceType",
				SourceComponentID = null, 
				Manufacturer = "Default Manufacturer",
				Model = "Default Model",
				ProductCode = "Default ProductCode",
				SerialNumber = "Default SerialNumber",
				MinimumValue = null, 
				MaximumValue = null, 
				AbsoluteAccuracy = null, 
				RelativeAccuracy = null, 
				UpdateFrequency = null, 
			};
		}
		public static JackUpProfile ConstructJackUpProfile()
		{
			return new JackUpProfile
			{
				LegLength = null, 
				LongitudinalLegSpacing = null, 
				TransverseLegSpacing = null, 
				MaximumCantileverSkidOut = null, 
				MaximumCantileverTransverseReach = null, 
				SubstructureTravel = null, 
				MaximumPreload = null, 
			};
		}
		public static MarineUnitProfile ConstructMarineUnitProfile()
		{
			return new MarineUnitProfile
			{
				HullLength = null, 
				HullWidth = null, 
				HullDepth = null, 
				OperatingDraft = null, 
				TransitDraft = null, 
				OperatingDisplacement = null, 
				VariableDeckLoad = null, 
				MaximumTransitSpeed = null, 
				AccommodationCapacity = null, 
				HelideckCapability = "Default HelideckCapability",
				CraneCount = null, 
			};
		}
		public static MudPumpLinerConfiguration ConstructMudPumpLinerConfiguration()
		{
			return new MudPumpLinerConfiguration
			{
				LinerInnerDiameter = null, 
				DisplacementPerStroke = null, 
				MaximumVolumetricFlowRate = null, 
				MaximumDischargePressure = null, 
			};
		}
		public static RigBatchCatalogDependencies ConstructRigBatchCatalogDependencies()
		{
			return new RigBatchCatalogDependencies
			{
				FeatureCategories = new List<RigFeatureCategory>
					{
						ConstructRigFeatureCategory(),
					},
			};
		}
		public static RigBatchCatalogMapping ConstructRigBatchCatalogMapping()
		{
			return new RigBatchCatalogMapping
			{
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static RigBatchError ConstructRigBatchError()
		{
			return new RigBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static RigBatchErrorEnvelope ConstructRigBatchErrorEnvelope()
		{
			return new RigBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<RigBatchError>
					{
						ConstructRigBatchError(),
					},
			};
		}
		public static RigBatchExportDocument ConstructRigBatchExportDocument()
		{
			return new RigBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructRigBatchCatalogDependencies(),
				ExternalReferences = ConstructRigBatchExternalReferences(),
				Rigs = new List<Rig>
					{
						ConstructRig(),
					},
				Photos = new List<RigBatchPhoto>
					{
						ConstructRigBatchPhoto(),
					},
			};
		}
		public static RigBatchExportRequest ConstructRigBatchExportRequest()
		{
			return new RigBatchExportRequest
			{
				Scope = (RigBatchExportScope)0,
				RigIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static RigBatchExternalReference ConstructRigBatchExternalReference()
		{
			return new RigBatchExternalReference
			{
				SourceID = new Guid(),
				Name = "Default Name",
			};
		}
		public static RigBatchExternalReferenceMapping ConstructRigBatchExternalReferenceMapping()
		{
			return new RigBatchExternalReferenceMapping
			{
				Resource = "Default Resource",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static RigBatchExternalReferences ConstructRigBatchExternalReferences()
		{
			return new RigBatchExternalReferences
			{
				Clusters = new List<RigBatchExternalReference>
					{
						ConstructRigBatchExternalReference(),
					},
			};
		}
		public static RigBatchPhoto ConstructRigBatchPhoto()
		{
			return new RigBatchPhoto
			{
				Metadata = ConstructRigPhotoMetadata(),
				ContentBase64 = "Default ContentBase64",
			};
		}
		public static RigBatchRestoreRequest ConstructRigBatchRestoreRequest()
		{
			return new RigBatchRestoreRequest
			{
				ConflictPolicy = (RigBatchRestoreConflictPolicy)0,
				CatalogPolicy = (RigBatchCatalogRestorePolicy)0,
				Document = ConstructRigBatchExportDocument(),
			};
		}
		public static RigBatchRestoreResponse ConstructRigBatchRestoreResponse()
		{
			return new RigBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				RestoredPhotoCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<RigBatchCatalogMapping>
					{
						ConstructRigBatchCatalogMapping(),
					},
				ExternalReferenceMappings = new List<RigBatchExternalReferenceMapping>
					{
						ConstructRigBatchExternalReferenceMapping(),
					},
				RigIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static RigExternalIdentifier ConstructRigExternalIdentifier()
		{
			return new RigExternalIdentifier
			{
				Authority = "Default Authority",
				Identifier = "Default Identifier",
			};
		}
		public static RigFeatureAssignment ConstructRigFeatureAssignment()
		{
			return new RigFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = new Guid(),
				FeatureOptionID = new Guid(),
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
				Notes = "Default Notes",
				EvidenceReference = "Default EvidenceReference",
			};
		}
		public static RigFeatureCategory ConstructRigFeatureCategory()
		{
			return new RigFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Code = "Default Code",
				Name = "Default Name",
				Description = "Default Description",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				IsBuiltIn = false, 
				IsDeprecated = false, 
				Options = new List<RigFeatureOption>
					{
						ConstructRigFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static RigFeatureOption ConstructRigFeatureOption()
		{
			return new RigFeatureOption
			{
				ID = new Guid(),
				Code = "Default Code",
				Name = "Default Name",
				Description = "Default Description",
				IsBuiltIn = false, 
				IsDeprecated = false, 
			};
		}
		public static RigIdentification ConstructRigIdentification()
		{
			return new RigIdentification
			{
				Owner = "Default Owner",
				Operator = "Default Operator",
				ManufacturerOrShipyard = "Default ManufacturerOrShipyard",
				DesignName = "Default DesignName",
				YearBuilt = null, 
				YearEnteredService = null, 
				Registration = "Default Registration",
				Flag = "Default Flag",
				ClassificationSociety = "Default ClassificationSociety",
				ClassNotation = "Default ClassNotation",
				ApprovalsAndCertifications = new List<string>
					{
						"",
					},
				FormerNames = new List<string>
					{
						"",
					},
				ExternalIdentifiers = new List<RigExternalIdentifier>
					{
						ConstructRigExternalIdentifier(),
					},
				MajorModifications = new List<RigModification>
					{
						ConstructRigModification(),
					},
			};
		}
		public static RigModification ConstructRigModification()
		{
			return new RigModification
			{
				Date = DateTimeOffset.UtcNow,
				Description = "Default Description",
			};
		}
		public static RigMutationError ConstructRigMutationError()
		{
			return new RigMutationError
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static RigMutationErrorEnvelope ConstructRigMutationErrorEnvelope()
		{
			return new RigMutationErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<RigMutationError>
					{
						ConstructRigMutationError(),
					},
			};
		}
		public static RigOperatingEnvelope ConstructRigOperatingEnvelope()
		{
			return new RigOperatingEnvelope
			{
				MaximumDrillingDepth = null, 
				MaximumWaterDepth = null, 
				RatedHookLoad = null, 
				MaximumSetbackLoad = null, 
				MaximumRotaryLoad = null, 
				MaximumMudSystemPressure = null, 
				MinimumAmbientTemperature = null, 
				MaximumAmbientTemperature = null, 
				MaximumOperatingWindSpeed = null, 
				MaximumSurvivalWindSpeed = null, 
			};
		}
		public static RigPhotoMetadata ConstructRigPhotoMetadata()
		{
			return new RigPhotoMetadata
			{
				MetaInfo = ConstructMetaInfo(),
				RigID = new Guid(),
				FileName = "Default FileName",
				Title = "Default Title",
				Caption = "Default Caption",
				AlternativeText = "Default AlternativeText",
				ContentType = "Default ContentType",
				ByteLength = 0, 
				Sha256 = "Default Sha256",
				DisplayOrder = 0, 
				IsPrimary = false, 
				Source = "Default Source",
				Attribution = "Default Attribution",
				License = "Default License",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static RigReadResponse ConstructRigReadResponse()
		{
			return new RigReadResponse
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				Description = "Default Description",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
				Identification = ConstructRigIdentification(),
				RigType = (RigType?)0,
				OperatingEnvironment = (RigEnvironment?)0,
				MobilityType = (RigMobilityType?)0,
				OperatingEnvelope = ConstructRigOperatingEnvelope(),
				MarineUnitProfile = ConstructMarineUnitProfile(),
				JackUpProfile = ConstructJackUpProfile(),
				StationKeepingSystem = ConstructStationKeepingSystem(),
				StorageCapacities = new List<RigStorageCapacity>
					{
						ConstructRigStorageCapacity(),
					},
				FeatureAssignments = new List<RigFeatureAssignment>(),
				MudPumpList = new List<MudPump>
					{
						ConstructMudPump(),
					},
				CementPumpList = new List<CementPump>
					{
						ConstructCementPump(),
					},
				CementUnit = ConstructCementUnit(),
				DriveMode = ConstructDriveMode(),
				MainRigMast = ConstructRigMast(),
				AuxiliaryRigMast = ConstructRigMast(),
				MudTankList = new List<MudTank>
					{
						ConstructMudTank(),
					},
				GeneratorList = new List<Generator>
					{
						ConstructGenerator(),
					},
				ShaleShakerList = new List<ShaleShaker>
					{
						ConstructShaleShaker(),
					},
				AuxSolidsControl = ConstructAuxSolidsControl(),
				DrillingFluidType = ConstructDrillingFluidTypeDescriptor(),
				FlowSensor = ConstructFlowSensor(),
				MeasurementAfm = ConstructMeasurementAfm(),
				ReturnFlowLine = ConstructReturnFlowLine(),
				MudGasSeparatorList = new List<MudGasSeparator>
					{
						ConstructMudGasSeparator(),
					},
				DesanderList = new List<Desander>
					{
						ConstructDesander(),
					},
				DesilterList = new List<Desilter>
					{
						ConstructDesilter(),
					},
				CentrifugeList = new List<Centrifuge>
					{
						ConstructCentrifuge(),
					},
				DegasserList = new List<Degasser>
					{
						ConstructDegasser(),
					},
				CuttingsTransportSystem = ConstructCuttingsTransportSystem(),
				CuttingsDryerList = new List<CuttingsDryer>
					{
						ConstructCuttingsDryer(),
					},
				PipeDeck = ConstructPipeDeck(),
				Accumulator = ConstructAccumulator(),
				BopStack = ConstructBopStack(),
				FloatValve = ConstructFloatValve(),
				AutoDriller = ConstructAutoDriller(),
				MpdController = ConstructMpdController(),
				MpdControlDevice = ConstructMpdControlDevice(),
				ContinuousCirculationDevice = ConstructContinuousCirculationDevice(),
				DrillingChokeManifold = ConstructDrillingChokeManifold(),
				SurfaceMpdEquipment = ConstructSurfaceMpdEquipment(),
				MarineMpdEquipment = ConstructMarineMpdEquipment(),
				MultiPhaseSeparator = ConstructMultiPhaseSeparator(),
				FlowRoutingManifold = ConstructFlowRoutingManifold(),
				DrillstringHeaveCompensator = ConstructDrillstringHeaveCompensator(),
				DrillingMarineRiser = ConstructDrillingMarineRiser(),
				RiserHeaveCompensator = ConstructRiserHeaveCompensator(),
				DrillFloorElevation = null, 
				IsFixedPlatform = false, 
				ClusterID = null, 
				Photos = new List<RigPhotoMetadata>
					{
						ConstructRigPhotoMetadata(),
					},
			};
		}
		public static RigStorageCapacity ConstructRigStorageCapacity()
		{
			return new RigStorageCapacity
			{
				StorageType = (RigStorageType)0,
				Name = "Default Name",
				MaximumVolume = null, 
				MaximumMass = null, 
			};
		}
		public static StationKeepingSystem ConstructStationKeepingSystem()
		{
			return new StationKeepingSystem
			{
				Modes = new List<StationKeepingMode>
					{
						(StationKeepingMode)0,
					},
				DynamicPositioningClass = (DynamicPositioningClass?)0,
				ThrusterCount = null, 
				MooringLineCount = null, 
				MaximumMooringLineTension = null, 
			};
		}
		public static AreaOfUse ConstructAreaOfUse()
		{
			return new AreaOfUse
			{
				Name = "Default Name",
				Scope = "Default Scope",
				Bounds = ConstructGeographicBoundingBox(),
			};
		}
		public static AuthorityIdentifier ConstructAuthorityIdentifier()
		{
			return new AuthorityIdentifier
			{
				Authority = "Default Authority",
				Code = "Default Code",
				Version = "Default Version",
				Uri = "Default Uri",
			};
		}
		public static CatalogProvenance ConstructCatalogProvenance()
		{
			return new CatalogProvenance
			{
				Source = "Default Source",
				MatchStatus = (CatalogMatchStatus)0,
				SourceVersion = "Default SourceVersion",
				SourceCode = "Default SourceCode",
				LegacyId = null, 
				Notes = "Default Notes",
				CanonicalId = null, 
				IsLegacyCombinedDefinition = false, 
			};
		}
		public static ProjectionDefinition ConstructProjectionDefinition()
		{
			return new ProjectionDefinition
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Aliases = new List<string>
					{
						"",
					},
				Identifier = ConstructAuthorityIdentifier(),
				BaseGeographicCrs = ConstructGeographicCrsReference(),
				MethodId = new Guid(),
				ConversionIdentifier = ConstructAuthorityIdentifier(),
				ConversionName = "Default ConversionName",
				Parameters = new List<ProjectionParameterValue>
					{
						ConstructProjectionParameterValue(),
					},
				CoordinateSystem = ConstructProjectedCoordinateSystem(),
				AreaOfUse = ConstructAreaOfUse(),
				IsBuiltIn = false, 
				IsDeprecated = false, 
				IsSuperseded = false, 
				SupersededByIdentifiers = new List<AuthorityIdentifier>
					{
						ConstructAuthorityIdentifier(),
					},
				Remarks = "Default Remarks",
				InformationSource = "Default InformationSource",
				RevisionDate = DateTimeOffset.UtcNow,
				Provenance = ConstructCatalogProvenance(),
				CreatedUtc = DateTimeOffset.UtcNow,
				ModifiedUtc = DateTimeOffset.UtcNow,
				RuntimeStatus = (ProjectionRuntimeStatus)0,
				RuntimeMessage = "Default RuntimeMessage",
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static ProjectionMethod ConstructProjectionMethod()
		{
			return new ProjectionMethod
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				ProjName = "Default ProjName",
				IsBuiltIn = false, 
				IsDeprecated = false, 
				IsCustomDefinitionAllowed = false, 
				CustomDefinitionRestrictionCode = "Default CustomDefinitionRestrictionCode",
				CustomDefinitionRestrictionMessage = "Default CustomDefinitionRestrictionMessage",
				Aliases = new List<string>
					{
						"",
					},
				Parameters = new List<ProjectionParameterDefinition>
					{
						ConstructProjectionParameterDefinition(),
					},
				Provenance = ConstructCatalogProvenance(),
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static CatalogSearchRequest ConstructCatalogSearchRequest()
		{
			return new CatalogSearchRequest
			{
				Query = "Default Query",
				Authority = "Default Authority",
				Code = "Default Code",
				CatalogStatus = (CatalogEntryStatus)0,
				IncludeLegacy = false, 
				IncludeDeprecated = false, 
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static CoordinateSystemAxis ConstructCoordinateSystemAxis()
		{
			return new CoordinateSystemAxis
			{
				Name = "Default Name",
				Abbreviation = "Default Abbreviation",
				Order = 0, 
				Direction = (AxisDirection)0,
				Unit = ConstructAuthorityIdentifier(),
				UnitName = "Default UnitName",
				UnitToMetre = 0.0, 
			};
		}
		public static CoordinateSystemAxisInput ConstructCoordinateSystemAxisInput()
		{
			return new CoordinateSystemAxisInput
			{
				Name = "Default Name",
				Abbreviation = "Default Abbreviation",
				Order = 0, 
				Direction = (AxisDirection)0,
			};
		}
		public static CreateProjectionDefinitionRequest ConstructCreateProjectionDefinitionRequest()
		{
			return new CreateProjectionDefinitionRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				Aliases = new List<string>
					{
						"",
					},
				Identifier = ConstructAuthorityIdentifier(),
				BaseGeographicCrs = ConstructGeographicCrsInput(),
				MethodId = new Guid(),
				ConversionName = "Default ConversionName",
				Parameters = new List<ProjectionParameterInput>
					{
						ConstructProjectionParameterInput(),
					},
				CoordinateSystem = ConstructProjectedCoordinateSystemInput(),
				AreaOfUse = ConstructAreaOfUse(),
				Remarks = "Default Remarks",
				LegacyId = null, 
				LegacySource = "Default LegacySource",
			};
		}
		public static CrsReference ConstructCrsReference()
		{
			return new CrsReference
			{
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
			};
		}
		public static EarthCartographicProjectionServiceInfo ConstructEarthCartographicProjectionServiceInfo()
		{
			return new EarthCartographicProjectionServiceInfo
			{
				Service = "Default Service",
				Version = "Default Version",
				EpsgDatasetVersion = "Default EpsgDatasetVersion",
				CoordinateConvention = "Default CoordinateConvention",
				CalculationBehavior = "Default CalculationBehavior",
				CatalogBehavior = "Default CatalogBehavior",
				ExternalResourceBehavior = "Default ExternalResourceBehavior",
				McpErrorBehavior = "Default McpErrorBehavior",
				MaximumPositionsPerRequest = 0, 
				McpMaximumPositionsPerRequest = 0, 
				McpBatchLimitBehavior = "Default McpBatchLimitBehavior",
				ProjectionMethodCount = 0, 
				ProjectionDefinitionCount = 0, 
			};
		}
		public static ForwardProjectionPosition ConstructForwardProjectionPosition()
		{
			return new ForwardProjectionPosition
			{
				PositionIndex = 0, 
				GeographicCoordinate = ConstructGeographicCoordinate(),
				ProjectedCoordinate = ConstructProjectedCoordinate(),
				GridConvergence = null, 
			};
		}
		public static ForwardProjectionRequest ConstructForwardProjectionRequest()
		{
			return new ForwardProjectionRequest
			{
				ProjectionDefinitionId = new Guid(),
				ApplicabilityPolicy = (ApplicabilityPolicy)0,
				Positions = new List<GeographicCoordinate>
					{
						ConstructGeographicCoordinate(),
					},
			};
		}
		public static ForwardProjectionResponse ConstructForwardProjectionResponse()
		{
			return new ForwardProjectionResponse
			{
				ProjectionDefinition = ConstructProjectionDefinitionReference(),
				GeographicCoordinateReferenceSystem = ConstructCrsReference(),
				ProjectedCoordinateReferenceSystem = ConstructCrsReference(),
				ApiAxisConvention = "Default ApiAxisConvention",
				GridConvergenceConvention = "Default GridConvergenceConvention",
				Applicability = (ProjectionApplicability)0,
				Positions = new List<ForwardProjectionPosition>
					{
						ConstructForwardProjectionPosition(),
					},
				Warnings = new List<ServiceWarning>
					{
						ConstructServiceWarning(),
					},
			};
		}
		public static GeodeticDatumReference ConstructGeodeticDatumReference()
		{
			return new GeodeticDatumReference
			{
				EarthGeodesyDatumId = null, 
				Identifier = ConstructAuthorityIdentifier(),
				Name = "Default Name",
				ExpectedModifiedUtc = DateTimeOffset.UtcNow,
			};
		}
		public static GeographicBoundingBox ConstructGeographicBoundingBox()
		{
			return new GeographicBoundingBox
			{
				SouthLatitude = 0.0, 
				NorthLatitude = 0.0, 
				WestLongitude = 0.0, 
				EastLongitude = 0.0, 
			};
		}
		public static GeographicCoordinate ConstructGeographicCoordinate()
		{
			return new GeographicCoordinate
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
			};
		}
		public static GeographicCrsInput ConstructGeographicCrsInput()
		{
			return new GeographicCrsInput
			{
				Name = "Default Name",
				Datum = ConstructProjectionDatumInput(),
			};
		}
		public static GeographicCrsReference ConstructGeographicCrsReference()
		{
			return new GeographicCrsReference
			{
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				Datum = ConstructGeodeticDatumReference(),
			};
		}
		public static InverseProjectionPosition ConstructInverseProjectionPosition()
		{
			return new InverseProjectionPosition
			{
				PositionIndex = 0, 
				ProjectedCoordinate = ConstructProjectedCoordinate(),
				GeographicCoordinate = ConstructGeographicCoordinate(),
				GridConvergence = null, 
			};
		}
		public static InverseProjectionRequest ConstructInverseProjectionRequest()
		{
			return new InverseProjectionRequest
			{
				ProjectionDefinitionId = new Guid(),
				ApplicabilityPolicy = (ApplicabilityPolicy)0,
				Positions = new List<ProjectedCoordinate>
					{
						ConstructProjectedCoordinate(),
					},
			};
		}
		public static InverseProjectionResponse ConstructInverseProjectionResponse()
		{
			return new InverseProjectionResponse
			{
				ProjectionDefinition = ConstructProjectionDefinitionReference(),
				GeographicCoordinateReferenceSystem = ConstructCrsReference(),
				ProjectedCoordinateReferenceSystem = ConstructCrsReference(),
				ApiAxisConvention = "Default ApiAxisConvention",
				GridConvergenceConvention = "Default GridConvergenceConvention",
				Applicability = (ProjectionApplicability)0,
				Positions = new List<InverseProjectionPosition>
					{
						ConstructInverseProjectionPosition(),
					},
				Warnings = new List<ServiceWarning>
					{
						ConstructServiceWarning(),
					},
			};
		}
		public static ProjectedCoordinate ConstructProjectedCoordinate()
		{
			return new ProjectedCoordinate
			{
				Easting = 0.0, 
				Northing = 0.0, 
			};
		}
		public static ProjectedCoordinateSystem ConstructProjectedCoordinateSystem()
		{
			return new ProjectedCoordinateSystem
			{
				Axes = new List<CoordinateSystemAxis>
					{
						ConstructCoordinateSystemAxis(),
					},
				ApiConvention = "Default ApiConvention",
			};
		}
		public static ProjectedCoordinateSystemInput ConstructProjectedCoordinateSystemInput()
		{
			return new ProjectedCoordinateSystemInput
			{
				Axes = new List<CoordinateSystemAxisInput>
					{
						ConstructCoordinateSystemAxisInput(),
					},
			};
		}
		public static ProjectionDatumInput ConstructProjectionDatumInput()
		{
			return new ProjectionDatumInput
			{
				EarthGeodesyDatumId = new Guid(),
				ExpectedModifiedUtc = DateTimeOffset.UtcNow,
			};
		}
		public static ProjectionDefinitionReference ConstructProjectionDefinitionReference()
		{
			return new ProjectionDefinitionReference
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static ProjectionDefinitionSearchRequest ConstructProjectionDefinitionSearchRequest()
		{
			return new ProjectionDefinitionSearchRequest
			{
				Query = "Default Query",
				Authority = "Default Authority",
				Code = "Default Code",
				CatalogStatus = (CatalogStatusFilter)0,
				IncludeDeprecated = false, 
				Offset = 0, 
				Limit = 0, 
				MethodId = null, 
				EarthGeodesyDatumId = null, 
				AreaQuery = "Default AreaQuery",
				ContainsPosition = ConstructGeographicCoordinate(),
				ContainsBounds = ConstructGeographicBoundingBox(),
				RuntimeStatus = (ProjectionRuntimeStatus)0,
				IsSuperseded = null, 
			};
		}
		public static ProjectionDefinitionSummary ConstructProjectionDefinitionSummary()
		{
			return new ProjectionDefinitionSummary
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Aliases = new List<string>
					{
						"",
					},
				Identifier = ConstructAuthorityIdentifier(),
				AreaOfUseName = "Default AreaOfUseName",
				AreaOfUseScope = "Default AreaOfUseScope",
				BaseGeographicCrsName = "Default BaseGeographicCrsName",
				DatumName = "Default DatumName",
				IsBuiltIn = false, 
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static ProjectionParameterDefinition ConstructProjectionParameterDefinition()
		{
			return new ProjectionParameterDefinition
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				Quantity = (ProjectionParameterQuantity)0,
				IsRequired = false, 
				MinimumValue = null, 
				MaximumValue = null, 
				Description = "Default Description",
			};
		}
		public static ProjectionParameterInput ConstructProjectionParameterInput()
		{
			return new ProjectionParameterInput
			{
				ParameterId = new Guid(),
				Value = 0.0, 
			};
		}
		public static ProjectionParameterValue ConstructProjectionParameterValue()
		{
			return new ProjectionParameterValue
			{
				ParameterId = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				Quantity = (ProjectionParameterQuantity)0,
				Value = 0.0, 
				OriginalValue = null, 
				OriginalUnit = ConstructAuthorityIdentifier(),
				OriginalUnitName = "Default OriginalUnitName",
			};
		}
		public static ServiceWarning ConstructServiceWarning()
		{
			return new ServiceWarning
			{
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static UpdateProjectionDefinitionRequest ConstructUpdateProjectionDefinitionRequest()
		{
			return new UpdateProjectionDefinitionRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				Aliases = new List<string>
					{
						"",
					},
				Identifier = ConstructAuthorityIdentifier(),
				BaseGeographicCrs = ConstructGeographicCrsInput(),
				MethodId = new Guid(),
				ConversionName = "Default ConversionName",
				Parameters = new List<ProjectionParameterInput>
					{
						ConstructProjectionParameterInput(),
					},
				CoordinateSystem = ConstructProjectedCoordinateSystemInput(),
				AreaOfUse = ConstructAreaOfUse(),
				Remarks = "Default Remarks",
				LegacyId = null, 
				LegacySource = "Default LegacySource",
			};
		}
		public static UsageStatisticsEarthCartographicProjection ConstructUsageStatisticsEarthCartographicProjection()
		{
			return new UsageStatisticsEarthCartographicProjection
			{
				RestForward = 0, 
				RestInverse = 0, 
				McpForward = 0, 
				McpInverse = 0, 
				PositionsProjected = 0, 
				Failures = 0, 
				CatalogReads = 0, 
				CatalogWrites = 0, 
			};
		}
		public static Microsoft_AspNetCore_Mvc_ProblemDetails ConstructMicrosoft_AspNetCore_Mvc_ProblemDetails()
		{
			return new Microsoft_AspNetCore_Mvc_ProblemDetails
			{
				Type = "Default Type",
				Title = "Default Title",
				Status = null, 
				Detail = "Default Detail",
				Instance = "Default Instance",
			};
		}
		public static CatalogReference ConstructCatalogReference()
		{
			return new CatalogReference
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
			};
		}
		public static CatalogSearchItem_GeodeticDatumSummary ConstructCatalogSearchItem_GeodeticDatumSummary()
		{
			return new CatalogSearchItem_GeodeticDatumSummary
			{
				Value = ConstructGeodeticDatumSummary(),
				MatchScore = 0.0, 
				MatchReason = "Default MatchReason",
			};
		}
		public static CatalogSearchItem_GeodeticTransformationSummary ConstructCatalogSearchItem_GeodeticTransformationSummary()
		{
			return new CatalogSearchItem_GeodeticTransformationSummary
			{
				Value = ConstructGeodeticTransformationSummary(),
				MatchScore = 0.0, 
				MatchReason = "Default MatchReason",
			};
		}
		public static CatalogSearchItem_ReferenceEllipsoidSummary ConstructCatalogSearchItem_ReferenceEllipsoidSummary()
		{
			return new CatalogSearchItem_ReferenceEllipsoidSummary
			{
				Value = ConstructReferenceEllipsoidSummary(),
				MatchScore = 0.0, 
				MatchReason = "Default MatchReason",
			};
		}
		public static CatalogSearchResult_GeodeticDatumSummary ConstructCatalogSearchResult_GeodeticDatumSummary()
		{
			return new CatalogSearchResult_GeodeticDatumSummary
			{
				TotalCount = 0, 
				Offset = 0, 
				Limit = 0, 
				ReturnedCount = 0, 
				Items = new List<CatalogSearchItem_GeodeticDatumSummary>
					{
						ConstructCatalogSearchItem_GeodeticDatumSummary(),
					},
			};
		}
		public static CatalogSearchResult_GeodeticTransformationSummary ConstructCatalogSearchResult_GeodeticTransformationSummary()
		{
			return new CatalogSearchResult_GeodeticTransformationSummary
			{
				TotalCount = 0, 
				Offset = 0, 
				Limit = 0, 
				ReturnedCount = 0, 
				Items = new List<CatalogSearchItem_GeodeticTransformationSummary>
					{
						ConstructCatalogSearchItem_GeodeticTransformationSummary(),
					},
			};
		}
		public static CatalogSearchResult_ReferenceEllipsoidSummary ConstructCatalogSearchResult_ReferenceEllipsoidSummary()
		{
			return new CatalogSearchResult_ReferenceEllipsoidSummary
			{
				TotalCount = 0, 
				Offset = 0, 
				Limit = 0, 
				ReturnedCount = 0, 
				Items = new List<CatalogSearchItem_ReferenceEllipsoidSummary>
					{
						ConstructCatalogSearchItem_ReferenceEllipsoidSummary(),
					},
			};
		}
		public static CoordinateOperationParameterValue ConstructCoordinateOperationParameterValue()
		{
			return new CoordinateOperationParameterValue
			{
				Identifier = ConstructAuthorityIdentifier(),
				Name = "Default Name",
				Value = null, 
				FileReference = "Default FileReference",
				Unit = ConstructAuthorityIdentifier(),
				UnitName = "Default UnitName",
			};
		}
		public static CoordinateReferenceSystemReference ConstructCoordinateReferenceSystemReference()
		{
			return new CoordinateReferenceSystemReference
			{
				Identifier = ConstructAuthorityIdentifier(),
				Name = "Default Name",
				Domain = (CoordinateOperationDomain)0,
			};
		}
		public static CreateGeodeticDatumRequest ConstructCreateGeodeticDatumRequest()
		{
			return new CreateGeodeticDatumRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				ReferenceEllipsoidId = new Guid(),
				Identifier = ConstructAuthorityIdentifier(),
				ReferenceObjectType = (GeodeticReferenceObjectType)0,
				IsDeprecated = false, 
				PrimeMeridianName = "Default PrimeMeridianName",
				PrimeMeridianIdentifier = ConstructAuthorityIdentifier(),
				PrimeMeridianLongitude = 0.0, 
				Origin = "Default Origin",
				PublicationDate = "Default PublicationDate",
				RealizationEpoch = "Default RealizationEpoch",
				FrameReferenceEpoch = null, 
				AnchorEpoch = null, 
				ConventionalReferenceSystem = "Default ConventionalReferenceSystem",
				RealizationMethod = "Default RealizationMethod",
				EnsembleAccuracy = null, 
				MemberDatumIds = new List<Guid>
					{
						new Guid(),
					},
				Usage = new List<GeodeticUsage>
					{
						ConstructGeodeticUsage(),
					},
				Remarks = "Default Remarks",
			};
		}
		public static CreateGeodeticTransformationRequest ConstructCreateGeodeticTransformationRequest()
		{
			return new CreateGeodeticTransformationRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				Method = (GeodeticTransformationMethod)0,
				TranslationX = 0.0, 
				TranslationY = 0.0, 
				TranslationZ = 0.0, 
				RotationX = 0.0, 
				RotationY = 0.0, 
				RotationZ = 0.0, 
				ScaleDifference = 0.0, 
				Accuracy = null, 
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
				Identifier = ConstructAuthorityIdentifier(),
			};
		}
		public static CreateReferenceEllipsoidRequest ConstructCreateReferenceEllipsoidRequest()
		{
			return new CreateReferenceEllipsoidRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				SemiMajorAxis = 0.0, 
				InverseFlattening = 0.0, 
			};
		}
		public static DatumReference ConstructDatumReference()
		{
			return new DatumReference
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				ReferenceEllipsoid = ConstructEllipsoidReference(),
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static DatumTransformationConnection ConstructDatumTransformationConnection()
		{
			return new DatumTransformationConnection
			{
				TransformationId = new Guid(),
				TransformationName = "Default TransformationName",
				Identifier = ConstructAuthorityIdentifier(),
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				IsReversible = false, 
				CanExecuteForward = null, 
				CanExecuteReverse = null, 
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
			};
		}
		public static EarthGeodesyServiceInfo ConstructEarthGeodesyServiceInfo()
		{
			return new EarthGeodesyServiceInfo
			{
				Service = "Default Service",
				Version = "Default Version",
				EpsgDatasetVersion = "Default EpsgDatasetVersion",
				CoordinateConvention = "Default CoordinateConvention",
				CalculationBehavior = "Default CalculationBehavior",
				InitializationBehavior = "Default InitializationBehavior",
				PerformanceGuidance = "Default PerformanceGuidance",
				ExternalResourceBehavior = "Default ExternalResourceBehavior",
				McpErrorBehavior = "Default McpErrorBehavior",
				CatalogBehavior = "Default CatalogBehavior",
				MaximumPositionsPerRequest = 0, 
				McpMaximumPositionsPerRequest = 0, 
				MaximumTransformationPathLength = 0, 
				ReferenceEllipsoidCount = 0, 
				GeodeticDatumCount = 0, 
				TransformationCount = 0, 
			};
		}
		public static EllipsoidReference ConstructEllipsoidReference()
		{
			return new EllipsoidReference
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				SemiMajorAxis = 0.0, 
				InverseFlattening = 0.0, 
			};
		}
		public static GeodeticDatumSummary ConstructGeodeticDatumSummary()
		{
			return new GeodeticDatumSummary
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				ReferenceEllipsoid = ConstructEllipsoidReference(),
				CatalogStatus = (CatalogEntryStatus)0,
				IsDefault = false, 
				ReferenceObjectType = (GeodeticReferenceObjectType)0,
				IsDeprecated = false, 
				IsSuperseded = false, 
				PrimeMeridianName = "Default PrimeMeridianName",
				PrimeMeridianIdentifier = ConstructAuthorityIdentifier(),
				PrimeMeridianLongitude = 0.0, 
				Origin = "Default Origin",
				PublicationDate = "Default PublicationDate",
				RealizationEpoch = "Default RealizationEpoch",
				FrameReferenceEpoch = null, 
				AnchorEpoch = null, 
				ConventionalReferenceSystem = "Default ConventionalReferenceSystem",
				RealizationMethod = "Default RealizationMethod",
				EnsembleAccuracy = null, 
				MemberDatumIds = new List<Guid>
					{
						new Guid(),
					},
				Usage = new List<GeodeticUsage>
					{
						ConstructGeodeticUsage(),
					},
				Remarks = "Default Remarks",
				InformationSource = "Default InformationSource",
				RevisionDate = DateTimeOffset.UtcNow,
				Provenance = ConstructCatalogProvenance(),
			};
		}
		public static GeodeticPosition ConstructGeodeticPosition()
		{
			return new GeodeticPosition
			{
				Latitude = 0.0, 
				Longitude = 0.0, 
				Depth = 0.0, 
				CoordinateEpochUtc = DateTimeOffset.UtcNow,
			};
		}
		public static GeodeticTransformation ConstructGeodeticTransformation()
		{
			return new GeodeticTransformation
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				Method = (GeodeticTransformationMethod)0,
				MethodName = "Default MethodName",
				MethodIdentifier = ConstructAuthorityIdentifier(),
				SourceCrs = ConstructCoordinateReferenceSystemReference(),
				TargetCrs = ConstructCoordinateReferenceSystemReference(),
				IsReversible = false, 
				ComponentOperationIdentifiers = new List<AuthorityIdentifier>
					{
						ConstructAuthorityIdentifier(),
					},
				Parameters = new List<CoordinateOperationParameterValue>
					{
						ConstructCoordinateOperationParameterValue(),
					},
				RequiresExternalResource = false, 
				RequiresCoordinateEpoch = false, 
				TranslationX = 0.0, 
				TranslationY = 0.0, 
				TranslationZ = 0.0, 
				RotationX = 0.0, 
				RotationY = 0.0, 
				RotationZ = 0.0, 
				ScaleDifference = 0.0, 
				Accuracy = null, 
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
				Identifier = ConstructAuthorityIdentifier(),
				IsBuiltIn = false, 
				IsDeprecated = false, 
				IsSuperseded = false, 
				Provenance = ConstructCatalogProvenance(),
				CreatedUtc = DateTimeOffset.UtcNow,
				ModifiedUtc = DateTimeOffset.UtcNow,
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static GeodeticTransformationSummary ConstructGeodeticTransformationSummary()
		{
			return new GeodeticTransformationSummary
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				SourceDatum = ConstructCatalogReference(),
				TargetDatum = ConstructCatalogReference(),
				Method = (GeodeticTransformationMethod)0,
				MethodName = "Default MethodName",
				MethodIdentifier = ConstructAuthorityIdentifier(),
				SourceCrs = ConstructCoordinateReferenceSystemReference(),
				TargetCrs = ConstructCoordinateReferenceSystemReference(),
				IsReversible = false, 
				ComponentOperationIdentifiers = new List<AuthorityIdentifier>
					{
						ConstructAuthorityIdentifier(),
					},
				Parameters = new List<CoordinateOperationParameterValue>
					{
						ConstructCoordinateOperationParameterValue(),
					},
				RequiresExternalResource = false, 
				RequiresCoordinateEpoch = false, 
				TranslationX = 0.0, 
				TranslationY = 0.0, 
				TranslationZ = 0.0, 
				RotationX = 0.0, 
				RotationY = 0.0, 
				RotationZ = 0.0, 
				ScaleDifference = 0.0, 
				Accuracy = null, 
				AccuracyMeaning = "Default AccuracyMeaning",
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
				CatalogStatus = (CatalogEntryStatus)0,
				Provenance = ConstructCatalogProvenance(),
			};
		}
		public static GeodeticUsage ConstructGeodeticUsage()
		{
			return new GeodeticUsage
			{
				Scope = "Default Scope",
				Extent = "Default Extent",
			};
		}
		public static ReferenceEllipsoid ConstructReferenceEllipsoid()
		{
			return new ReferenceEllipsoid
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				SemiMajorAxis = 0.0, 
				InverseFlattening = 0.0, 
				IsBuiltIn = false, 
				IsDefault = false, 
				Provenance = ConstructCatalogProvenance(),
				CreatedUtc = DateTimeOffset.UtcNow,
				ModifiedUtc = DateTimeOffset.UtcNow,
				Aliases = new List<string>
					{
						"",
					},
				IsDeprecated = false, 
				IsSuperseded = false, 
				Remarks = "Default Remarks",
				InformationSource = "Default InformationSource",
				RevisionDate = DateTimeOffset.UtcNow,
				SourceUnit = ConstructAuthorityIdentifier(),
				CatalogStatus = (CatalogEntryStatus)0,
				SemiMinorAxis = 0.0, 
			};
		}
		public static ReferenceEllipsoidSummary ConstructReferenceEllipsoidSummary()
		{
			return new ReferenceEllipsoidSummary
			{
				Id = new Guid(),
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				SemiMajorAxis = 0.0, 
				InverseFlattening = 0.0, 
				SemiMinorAxis = 0.0, 
				SourceUnit = ConstructAuthorityIdentifier(),
				CatalogStatus = (CatalogEntryStatus)0,
				IsDefault = false, 
				IsDeprecated = false, 
				IsSuperseded = false, 
				Remarks = "Default Remarks",
				InformationSource = "Default InformationSource",
				RevisionDate = DateTimeOffset.UtcNow,
				Provenance = ConstructCatalogProvenance(),
			};
		}
		public static ResolveTransformationPathsRequest ConstructResolveTransformationPathsRequest()
		{
			return new ResolveTransformationPathsRequest
			{
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				Positions = new List<GeodeticPosition>
					{
						ConstructGeodeticPosition(),
					},
				MaximumCandidates = 0, 
			};
		}
		public static ResolveTransformationPathsResponse ConstructResolveTransformationPathsResponse()
		{
			return new ResolveTransformationPathsResponse
			{
				SourceDatum = ConstructDatumReference(),
				TargetDatum = ConstructDatumReference(),
				IsAmbiguous = false, 
				SelectionGuidance = "Default SelectionGuidance",
				Candidates = new List<TransformationPathCandidate>
					{
						ConstructTransformationPathCandidate(),
					},
			};
		}
		public static TransformCoordinatesRequest ConstructTransformCoordinatesRequest()
		{
			return new TransformCoordinatesRequest
			{
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				Positions = new List<GeodeticPosition>
					{
						ConstructGeodeticPosition(),
					},
				SelectionPolicy = (TransformationSelectionPolicy)0,
				TransformationPathIds = new List<Guid>
					{
						new Guid(),
					},
				SelectionToken = "Default SelectionToken",
				ApplicabilityPolicy = (ApplicabilityPolicy)0,
				DepthPolicy = (DepthTransformationPolicy)0,
			};
		}
		public static TransformCoordinatesResponse ConstructTransformCoordinatesResponse()
		{
			return new TransformCoordinatesResponse
			{
				SourceDatum = ConstructCatalogReference(),
				TargetDatum = ConstructCatalogReference(),
				TransformationPath = new List<TransformationReference>
					{
						ConstructTransformationReference(),
					},
				Positions = new List<GeodeticPosition>
					{
						ConstructGeodeticPosition(),
					},
				Applicability = (TransformationApplicability)0,
				DepthReferenceChanged = false, 
				DepthChanges = new List<double>
					{
						0.0, 
					},
				MaximumAbsoluteDepthChange = 0.0, 
				Warnings = new List<ServiceWarning>
					{
						ConstructServiceWarning(),
					},
			};
		}
		public static TransformationPathCandidate ConstructTransformationPathCandidate()
		{
			return new TransformationPathCandidate
			{
				Rank = 0, 
				IsRecommended = false, 
				RecommendationReason = "Default RecommendationReason",
				Applicability = (TransformationApplicability)0,
				ApplicabilityMessage = "Default ApplicabilityMessage",
				IsExecutable = false, 
				ExecutionIssues = new List<ServiceWarning>
					{
						ConstructServiceWarning(),
					},
				SelectionToken = "Default SelectionToken",
				CombinedAccuracy = null, 
				AccuracyMeaning = "Default AccuracyMeaning",
				Operations = new List<TransformationReference>
					{
						ConstructTransformationReference(),
					},
			};
		}
		public static TransformationReference ConstructTransformationReference()
		{
			return new TransformationReference
			{
				Id = new Guid(),
				Name = "Default Name",
				Identifier = ConstructAuthorityIdentifier(),
				Method = (GeodeticTransformationMethod)0,
				MethodName = "Default MethodName",
				MethodIdentifier = ConstructAuthorityIdentifier(),
				SourceCrs = ConstructCoordinateReferenceSystemReference(),
				TargetCrs = ConstructCoordinateReferenceSystemReference(),
				ComponentOperationIdentifiers = new List<AuthorityIdentifier>
					{
						ConstructAuthorityIdentifier(),
					},
				RequiresExternalResource = false, 
				RequiresCoordinateEpoch = false, 
				Accuracy = null, 
				AccuracyMeaning = "Default AccuracyMeaning",
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
				AppliedInReverse = false, 
				CatalogStatus = (CatalogEntryStatus)0,
			};
		}
		public static UpdateGeodeticDatumRequest ConstructUpdateGeodeticDatumRequest()
		{
			return new UpdateGeodeticDatumRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				ReferenceEllipsoidId = new Guid(),
				Identifier = ConstructAuthorityIdentifier(),
				ReferenceObjectType = (GeodeticReferenceObjectType)0,
				IsDeprecated = false, 
				PrimeMeridianName = "Default PrimeMeridianName",
				PrimeMeridianIdentifier = ConstructAuthorityIdentifier(),
				PrimeMeridianLongitude = 0.0, 
				Origin = "Default Origin",
				PublicationDate = "Default PublicationDate",
				RealizationEpoch = "Default RealizationEpoch",
				FrameReferenceEpoch = null, 
				AnchorEpoch = null, 
				ConventionalReferenceSystem = "Default ConventionalReferenceSystem",
				RealizationMethod = "Default RealizationMethod",
				EnsembleAccuracy = null, 
				MemberDatumIds = new List<Guid>
					{
						new Guid(),
					},
				Usage = new List<GeodeticUsage>
					{
						ConstructGeodeticUsage(),
					},
				Remarks = "Default Remarks",
			};
		}
		public static UpdateGeodeticTransformationRequest ConstructUpdateGeodeticTransformationRequest()
		{
			return new UpdateGeodeticTransformationRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				SourceDatumId = new Guid(),
				TargetDatumId = new Guid(),
				Method = (GeodeticTransformationMethod)0,
				TranslationX = 0.0, 
				TranslationY = 0.0, 
				TranslationZ = 0.0, 
				RotationX = 0.0, 
				RotationY = 0.0, 
				RotationZ = 0.0, 
				ScaleDifference = 0.0, 
				Accuracy = null, 
				AreaOfUse = "Default AreaOfUse",
				AreaOfUseBounds = ConstructGeographicBoundingBox(),
				Identifier = ConstructAuthorityIdentifier(),
			};
		}
		public static UpdateReferenceEllipsoidRequest ConstructUpdateReferenceEllipsoidRequest()
		{
			return new UpdateReferenceEllipsoidRequest
			{
				Name = "Default Name",
				Description = "Default Description",
				Identifier = ConstructAuthorityIdentifier(),
				SemiMajorAxis = 0.0, 
				InverseFlattening = 0.0, 
			};
		}
		public static UsageStatisticsEarthGeodesy ConstructUsageStatisticsEarthGeodesy()
		{
			return new UsageStatisticsEarthGeodesy
			{
				RestTransforms = 0, 
				MCPTransforms = 0, 
				FailedRequests = 0, 
				PositionsTransformed = 0, 
				CatalogReads = 0, 
				CatalogWrites = 0, 
			};
		}
		public static WellBatchCatalogDependencies ConstructWellBatchCatalogDependencies()
		{
			return new WellBatchCatalogDependencies
			{
				Identities = new List<WellIdentity>
					{
						ConstructWellIdentity(),
					},
				FeatureCategories = new List<WellFeatureCategory>
					{
						ConstructWellFeatureCategory(),
					},
			};
		}
		public static WellBatchCatalogMapping ConstructWellBatchCatalogMapping()
		{
			return new WellBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static WellBatchError ConstructWellBatchError()
		{
			return new WellBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellBatchErrorEnvelope ConstructWellBatchErrorEnvelope()
		{
			return new WellBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<WellBatchError>
					{
						ConstructWellBatchError(),
					},
			};
		}
		public static WellBatchExportDocument ConstructWellBatchExportDocument()
		{
			return new WellBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructWellBatchCatalogDependencies(),
				Wells = new List<Well>
					{
						ConstructWell(),
					},
			};
		}
		public static WellBatchExportRequest ConstructWellBatchExportRequest()
		{
			return new WellBatchExportRequest
			{
				Scope = (WellBatchExportScope)0,
				WellIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellBatchRestoreRequest ConstructWellBatchRestoreRequest()
		{
			return new WellBatchRestoreRequest
			{
				ConflictPolicy = (WellBatchRestoreConflictPolicy)0,
				CatalogPolicy = (WellBatchCatalogRestorePolicy)0,
				Document = ConstructWellBatchExportDocument(),
			};
		}
		public static WellBatchRestoreResponse ConstructWellBatchRestoreResponse()
		{
			return new WellBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				CreatedCatalogOptionCount = 0, 
				CatalogMappings = new List<WellBatchCatalogMapping>
					{
						ConstructWellBatchCatalogMapping(),
					},
				WellIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellDetailsUpdate ConstructWellDetailsUpdate()
		{
			return new WellDetailsUpdate
			{
				Name = "Default Name",
				Description = "Default Description",
			};
		}
		public static WellExternalReferenceAuditRequest ConstructWellExternalReferenceAuditRequest()
		{
			return new WellExternalReferenceAuditRequest
			{
				Scope = (WellExternalReferenceAuditScope)0,
				WellIDs = new List<Guid>
					{
						new Guid(),
					},
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static WellExternalReferenceAuditResult ConstructWellExternalReferenceAuditResult()
		{
			return new WellExternalReferenceAuditResult
			{
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Total = 0, 
				Offset = 0, 
				Limit = 0, 
				ValidCount = 0, 
				InvalidCount = 0, 
				UnavailableCount = 0, 
				Items = new List<WellExternalReferenceValidation>
					{
						ConstructWellExternalReferenceValidation(),
					},
			};
		}
		public static WellExternalReferenceIssue ConstructWellExternalReferenceIssue()
		{
			return new WellExternalReferenceIssue
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static WellExternalReferenceValidation ConstructWellExternalReferenceValidation()
		{
			return new WellExternalReferenceValidation
			{
				WellID = new Guid(),
				ClusterID = null, 
				SlotID = null, 
				ClusterExists = null, 
				SlotBelongsToCluster = null, 
				Status = (WellExternalReferenceValidationStatus)0,
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Issues = new List<WellExternalReferenceIssue>
					{
						ConstructWellExternalReferenceIssue(),
					},
			};
		}
		public static WellFeatureAssignment ConstructWellFeatureAssignment()
		{
			return new WellFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellFeatureCategory ConstructWellFeatureCategory()
		{
			return new WellFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<WellFeatureOption>
					{
						ConstructWellFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellFeatureOption ConstructWellFeatureOption()
		{
			return new WellFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static WellIdentity ConstructWellIdentity()
		{
			return new WellIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static WellIdentityAssignment ConstructWellIdentityAssignment()
		{
			return new WellIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static WellLocationUpdate ConstructWellLocationUpdate()
		{
			return new WellLocationUpdate
			{
				ClusterID = null, 
				SlotID = null, 
				IsSingleWell = false, 
			};
		}
		public static WellMutationError ConstructWellMutationError()
		{
			return new WellMutationError
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
				ReferencingWellIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static WellMutationErrorEnvelope ConstructWellMutationErrorEnvelope()
		{
			return new WellMutationErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<WellMutationError>
					{
						ConstructWellMutationError(),
					},
			};
		}
		public static WellSearchResult ConstructWellSearchResult()
		{
			return new WellSearchResult
			{
				Items = new List<Well>
					{
						ConstructWell(),
					},
				Total = 0, 
				Offset = 0, 
				Limit = 0, 
			};
		}
		public static SurveyInstrumentBatchCatalogDependencies ConstructSurveyInstrumentBatchCatalogDependencies()
		{
			return new SurveyInstrumentBatchCatalogDependencies
			{
				ErrorSourceTemplates = new List<ErrorSource>
					{
						ConstructErrorSource(),
					},
				Identities = new List<SurveyInstrumentIdentity>
					{
						ConstructSurveyInstrumentIdentity(),
					},
				FeatureCategories = new List<SurveyInstrumentFeatureCategory>
					{
						ConstructSurveyInstrumentFeatureCategory(),
					},
			};
		}
		public static SurveyInstrumentBatchError ConstructSurveyInstrumentBatchError()
		{
			return new SurveyInstrumentBatchError
			{
				PositionIndex = null, 
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static SurveyInstrumentBatchErrorEnvelope ConstructSurveyInstrumentBatchErrorEnvelope()
		{
			return new SurveyInstrumentBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<SurveyInstrumentBatchError>
					{
						ConstructSurveyInstrumentBatchError(),
					},
			};
		}
		public static SurveyInstrumentBatchExportDocument ConstructSurveyInstrumentBatchExportDocument()
		{
			return new SurveyInstrumentBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0, 
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructSurveyInstrumentBatchCatalogDependencies(),
				SurveyInstruments = new List<SurveyInstrument>
					{
						ConstructSurveyInstrument(),
					},
			};
		}
		public static SurveyInstrumentBatchExportRequest ConstructSurveyInstrumentBatchExportRequest()
		{
			return new SurveyInstrumentBatchExportRequest
			{
				Scope = (SurveyInstrumentBatchExportScope)0,
				SurveyInstrumentIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static SurveyInstrumentBatchRestoreRequest ConstructSurveyInstrumentBatchRestoreRequest()
		{
			return new SurveyInstrumentBatchRestoreRequest
			{
				ConflictPolicy = (SurveyInstrumentBatchRestoreConflictPolicy)0,
				Document = ConstructSurveyInstrumentBatchExportDocument(),
			};
		}
		public static SurveyInstrumentBatchRestoreResponse ConstructSurveyInstrumentBatchRestoreResponse()
		{
			return new SurveyInstrumentBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedCount = 0, 
				ReplacedCount = 0, 
				CreatedCatalogDefinitionCount = 0, 
				SurveyInstrumentIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static SurveyInstrumentFeatureAssignment ConstructSurveyInstrumentFeatureAssignment()
		{
			return new SurveyInstrumentFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static SurveyInstrumentFeatureCategory ConstructSurveyInstrumentFeatureCategory()
		{
			return new SurveyInstrumentFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<SurveyInstrumentFeatureOption>
					{
						ConstructSurveyInstrumentFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static SurveyInstrumentFeatureOption ConstructSurveyInstrumentFeatureOption()
		{
			return new SurveyInstrumentFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static SurveyInstrumentIdentity ConstructSurveyInstrumentIdentity()
		{
			return new SurveyInstrumentIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static SurveyInstrumentIdentityAssignment ConstructSurveyInstrumentIdentityAssignment()
		{
			return new SurveyInstrumentIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static ProblemDetails ConstructProblemDetails()
		{
			return new ProblemDetails
			{
				Type = "Default Type",
				Title = "Default Title",
				Status = null,
				Detail = "Default Detail",
				Instance = "Default Instance",
			};
		}
		public static ExternalReferenceIssue ConstructExternalReferenceIssue()
		{
			return new ExternalReferenceIssue
			{
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static OctreeIndexStatus ConstructOctreeIndexStatus()
		{
			return new OctreeIndexStatus
			{
				TrajectoryID = new Guid(),
				State = (OctreeIndexState)0,
				HasIndex = false,
				IsCurrent = false,
				TrajectoryType = (TrajectoryType)0,
				IsDefinitive = false,
				SurveyStationCount = 0,
				BucketCount = 0,
				OctreeCodeCount = 0,
				SourceLastModificationDate = DateTimeOffset.UtcNow,
				IndexSchemaVersion = null,
				ConfidenceFactor = null,
				CalculationParametersHash = "Default CalculationParametersHash",
			};
		}
		public static SurveyRunExternalReferenceAuditRequest ConstructSurveyRunExternalReferenceAuditRequest()
		{
			return new SurveyRunExternalReferenceAuditRequest
			{
				Scope = (ExternalReferenceAuditScope)0,
				SurveyRunIDs = new List<Guid>
					{
						new Guid(),
					},
				Offset = 0,
				Limit = 0,
			};
		}
		public static SurveyRunExternalReferenceAuditResult ConstructSurveyRunExternalReferenceAuditResult()
		{
			return new SurveyRunExternalReferenceAuditResult
			{
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Total = 0,
				Offset = 0,
				Limit = 0,
				ValidCount = 0,
				InvalidCount = 0,
				UnavailableCount = 0,
				Items = new List<SurveyRunExternalReferenceValidation>
					{
						ConstructSurveyRunExternalReferenceValidation(),
					},
			};
		}
		public static SurveyRunExternalReferenceValidation ConstructSurveyRunExternalReferenceValidation()
		{
			return new SurveyRunExternalReferenceValidation
			{
				SurveyRunID = new Guid(),
				FieldID = null,
				ClusterID = null,
				WellID = null,
				WellBoreID = new Guid(),
				SurveyInstrumentID = new Guid(),
				FieldExists = null,
				ClusterExists = null,
				WellExists = null,
				WellBoreExists = null,
				SurveyInstrumentExists = null,
				Status = (ExternalReferenceValidationStatus)0,
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Issues = new List<ExternalReferenceIssue>
					{
						ConstructExternalReferenceIssue(),
					},
			};
		}
		public static SurveyRunSearchResult ConstructSurveyRunSearchResult()
		{
			return new SurveyRunSearchResult
			{
				Offset = 0,
				Limit = 0,
				TotalCount = 0,
				Items = new List<SurveyRunLight>(),
			};
		}
		public static TrajectoryBatchCatalogDependencies ConstructTrajectoryBatchCatalogDependencies()
		{
			return new TrajectoryBatchCatalogDependencies
			{
				Identities = new List<TrajectoryIdentity>
					{
						ConstructTrajectoryIdentity(),
					},
				FeatureCategories = new List<TrajectoryFeatureCategory>
					{
						ConstructTrajectoryFeatureCategory(),
					},
			};
		}
		public static TrajectoryBatchCatalogMapping ConstructTrajectoryBatchCatalogMapping()
		{
			return new TrajectoryBatchCatalogMapping
			{
				Catalog = "Default Catalog",
				Name = "Default Name",
				SourceID = new Guid(),
				LocalID = new Guid(),
				Resolution = "Default Resolution",
			};
		}
		public static TrajectoryBatchError ConstructTrajectoryBatchError()
		{
			return new TrajectoryBatchError
			{
				PositionIndex = null,
				Property = "Default Property",
				Code = "Default Code",
				Message = "Default Message",
			};
		}
		public static TrajectoryBatchErrorEnvelope ConstructTrajectoryBatchErrorEnvelope()
		{
			return new TrajectoryBatchErrorEnvelope
			{
				Error = "Default Error",
				Message = "Default Message",
				Errors = new List<TrajectoryBatchError>
					{
						ConstructTrajectoryBatchError(),
					},
			};
		}
		public static TrajectoryBatchExportDocument ConstructTrajectoryBatchExportDocument()
		{
			return new TrajectoryBatchExportDocument
			{
				FormatIdentifier = "Default FormatIdentifier",
				SchemaVersion = 0,
				ExportedAtUtc = DateTimeOffset.UtcNow,
				CatalogDependencies = ConstructTrajectoryBatchCatalogDependencies(),
				SurveyRuns = new List<SurveyRun>
					{
						ConstructSurveyRun(),
					},
				Trajectories = new List<Trajectory>
					{
						ConstructTrajectory(),
					},
			};
		}
		public static TrajectoryBatchExportRequest ConstructTrajectoryBatchExportRequest()
		{
			return new TrajectoryBatchExportRequest
			{
				Scope = (TrajectoryBatchExportScope)0,
				SurveyRunIDs = new List<Guid>
					{
						new Guid(),
					},
				TrajectoryIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static TrajectoryBatchRestoreRequest ConstructTrajectoryBatchRestoreRequest()
		{
			return new TrajectoryBatchRestoreRequest
			{
				ConflictPolicy = (TrajectoryBatchRestoreConflictPolicy)0,
				CatalogPolicy = (TrajectoryBatchCatalogRestorePolicy)0,
				AllowNormalizedNameMapping = false,
				Document = ConstructTrajectoryBatchExportDocument(),
			};
		}
		public static TrajectoryBatchRestoreResponse ConstructTrajectoryBatchRestoreResponse()
		{
			return new TrajectoryBatchRestoreResponse
			{
				RestoredAtUtc = DateTimeOffset.UtcNow,
				CreatedSurveyRunCount = 0,
				ReplacedSurveyRunCount = 0,
				CreatedTrajectoryCount = 0,
				ReplacedTrajectoryCount = 0,
				CreatedCatalogDefinitionCount = 0,
				CatalogMappings = new List<TrajectoryBatchCatalogMapping>
					{
						ConstructTrajectoryBatchCatalogMapping(),
					},
				SurveyRunIDs = new List<Guid>
					{
						new Guid(),
					},
				TrajectoryIDs = new List<Guid>
					{
						new Guid(),
					},
			};
		}
		public static TrajectoryExternalReferenceAuditRequest ConstructTrajectoryExternalReferenceAuditRequest()
		{
			return new TrajectoryExternalReferenceAuditRequest
			{
				Scope = (ExternalReferenceAuditScope)0,
				TrajectoryIDs = new List<Guid>
					{
						new Guid(),
					},
				Offset = 0,
				Limit = 0,
			};
		}
		public static TrajectoryExternalReferenceAuditResult ConstructTrajectoryExternalReferenceAuditResult()
		{
			return new TrajectoryExternalReferenceAuditResult
			{
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Total = 0,
				Offset = 0,
				Limit = 0,
				ValidCount = 0,
				InvalidCount = 0,
				UnavailableCount = 0,
				Items = new List<TrajectoryExternalReferenceValidation>
					{
						ConstructTrajectoryExternalReferenceValidation(),
					},
			};
		}
		public static TrajectoryExternalReferenceValidation ConstructTrajectoryExternalReferenceValidation()
		{
			return new TrajectoryExternalReferenceValidation
			{
				TrajectoryID = new Guid(),
				FieldID = null,
				ClusterID = null,
				WellID = null,
				WellBoreID = new Guid(),
				FieldExists = null,
				ClusterExists = null,
				WellExists = null,
				WellBoreExists = null,
				Status = (ExternalReferenceValidationStatus)0,
				CheckedAtUtc = DateTimeOffset.UtcNow,
				Issues = new List<ExternalReferenceIssue>
					{
						ConstructExternalReferenceIssue(),
					},
			};
		}
		public static TrajectoryFeatureAssignment ConstructTrajectoryFeatureAssignment()
		{
			return new TrajectoryFeatureAssignment
			{
				ID = new Guid(),
				FeatureCategoryID = null, 
				FeatureOptionID = null, 
				FromDate = DateTimeOffset.UtcNow,
				ToDate = DateTimeOffset.UtcNow,
			};
		}
		public static TrajectoryFeatureCategory ConstructTrajectoryFeatureCategory()
		{
			return new TrajectoryFeatureCategory
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				IsExclusive = false, 
				HasValidityPeriod = false, 
				Options = new List<TrajectoryFeatureOption>
					{
						ConstructTrajectoryFeatureOption(),
					},
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static TrajectoryFeatureOption ConstructTrajectoryFeatureOption()
		{
			return new TrajectoryFeatureOption
			{
				ID = new Guid(),
				Name = "Default Name",
			};
		}
		public static TrajectoryIdentity ConstructTrajectoryIdentity()
		{
			return new TrajectoryIdentity
			{
				MetaInfo = ConstructMetaInfo(),
				Name = "Default Name",
				CreationDate = DateTimeOffset.UtcNow,
				LastModificationDate = DateTimeOffset.UtcNow,
			};
		}
		public static TrajectoryIdentityAssignment ConstructTrajectoryIdentityAssignment()
		{
			return new TrajectoryIdentityAssignment
			{
				ID = new Guid(),
				IdentityID = null, 
				Value = "Default Value",
			};
		}
		public static TrajectorySearchResult ConstructTrajectorySearchResult()
		{
			return new TrajectorySearchResult
			{
				Offset = 0,
				Limit = 0,
				TotalCount = 0,
				Items = new List<TrajectoryLight>(),
			};
		}
	}
}
