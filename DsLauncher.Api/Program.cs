using DibBase.Infrastructure;
using DibBase.ModelBase;
using DsLauncher.Infrastructure;
using DsLauncher.Api.Models;
using DsCore.ApiClient;
using Microsoft.EntityFrameworkCore;
using DsStorage.ApiClient;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using DsLauncher.Api.Ndib;
using DsSftpLib;
using System.Text.Json.Serialization;
using DsLauncher.Api;
using DsNotifier.Client;
using DsLauncher.Events;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddCors();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** your JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtSecurityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddSwaggerDocument();
builder.Configuration.AddDsSftpLib(builder.Services, true);
builder.Services.AddDbContext<DbContext, DsLauncherContext>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddMemoryCache();
builder.Services.AddOptions<DsDbLibOptions>()
    .Bind(builder.Configuration.GetSection(DsDbLibOptions.SECTION))
    .ValidateDataAnnotations();
var entityTypes = new List<Type>();
var assemblies = AppDomain.CurrentDomain.GetAssemblies();
PurchasedEvent b; //█▬█ █ ▀█▀
foreach (var assembly in assemblies)
{
    entityTypes.AddRange(assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(DibBase.ModelBase.Entity))).ToList());
    foreach (var e in entityTypes)
    {
        var repositoryType = typeof(Repository<>).MakeGenericType(e);
        builder.Services.AddScoped(repositoryType);
    }
}
builder.Services.AddScoped<NdibService>();
builder.Services.AddSingleton<AccessContext>();
builder.Services.AddDsCore(builder.Configuration);
builder.Services.AddDsStorage(builder.Configuration);
builder.Services.AddDsNotifier(builder.Configuration);
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