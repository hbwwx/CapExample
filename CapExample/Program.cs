using CapExample;
using DotNetCore.CAP.Dashboard.NodeDiscovery;

var builder = WebApplication.CreateBuilder(args);

ConfigurationManager _configuration = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>();

builder.Services.AddCap(x =>
{
    x.UseEntityFramework<AppDbContext>();
    x.UseRabbitMQ(configure =>
    {
        configure.UserName = "guest";
        configure.UserName = "guest";
        configure.HostName = "10.27.254.167";

    });
    x.UseDashboard();
    x.FailedRetryCount = 5;
    x.FailedThresholdCallback = failed =>
    {
    };
    x.UseDiscovery(options =>
    {
        options.DiscoveryServerHostName = "10.27.254.167";
        options.DiscoveryServerPort = 8500;
        options.CurrentNodeHostName = "10.27.254.167";
        options.CurrentNodePort = 55008;
        options.Scheme = "https";
        options.NodeId = "CAP";
        options.NodeName = "CAP";

        options.DiscoveryServerHostName = _configuration["Consul:DiscoveryServerHostName"];
        options.DiscoveryServerPort = int.TryParse(_configuration["Consul:DiscoveryServerPort"], out int DiscoveryServerPort) ? DiscoveryServerPort : 8500;
        options.CurrentNodeHostName = _configuration["Consul:CurrentNodeHostName"];
        options.CurrentNodePort = int.TryParse(_configuration["Consul:CurrentNodePort"], out int CurrentNodePort) ? CurrentNodePort : 0; ;
        options.Scheme = _configuration["Consul:Scheme"];
        options.NodeId = _configuration["Consul:NodeId"];
        options.NodeName = _configuration["Consul:NodeName"];
    });
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
