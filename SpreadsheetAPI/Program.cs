using FluentValidation.AspNetCore;
using SpreadsheetAPI.DAL;
using SpreadsheetAPI.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<SpreadSheetDatabaseSettings>(
    builder.Configuration.GetSection("SpreadSheetDatabaseSettings"));
builder.Services.AddSingleton<ISpreadSheetService, SpreadSheetService>();



builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();

app.MapControllers();

app.Run();
