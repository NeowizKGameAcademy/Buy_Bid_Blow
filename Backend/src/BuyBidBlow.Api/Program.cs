using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// 비밀정보(DB 비밀번호 등)는 커밋하지 않는 appsettings.Local.json이나 환경변수로 넣는다.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("Main")
    ?? throw new InvalidOperationException("ConnectionStrings:Main 설정이 없습니다.");
builder.Services.AddSingleton(new MySqlDataSource(connectionString));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("ok"));

app.MapGet("/health/db", async (MySqlDataSource db) =>
{
    await using var connection = await db.OpenConnectionAsync();
    return Results.Ok("ok");
});

app.Run();
