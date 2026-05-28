using System.Data;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocal", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowLocal");

app.MapGet("/", () => Results.Ok(new
{
    service = "GDMS Status Bridge",
    status = "Running",
    endpoints = new[]
    {
        "/api/site-status",
        "/api/health"
    }
}));

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "OK",
    timestamp = DateTimeOffset.Now
}));

app.MapGet("/api/site-status", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("PIAEData");

    const string sql = @"
SELECT 
    'site-' + REPLACE(REPLACE(s.AssetID, '-AK', ''), '-', '') AS Tag,
    s.ID AS SiteID,
    s.AssetID,
    s.TelemetryID,
    v.StatusName
FROM [PIAEData].[dbo].[vw_GDMS_SiteCurrentStatus] v
INNER JOIN [PIAEData].[dbo].[GDMS_Site] s
    ON s.ID = v.SiteID
WHERE s.TelemetryID IS NOT NULL
ORDER BY s.AssetID;";

    var results = new List<SiteStatusDto>();

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = new SqlCommand(sql, connection);
    command.CommandType = CommandType.Text;
    command.CommandTimeout = 30;

    await using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        results.Add(new SiteStatusDto
        {
            Tag = reader["Tag"]?.ToString() ?? string.Empty,
            SiteID = reader["SiteID"] == DBNull.Value ? null : Convert.ToInt32(reader["SiteID"]),
            AssetID = reader["AssetID"]?.ToString() ?? string.Empty,
            TelemetryID = reader["TelemetryID"]?.ToString() ?? string.Empty,
            StatusName = reader["StatusName"]?.ToString() ?? string.Empty,
            Colour = string.Equals(reader["StatusName"]?.ToString(), "Alarmed", StringComparison.OrdinalIgnoreCase)
                ? "#ff0000"
                : "#ffff00"
        });
    }

    return Results.Ok(results);
});

app.Run();

public sealed class SiteStatusDto
{
    public string Tag { get; set; } = string.Empty;
    public int? SiteID { get; set; }
    public string AssetID { get; set; } = string.Empty;
    public string TelemetryID { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
}
