using ClickHouse.Driver;
using Microsoft.EntityFrameworkCore;
using PersistenceClickhouse;
using PersistencePostgres;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICustomClickHouseConnectionFactory>(new CustomClickHouseConnectionFactory(""));
builder.Services.AddScoped<IClickHouseConnection>((serviceProvider) =>
    serviceProvider.GetRequiredService<ICustomClickHouseConnectionFactory>().CreateConnection());
builder.Services.AddScoped<ClickHouseMigrations>();

builder.Services.AddDbContext<MainDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

{
    using var scope = app.Services.CreateScope();
    var mainDbContext = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    await mainDbContext.Database.MigrateAsync();
}

{
    using var scope = app.Services.CreateScope();
    var migrations = scope.ServiceProvider.GetRequiredService<ClickHouseMigrations>();
    await migrations.Migrate();
}

app.Run();