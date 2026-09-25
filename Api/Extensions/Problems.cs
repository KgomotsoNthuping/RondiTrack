using Microsoft.AspNetCore.Mvc;
using Api.Services;

namespace Api.Http;

public static class Problems
{
    public static ObjectResult ToProblem<T>(
        this ControllerBase controller,
        ServiceResult<T> result)
    {
        var (statusCode, title) =
            result.Status switch
            {
                ServiceResult.BadRequest =>
                    (400, "Bad Request"),

                ServiceResult.NotFound =>
                    (404, "Not Found"),

                ServiceResult.Conflict =>
                    (409, "Conflict"),

                ServiceResult.UnprocessableEntity =>
                    (422, "Unprocessable Entity"),

                _ =>
                    (500, "Unexpected Error")
            };

        return controller.Problem(
            statusCode: statusCode,
            title: title,
            detail: result.Detail);
    }

    public static ObjectResult NotFoundProblem(
        this ControllerBase controller,
        string detail)
    {
        return controller.Problem(
            statusCode: 404,
            title: "Not Found",
            detail: detail);
    }

    public static ObjectResult UnprocessableProblem(
        this ControllerBase controller,
        string detail)
    {
        return controller.Problem(
            statusCode: 422,
            title: "Unprocessable Entity",
            detail: detail);
    }
}