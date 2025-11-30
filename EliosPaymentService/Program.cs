using CEliosPaymentService.Repositories.Interfaces;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Implementations;
using EliosPaymentService.Repositories.Implementations.Repository;
using EliosPaymentService.Repositories.Interfaces;
using EliosPaymentService.Repositories.Interfaces.IRepository;
using EliosPaymentService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PayOS;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SUtility API",
        Version = "v1",
        Description = "API utility operations"
    });
    c.AddServer(new OpenApiServer { Url = "/" });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database Context
builder.Services.AddDbContext<CVBuilderDataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderTransactionRepository, OrderTransactionRepository>();
builder.Services.AddScoped<IOrderInvoiceRepository, OrderInvoiceRepository>();

// Service registration
builder.Services.AddScoped<IAppConfiguration, AppConfiguration>();
builder.Services.AddScoped<ICombinedTransaction, CombinedTransaction>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped(typeof(IKafkaProducerRepository<>), typeof(KafkaProducerRepository<>));
builder.Services.AddScoped(typeof(IKafkaConsumerRepository<>), typeof(KafkaConsumerRepository<>));
builder.Services.AddScoped(typeof(IKafkaConsumerFactory<>), typeof(KafkaConsumerFactory<>));
builder.Services.AddScoped(typeof(IKafkaResponseHandler<>), typeof(KafkaResponseHandler<>));
builder.Services.AddScoped<IKafkaTransaction, KafkaTransaction>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<OrderTransactionService>();
builder.Services.AddScoped<OrderInvoiceService>();

// Configure payOS for order controller
builder.Services.AddKeyedSingleton("OrderClient", (sp, key) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PayOSClient(new PayOSOptions
    {
        ClientId = config["PayOS:ClientId"] ?? Environment.GetEnvironmentVariable("PAYOS_CLIENT_ID"),
        ApiKey = config["PayOS:ApiKey"] ?? Environment.GetEnvironmentVariable("PAYOS_API_KEY"),
        ChecksumKey = config["PayOS:ChecksumKey"] ?? Environment.GetEnvironmentVariable("PAYOS_CHECKSUM_KEY"),
        LogLevel = LogLevel.Debug,
    });
});

// Configure payOS for transfer controller
builder.Services.AddKeyedSingleton("TransferClient", (sp, key) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new PayOSClient(new PayOSOptions
    {
        ClientId = config["PayOS:PayoutClientId"],
        ApiKey = config["PayOS:PayoutApiKey"],
        ChecksumKey = config["PayOS:PayoutChecksumKey"],
        LogLevel = LogLevel.Debug,
    });
});

// Kafka Consumers
var sourceServices = builder.Configuration.GetSection("Kafka:SourceServices").Get<string[]>() ?? [];

Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Registering {sourceServices.Length} Kafka consumers for sources: [{string.Join(", ", sourceServices)}]");


foreach (var sourceService in sourceServices)
{
    var currentSource = sourceService;

    builder.Services.AddSingleton<IHostedService>(sp =>
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        return ActivatorUtilities.CreateInstance<KafkaConsumerHostedService<Order>>(
            sp,
            scopeFactory,
            currentSource
        );
    });
}


var app = builder.Build();


app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope = app.Services.CreateScope();
    var appConfiguration = scope.ServiceProvider.GetRequiredService<IAppConfiguration>();
    KafkaResponseConsumer.Initialize(appConfiguration);
    Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] KafkaResponseConsumer initialized");
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.MapOpenApi();
//    app.UseDeveloperExceptionPage();
//    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
//}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PaymentService API forum");
    c.DocumentTitle = "PaymentService API Documentation";
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

var currentService = builder.Configuration["KafkaCommunication:CurrentService"];
Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {currentService} Service Started!");
Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] Kafka Consumers registered for: [{string.Join(", ", sourceServices)}]");

app.Run();
