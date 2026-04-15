namespace NORCE.Drilling.Trajectory.ModelShared
{
    using System = global::System;

    public partial class Client
    {
        public virtual async System.Threading.Tasks.Task<System.Collections.Generic.ICollection<System.Guid>> GetAllInterpolatedTrajectoryIdAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));

            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory");

            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);

            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<System.Collections.Generic.ICollection<System.Guid>>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task<System.Collections.Generic.ICollection<MetaInfo>> GetAllInterpolatedTrajectoryMetaInfoAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/MetaInfo");
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<System.Collections.Generic.ICollection<MetaInfo>>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task<InterpolatedTrajectory> GetInterpolatedTrajectoryCaseByIdAsync(System.Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/").Append(System.Uri.EscapeDataString(ConvertToString(id, System.Globalization.CultureInfo.InvariantCulture)));
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<InterpolatedTrajectory>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            if (status_ == 404) throw new ApiException("A server side error occurred.", status_, null, headers_, null);
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task<InterpolatedTrajectory> GetInterpolatedTrajectoryByTrajectoryIdAsync(System.Guid trajectoryId, System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/Trajectory/").Append(System.Uri.EscapeDataString(ConvertToString(trajectoryId, System.Globalization.CultureInfo.InvariantCulture)));
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<InterpolatedTrajectory>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            if (status_ == 404) throw new ApiException("A server side error occurred.", status_, null, headers_, null);
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task<System.Collections.Generic.ICollection<InterpolatedTrajectoryLight>> GetAllInterpolatedTrajectoryLightAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/LightData");
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<System.Collections.Generic.ICollection<InterpolatedTrajectoryLight>>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task<System.Collections.Generic.ICollection<InterpolatedTrajectory>> GetAllInterpolatedTrajectoryAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("GET");
            request_.Headers.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue.Parse("application/json"));
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/HeavyData");
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ == 200)
            {
                var objectResponse_ = await ReadObjectResponseAsync<System.Collections.Generic.ICollection<InterpolatedTrajectory>>(response_, headers_, cancellationToken).ConfigureAwait(false);
                return objectResponse_.Object ?? throw new ApiException("Response was null which was not expected.", status_, objectResponse_.Text, headers_, null);
            }
            var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
            throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
        }

        public virtual async System.Threading.Tasks.Task PostInterpolatedTrajectoryAsync(InterpolatedTrajectory body, System.Threading.CancellationToken cancellationToken = default)
        {
            await SendInterpolatedTrajectoryAsync("POST", "InterpolatedTrajectory", body, cancellationToken).ConfigureAwait(false);
        }

        public virtual async System.Threading.Tasks.Task PutInterpolatedTrajectoryByIdAsync(System.Guid id, InterpolatedTrajectory body, System.Threading.CancellationToken cancellationToken = default)
        {
            await SendInterpolatedTrajectoryAsync("PUT", "InterpolatedTrajectory/" + System.Uri.EscapeDataString(ConvertToString(id, System.Globalization.CultureInfo.InvariantCulture)), body, cancellationToken).ConfigureAwait(false);
        }

        public virtual async System.Threading.Tasks.Task DeleteInterpolatedTrajectoryByIdAsync(System.Guid id, System.Threading.CancellationToken cancellationToken = default)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            request_.Method = new System.Net.Http.HttpMethod("DELETE");
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append("InterpolatedTrajectory/").Append(System.Uri.EscapeDataString(ConvertToString(id, System.Globalization.CultureInfo.InvariantCulture)));
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ != 200)
            {
                var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
                throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
            }
        }

        private async System.Threading.Tasks.Task SendInterpolatedTrajectoryAsync(string method, string relativeUrl, InterpolatedTrajectory body, System.Threading.CancellationToken cancellationToken)
        {
            var client_ = _httpClient;
            using var request_ = new System.Net.Http.HttpRequestMessage();
            var json_ = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(body, JsonSerializerSettings);
            request_.Content = new System.Net.Http.ByteArrayContent(json_);
            request_.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
            request_.Method = new System.Net.Http.HttpMethod(method);
            var urlBuilder_ = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(_baseUrl)) urlBuilder_.Append(_baseUrl);
            urlBuilder_.Append(relativeUrl);
            PrepareRequest(client_, request_, urlBuilder_);
            var url_ = urlBuilder_.ToString();
            request_.RequestUri = new System.Uri(url_, System.UriKind.RelativeOrAbsolute);
            PrepareRequest(client_, request_, url_);
            using var response_ = await client_.SendAsync(request_, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var headers_ = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<string>>();
            foreach (var item_ in response_.Headers) headers_[item_.Key] = item_.Value;
            if (response_.Content != null && response_.Content.Headers != null)
            {
                foreach (var item_ in response_.Content.Headers) headers_[item_.Key] = item_.Value;
            }
            ProcessResponse(client_, response_);
            var status_ = (int)response_.StatusCode;
            if (status_ != 200)
            {
                var responseData_ = response_.Content == null ? null : await ReadAsStringAsync(response_.Content, cancellationToken).ConfigureAwait(false);
                throw new ApiException("The HTTP status code of the response was not expected (" + status_ + ").", status_, responseData_, headers_, null);
            }
        }
    }

    public partial class InterpolatedTrajectoryLight
    {
        [System.Text.Json.Serialization.JsonPropertyName("MetaInfo")]
        public MetaInfo? MetaInfo { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Name")]
        public string? Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("Description")]
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("CreationDate")]
        public System.DateTimeOffset? CreationDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("LastModificationDate")]
        public System.DateTimeOffset? LastModificationDate { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("TrajectoryID")]
        public System.Guid TrajectoryID { get; set; }

        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [System.Text.Json.Serialization.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }
    }

    public partial class InterpolatedTrajectory : InterpolatedTrajectoryLight
    {
        [System.Text.Json.Serialization.JsonPropertyName("SurveyStationList")]
        public System.Collections.Generic.ICollection<SurveyStation>? SurveyStationList { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InterpolationStep")]
        public double? InterpolationStep { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InterpolationReferenceDepth")]
        public double? InterpolationReferenceDepth { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("MaximumChordArcDistance")]
        public double? MaximumChordArcDistance { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("IncludeFirstSurvey")]
        public bool IncludeFirstSurvey { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("IncludeLastSurvey")]
        public bool IncludeLastSurvey { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InterpolateAtCasingAndLinerShoeDepths")]
        public bool InterpolateAtCasingAndLinerShoeDepths { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InterpolateAtLinerHangerDepths")]
        public bool InterpolateAtLinerHangerDepths { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InterpolateAtCasingChangeOfDiameter")]
        public bool InterpolateAtCasingChangeOfDiameter { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("AdditionalAbscissaList")]
        public System.Collections.Generic.ICollection<AnnotatedAbscissa>? AdditionalAbscissaList { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("InternalAdditionalAbscissaList")]
        public System.Collections.Generic.ICollection<AnnotatedAbscissa>? InternalAdditionalAbscissaList { get; set; }
    }
}
