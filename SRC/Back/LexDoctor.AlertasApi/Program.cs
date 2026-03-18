using LexDoctor.AlertasApi.Config;
using LexDoctor.AlertasApi.Models;
using LexDoctor.AlertasApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Pattern Options: enlazar sección "Database"
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<AlertasCaducidadOptions>(builder.Configuration.GetSection("AlertasCaducidad"));
builder.Services.AddScoped<IExpedienteRepository, ExpedienteRepository>();


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//cache
builder.Services.AddMemoryCache();



var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//}
    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
