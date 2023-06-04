using ManewryMorskie.Server;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes
        .Concat(new[] { "application/octet-stream" });
});
builder.Services.AddSignalR()
    .AddJsonProtocol(options => {
        options.PayloadSerializerOptions.Converters.Add(new CellLib.CellLocationConverter());
    });

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSingleton<Rooms>();
builder.Services.AddScoped<Client>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAny", builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.MapHub<ManewryMorskieHub>("/ManewryMorskie");
app.MapGet("/ping", () => Results.Ok());

app.UseCors("AllowAny");

app.Run();
