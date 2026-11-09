using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Technyx.Sdk.Http;
using Technyx.Sdk.Models;

namespace Technyx.Sdk.Photos
{
    public class PhotoService
    {
        public const int MaxPhotoBytes = 50 * 1024 * 1024;

        private readonly ApiClient _http;

        public PhotoService(ApiClient http)
        {
            _http = http;
        }

        // The backend returns 30 photos per page, newest first.
        public Task<ApiResponse<PaginatedResponse<PhotoData>>> ListAsync(
            int page = 1,
            PhotoFilter filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = new QueryBuilder().Add("page", page);
            filter?.AppendTo(query);

            return _http.GetAsync<PaginatedResponse<PhotoData>>(query.AppendTo("photos"), cancellationToken);
        }

        public Task<ApiResponse<PhotoData>> GetAsync(string photoId, CancellationToken cancellationToken = default)
        {
            return _http.GetAsync<PhotoData>($"photos/{Guard.Segment(photoId, nameof(photoId))}", cancellationToken);
        }

        // The whole image is held in memory while it is encoded and sent, so compress or downscale camera
        // frames first.
        public Task<ApiResponse<PhotoData>> UploadAsync(
            byte[] image,
            string fileName,
            string contentType,
            PhotoMetadata metadata = null,
            CancellationToken cancellationToken = default)
        {
            var invalid = UploadValidation.CheckFile<PhotoData>("file", image, fileName, contentType, MaxPhotoBytes);
            if (invalid != null)
                return Task.FromResult(invalid);

            if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(UploadValidation.Failure<PhotoData>("file", "Only images can be uploaded."));

            var form = new MultipartFormBuilder().AddFile("file", fileName, contentType, image);
            if (metadata != null)
                form.AddField("metadata", JsonConvert.SerializeObject(metadata.ToPayload()));

            return _http.PostMultipartAsync<PhotoData>("photos", form, cancellationToken);
        }

        public Task<ApiResponse<PhotoData>> UpdateAsync(
            string photoId,
            UpdatePhotoRequest request,
            CancellationToken cancellationToken = default)
        {
            var segment = Guard.Segment(photoId, nameof(photoId));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            return _http.PatchAsync<PhotoData>($"photos/{segment}", request.ToPayload(), cancellationToken);
        }

        public Task<ApiResponse<object>> DeleteAsync(string photoId, CancellationToken cancellationToken = default)
        {
            return _http.DeleteAsync($"photos/{Guard.Segment(photoId, nameof(photoId))}", cancellationToken);
        }
    }
}
