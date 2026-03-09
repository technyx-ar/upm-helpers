namespace Technyx.One.Http
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public T Data { get; set; }
        public ApiError Error { get; set; }
        public string RawBody { get; set; }

        public static ApiResponse<T> Success(T data, int statusCode, string rawBody) => new()
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data,
            RawBody = rawBody,
        };

        public static ApiResponse<T> Fail(ApiError error, int statusCode, string rawBody) => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            Error = error ?? new ApiError { Code = "UNKNOWN", Message = "An unknown error occurred." },
            RawBody = rawBody,
        };
    }
}
