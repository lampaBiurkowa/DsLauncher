using DibBase.Infrastructure;
using DibBaseSampleApi.Controllers;
using DsLauncher.Models;
using Microsoft.AspNetCore.Mvc;

namespace DsLauncher.Api;

[ApiController]
[Route("[controller]")]
public class PackageController(Repository<Package> repository) : EntityController<Package>(repository)
{
}