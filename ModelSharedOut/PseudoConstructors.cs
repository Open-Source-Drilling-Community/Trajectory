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
	}
}