using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsLauncher.Models;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class NewsController(Repository<News> repository) : EntityController<News>(repository)
{
}