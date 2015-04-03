using Nancy;

namespace Flumine.Nancy
{
    public static class NancyExtensions
    {
        public static Response Success(this IResponseFormatter response, string message = "OK")
        {
            return response.AsJson(new { Message = message });
        }

        public static Response BadRequest(this IResponseFormatter response, string message = "Invalid arguments")
        {
            return response.AsJson(new { Message = message }, HttpStatusCode.BadRequest);
        }
    }
}