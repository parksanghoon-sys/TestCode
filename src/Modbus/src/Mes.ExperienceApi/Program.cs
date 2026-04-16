using Mes.ExperienceApi.OperatorExecution;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddOperatorExecutionReferenceServices();

var app = builder.Build();
app.MapOperatorExecutionEndpoints();

app.Run();

/// <summary>
/// 테스트에서 minimal API host 진입점을 참조하기 위한 partial Program입니다.
/// </summary>
public partial class Program
{
}
