using PayOS;
using EliosPaymentService.Models;
using EliosPaymentService.Repositories.Implementations;
using EliosPaymentService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
