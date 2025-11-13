using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ProxySharp.Models;
using ProxySharp.Services;
using System.Net;

namespace ProxySharp.Controllers;

[Route("[controller]")]
public class ProxyController : ControllerBase
{
    private readonly TestServiceClient _serviceClient;

    public ProxyController(TestServiceClient serviceClient)
    {
        _serviceClient = serviceClient;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok("Healthy!");
    }

    [HttpGet("entity/action")]
    public async Task<IActionResult> GetEntityData([FromQuery(Name = "page")] string page)
    {
        var result = await _serviceClient.GetAsync<EntityDataResponse>("entity/action?page=" + page);

        if (HttpStatusCode.OK == result.StatusCode && null != result.Data)
        {
            return Ok(result.Data);
        }

        return StatusCode((int) result.StatusCode, result.RawBody);
    }
}
