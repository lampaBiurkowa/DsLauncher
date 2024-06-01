using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsLauncher.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class NewsController(Repository<News> repository) : EntityController<News>(repository)
{
}