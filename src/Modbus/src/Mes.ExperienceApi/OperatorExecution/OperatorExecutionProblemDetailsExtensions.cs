using System.Text.Json;
using Mes.Application.OperatorExecution;
using Mes.Domain.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mes.ExperienceApi.OperatorExecution;

/// <summary>
/// operator-execution host의 problem details 및 bad-request 동작을 구성하는 확장 메서드를 제공합니다.
/// </summary>
public static class OperatorExecutionProblemDetailsExtensions
{
    /// <summary>
    /// operator-execution host가 transport 바인딩 실패를 예외로 승격하도록 구성합니다.
    /// </summary>
    /// <param name="services">서비스 컬렉션입니다.</param>
    /// <returns>동일한 서비스 컬렉션입니다.</returns>
    public static IServiceCollection AddOperatorExecutionProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        return services;
    }

    /// <summary>
    /// operator-execution host에 공통 problem-details 예외 매핑을 적용합니다.
    /// </summary>
    /// <param name="application">웹 애플리케이션입니다.</param>
    /// <returns>동일한 애플리케이션입니다.</returns>
    public static WebApplication UseOperatorExecutionProblemDetails(this WebApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);

        application.UseExceptionHandler(exceptionHandlerApplication =>
        {
            exceptionHandlerApplication.Run(WriteProblemDetailsAsync);
        });

        return application;
    }

    /// <summary>
    /// 현재 요청의 예외를 stable problem details 응답으로 기록합니다.
    /// </summary>
    /// <param name="httpContext">현재 HTTP 문맥입니다.</param>
    /// <returns>비동기 응답 작성 작업입니다.</returns>
    private static Task WriteProblemDetailsAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var exception = httpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        var mapping = MapException(exception);
        var extensions = new Dictionary<string, object?>
        {
            ["errorCode"] = mapping.ErrorCode,
            ["traceId"] = httpContext.TraceIdentifier
        };

        AddContextExtensions(extensions, mapping.Context);

        return Results.Problem(
                statusCode: mapping.StatusCode,
                title: mapping.Title,
                type: mapping.Type,
                detail: mapping.Detail,
                extensions: extensions)
            .ExecuteAsync(httpContext);
    }

    /// <summary>
    /// 예외를 operator-execution problem-details 매핑 결과로 변환합니다.
    /// </summary>
    /// <param name="exception">변환할 예외입니다.</param>
    /// <returns>host가 사용할 problem-details 매핑 결과입니다.</returns>
    private static OperatorExecutionProblemMapping MapException(Exception? exception)
    {
        return exception switch
        {
            OperatorExecutionNotFoundException notFound => new OperatorExecutionProblemMapping(
                StatusCodes.Status404NotFound,
                "Not Found",
                "urn:mes:problem:operator_execution.not_found",
                notFound.ErrorCode,
                notFound.Message,
                notFound.Context),
            OperatorExecutionConflictException conflict => new OperatorExecutionProblemMapping(
                StatusCodes.Status409Conflict,
                "Conflict",
                "urn:mes:problem:operator_execution.conflict",
                conflict.ErrorCode,
                conflict.Message,
                conflict.Context),
            OperatorExecutionValidationException validation => new OperatorExecutionProblemMapping(
                StatusCodes.Status422UnprocessableEntity,
                "Unprocessable Entity",
                "urn:mes:problem:operator_execution.validation_failed",
                validation.ErrorCode,
                validation.Message,
                validation.Context),
            BadHttpRequestException => new OperatorExecutionProblemMapping(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "urn:mes:problem:transport.invalid_request",
                "transport.invalid_request",
                "The request body, route values, or query string could not be parsed.",
                null),
            JsonException => new OperatorExecutionProblemMapping(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "urn:mes:problem:transport.invalid_request",
                "transport.invalid_request",
                "The request body, route values, or query string could not be parsed.",
                null),
            ArgumentException => new OperatorExecutionProblemMapping(
                StatusCodes.Status400BadRequest,
                "Bad Request",
                "urn:mes:problem:transport.invalid_request",
                "transport.invalid_request",
                "The request body, route values, or query string could not be parsed.",
                null),
            DomainException domainException => new OperatorExecutionProblemMapping(
                StatusCodes.Status409Conflict,
                "Conflict",
                "urn:mes:problem:operator_execution.conflict",
                "operator_execution.conflict",
                domainException.Message,
                null),
            _ => new OperatorExecutionProblemMapping(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "urn:mes:problem:system.unexpected_error",
                "system.unexpected_error",
                "An unexpected error occurred while processing the request.",
                null)
        };
    }

    /// <summary>
    /// 오류 문맥이 있을 때 problem details extension 필드를 채웁니다.
    /// </summary>
    /// <param name="extensions">problem details extension 사전입니다.</param>
    /// <param name="context">추가할 오류 문맥입니다.</param>
    private static void AddContextExtensions(
        IDictionary<string, object?> extensions,
        OperatorExecutionErrorContext? context)
    {
        if (context is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(context.AggregateType))
        {
            extensions["aggregateType"] = context.AggregateType;
        }

        if (!string.IsNullOrWhiteSpace(context.AggregateId))
        {
            extensions["aggregateId"] = context.AggregateId;
        }

        if (!string.IsNullOrWhiteSpace(context.CommandId))
        {
            extensions["commandId"] = context.CommandId;
        }

        if (!string.IsNullOrWhiteSpace(context.IdempotencyKey))
        {
            extensions["idempotencyKey"] = context.IdempotencyKey;
        }
    }

    /// <summary>
    /// host가 사용할 problem-details 매핑 결과를 표현합니다.
    /// </summary>
    /// <param name="StatusCode">응답 상태 코드입니다.</param>
    /// <param name="Title">problem details 제목입니다.</param>
    /// <param name="Type">problem details 형식 식별자입니다.</param>
    /// <param name="ErrorCode">안정적인 오류 코드입니다.</param>
    /// <param name="Detail">응답 세부 메시지입니다.</param>
    /// <param name="Context">선택적 오류 문맥입니다.</param>
    private sealed record OperatorExecutionProblemMapping(
        int StatusCode,
        string Title,
        string Type,
        string ErrorCode,
        string Detail,
        OperatorExecutionErrorContext? Context);
}
