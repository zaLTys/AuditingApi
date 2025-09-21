using AuditingApi.Middleware;
using AuditingApi.Services;
using AuditingApi.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAuditMemoryStore, AuditMemoryStore>();
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();

builder.Services.AddHostedService<AuditKafkaWorker>();
builder.Services.AddHostedService<AuditMongoWorker>();

builder.Services.AddLogging();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Local")
{
    //app.UseHttpsRedirection();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<AuditingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
