using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using ProxySharp.Models;
using ProxySharp.Services;
using System.Net;

namespace ProxySharp.Controllers;

[Route("[controller]")]
public class ProxyController(TestServiceClient serviceClient, ILogger<ProxyController> logger) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        logger.LogInformation("Health check requested");
        return Ok("Healthy!");
    }

    [HttpGet("entity/action")]
    [EnableRateLimiting("sliding")]
    public async Task<IActionResult> GetEntityData([FromQuery(Name = "page")] string page)
    {
        logger.LogInformation("GetEntityData requested for page: {Page}", page);

        var result = await serviceClient.GetAsync<EntityDataResponse>("entity/action?page=" + page);

        if (HttpStatusCode.OK == result.StatusCode && null != result.Data)
        {
            logger.LogInformation("Successfully retrieved entity data for page: {Page}, StatusCode: {StatusCode}", 
                page, result.StatusCode);
            return Ok(result.Data);
        }

        logger.LogWarning("Failed to retrieve entity data for page: {Page}, StatusCode: {StatusCode}, RawBody: {RawBody}", 
            page, result.StatusCode, result.RawBody);

        return StatusCode((int) result.StatusCode, result.RawBody);
    }
}
