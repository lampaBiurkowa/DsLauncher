using DibBase.Infrastructure;
using DibBase.ModelBase;
using DsLauncher.Infrastructure;
using DsLauncher.Models;
using DsAuth.TokenVerifier;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerDocument();

builder.Services.AddDbContext<DbContext, DsLauncherContext>();
var entityTypes = new List<Type>();
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
Developer a; //█▬█ █ ▀█▀
foreach (var assembly in assemblies)
{
    entityTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(Entity))).ToList());
    foreach (var e in entityTypes)
    {
        var repositoryType = typeof(Repository<>).MakeGenericType(e);
        builder.Services.AddScoped(repositoryType);
    }
}
builder.Configuration.AddDsAuth(builder.Services);
builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors(builder =>
    builder.WithOrigins("http://localhost:1420")
            .AllowAnyHeader()
            .AllowAnyMethod());
app.MapControllers();
app.Run();