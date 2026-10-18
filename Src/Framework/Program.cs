using Application.Interfaces;
using Application.Services;
using Infrastructure.Caching;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Scripting;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cfg = builder.Configuration;
var postgres = cfg.GetConnectionString("Postgres") ?? "Host=localhost;Port=5432;Database=rulesdb;Username=rulesuser;Password=devpassword";
var redis = cfg.GetConnectionString("Redis") ?? "localhost:6379";
var rabbitHost = cfg["RabbitMQ:Host"] ?? "localhost";
var rabbitUser = cfg["RabbitMQ:User"] ?? "guest";
var rabbitPassword = cfg["RabbitMQ:Password"] ?? "guest";

builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(postgres));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));
builder.Services.AddSingleton<IRuleCache, RedisRuleCache>();
builder.Services.AddSingleton<IRuleCompiler, LuaRuleCompiler>();
builder.Services.AddSingleton<IRuleEvaluator, LuaRuleEvaluator>();

builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IProcessedEventRepository, ProcessedEventRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<CreateRuleService>();
builder.Services.AddScoped<EvaluateDeviceService>();

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory { HostName = rabbitHost, UserName = rabbitUser, Password = rabbitPassword, DispatchConsumersAsync = true };
    return factory.CreateConnection("rule-engine-outbox-publisher");
});
builder.Services.AddSingleton<IActionPublisher, RabbitMqActionPublisher>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<MqttTelemetryWorker>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.Run();
