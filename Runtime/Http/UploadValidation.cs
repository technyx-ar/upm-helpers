using System.Collections.Generic;

namespace Technyx.Sdk.Http
{
    internal static class UploadValidation
    {
        internal static ApiResponse<T> CheckFile<T>(string field, byte[] data, string fileName, string contentType, long maxBytes)
        {
            if (data == null || data.Length == 0)
                return Failure<T>(field, "The file is empty.");

            if (string.IsNullOrWhiteSpace(fileName))
                return Failure<T>(field, "A file name is required.");

            if (string.IsNullOrWhiteSpace(contentType))
                return Failure<T>(field, "A content type is required.");

            if (data.LongLength > maxBytes)
                return Failure<T>(field, $"The file is larger than {maxBytes / 1024} KB.");

            return null;
        }

        internal static ApiResponse<T> Failure<T>(string field, string message)
        {
            var error = new ApiError
            {
                Code = ApiErrorCodes.ValidationError,
                Message = message,
                ValidationErrors = new Dictionary<string, List<string>> { { field, new List<string> { message } } },
            };

            return ApiResponse<T>.Fail(error, 0, "");
        }
    }
}
