using EnergyBackend.Data;
using EnergyBackend.Domain.Models;
using EnergyBackend.Services;
using InfluxDB.Client;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCorsPolicy", policy =>
    {
        //policy.WithOrigins("http://localhost:4200") // Angular dev server
        policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return InfluxDBClientFactory.Create(config["InfluxDB:Url"], config["InfluxDB:Token"].ToCharArray());
});
builder.Services.AddSingleton<InfluxDbService>();



var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// OPTIONAL : seed DB here or call a seeder
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    // Seed data if necessary

    if (!db.Devices.Any())
    {
        db.Devices.AddRange(new[]
        {
            new Device { DeviceIdentifier = "Device001", Name = "Solar Panel 1", Location = "Roof", IsActive = true, CreatedAt = DateTime.Now },
            new Device { DeviceIdentifier = "Device002", Name = "Wind Turbine 1", Location = "Field", IsActive = true, CreatedAt = DateTime.Now }
        });

        db.SaveChanges();
    }
}


app.Run();
