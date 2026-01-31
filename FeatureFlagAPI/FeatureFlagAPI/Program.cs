using FeatureFlagAPI;
using FeatureFlagAPI.Service;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMemoryCache();
builder.Services.AddScoped<FeatureEvaluationService>();

builder.Services.AddControllers();

builder.Services.AddCors(options => {
options.AddPolicy("AllowAngularApp", policy => policy.WithOrigins("http://localhost:4200") // Angular dev server
                                                                                         .AllowAnyHeader() .AllowAnyMethod()); });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors("AllowAngularApp");

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
