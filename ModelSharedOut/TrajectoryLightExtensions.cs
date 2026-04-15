using System;

namespace NORCE.Drilling.Trajectory.ModelShared
{
    public partial class TrajectoryLight
    {
        public Guid? FieldID
        {
            get => GetGuidAdditionalProperty("FieldID");
            set => SetGuidAdditionalProperty("FieldID", value);
        }

        public Guid? ClusterID
        {
            get => GetGuidAdditionalProperty("ClusterID");
            set => SetGuidAdditionalProperty("ClusterID", value);
        }

        public Guid? WellID
        {
            get => GetGuidAdditionalProperty("WellID");
            set => SetGuidAdditionalProperty("WellID", value);
        }

        private Guid? GetGuidAdditionalProperty(string propertyName)
        {
            if (AdditionalProperties == null || !AdditionalProperties.TryGetValue(propertyName, out object? value) || value == null)
            {
                return null;
            }

            if (value is Guid guidValue)
            {
                return guidValue;
            }

            if (value is string stringValue && Guid.TryParse(stringValue, out Guid parsedGuid))
            {
                return parsedGuid;
            }

            if (value is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String &&
                    Guid.TryParse(jsonElement.GetString(), out Guid jsonGuid))
                {
                    return jsonGuid;
                }
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Null)
                {
                    return null;
                }
            }

            return null;
        }

        private void SetGuidAdditionalProperty(string propertyName, Guid? value)
        {
            if (value == null || value == Guid.Empty)
            {
                AdditionalProperties?.Remove(propertyName);
                return;
            }

            AdditionalProperties[propertyName] = value.Value;
        }
    }
}
