using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mes.Application.Contracts.Common;
using Mes.Application.Contracts.OperatorExecution;
using Mes.ExperienceApi.OperatorExecution;
using Mes.Infrastructure.OperatorExecution;
using Mes.Infrastructure.OperatorExecution.InMemory;

namespace Mes.ExperienceApi.Tests;

/// <summary>
/// 운영자 실행 Experience API host의 DI와 라우트 매핑을 검증합니다.
/// </summary>
public sealed class OperatorExecutionExperienceApiEndpointRouteBuilderTests
{
    /// <summary>
    /// reference 서비스 등록이 endpoint adapter와 저장소를 함께 해석하는지 검증합니다.
    /// </summary>
    [Fact]
    public void AddOperatorExecutionReferenceServices_should_resolve_endpoint_adapter_and_store()
    {
        var app = CreateApplication();

        Assert.NotNull(app.Services.GetService<OperatorExecutionBffEndpointAdapter>());
        Assert.NotNull(app.Services.GetService<InMemoryOperatorExecutionStore>());
    }

    /// <summary>
    /// documented endpoint signature가 모두 minimal API 라우트로 노출되는지 검증합니다.
    /// </summary>
    [Fact]
    public void MapOperatorExecutionEndpoints_should_expose_all_documented_signatures()
    {
        var app = CreateApplication();
        var routeEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();

        var expectedSignatures = OperatorExecutionEndpointSignatures.GetAll();

        Assert.Equal(expectedSignatures.Count, routeEndpoints.Count);

        foreach (var signature in expectedSignatures)
        {
            var routeEndpoint = Assert.Single(
                routeEndpoints,
                endpoint => string.Equals(endpoint.RoutePattern.RawText, signature.Route, StringComparison.Ordinal));

            Assert.Equal(signature.OperationName, routeEndpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
            Assert.Contains(signature.HttpMethod, routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? []);
            Assert.NotNull(routeEndpoint.Metadata.GetMetadata<ITagsMetadata>());
        }
    }

    /// <summary>
    /// 테스트용 minimal API application을 생성하고 운영자 실행 라우트를 등록합니다.
    /// </summary>
    /// <returns>운영자 실행 endpoint가 등록된 web application입니다.</returns>
    private static WebApplication CreateApplication()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.FullName,
            EnvironmentName = Environments.Development
        });

        builder.Services.AddOperatorExecutionReferenceServices();

        var app = builder.Build();
        app.MapOperatorExecutionEndpoints();
        return app;
    }
}
