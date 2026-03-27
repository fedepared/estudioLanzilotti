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


//cors dev
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AngularPolicy", policy =>
//    {
//        policy
//            .WithOrigins("http://localhost:4200")
//            .AllowAnyHeader()
//            .AllowAnyMethod();
//    });
//});

//cors prod
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});




var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//}
    app.UseSwagger();
    app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("OpenCorsPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
