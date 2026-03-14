using Microsoft.AspNetCore.Mvc;
using ServerMonitor.Api.Models;
using ServerMonitor.Api.Services;

namespace ServerMonitor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServersController : ControllerBase
{
    private readonly IServerService _serverService;
    private readonly ISshMetricService _sshService;

    public ServersController(IServerService serverService, ISshMetricService sshService)
    {
        _serverService = serverService;
        _sshService = sshService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ServerDto>>> GetAllServers()
    {
        var servers = await _serverService.GetAllServersAsync();
        return Ok(servers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServerDto>> GetServer(int id)
    {
        var server = await _serverService.GetServerByIdAsync(id);
        if (server == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        return Ok(server);
    }

    [HttpPost]
    public async Task<ActionResult<ServerDto>> CreateServer([FromBody] CreateServerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required" });

        if (string.IsNullOrWhiteSpace(request.Host))
            return BadRequest(new { message = "Host is required" });

        if (string.IsNullOrWhiteSpace(request.Username))
            return BadRequest(new { message = "Username is required" });

        var server = await _serverService.CreateServerAsync(request);
        return CreatedAtAction(nameof(GetServer), new { id = server.Id }, server);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServerDto>> UpdateServer(int id, [FromBody] UpdateServerRequest request)
    {
        var server = await _serverService.UpdateServerAsync(id, request);
        if (server == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        return Ok(server);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteServer(int id)
    {
        var deleted = await _serverService.DeleteServerAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Server with ID {id} not found" });

        return NoContent();
    }

    [HttpGet("{id:int}/metrics")]
    public async Task<ActionResult<List<MetricSnapshotDto>>> GetServerMetrics(
        int id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? limit)
    {
        var server = await _serverService.GetServerByIdAsync(id);
        if (server == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        var metrics = await _serverService.GetServerMetricsAsync(id, from, to, limit ?? 100);
        return Ok(metrics);
    }

    [HttpGet("{id:int}/services")]
    public async Task<ActionResult<List<ServiceStatusDto>>> GetServerServices(int id)
    {
        var server = await _serverService.GetServerByIdAsync(id);
        if (server == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        var services = await _serverService.GetServerServicesAsync(id);
        return Ok(services);
    }

    [HttpPost("{id:int}/test-connection")]
    public async Task<ActionResult<TestConnectionResult>> TestConnection(int id, [FromQuery] bool activate = false)
    {
        var serverDto = await _serverService.GetServerByIdAsync(id);
        if (serverDto == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        var server = new Server
        {
            Id = serverDto.Id,
            Name = serverDto.Name,
            Host = serverDto.Host,
            Port = serverDto.Port,
            Username = serverDto.Username
        };

        var result = await _sshService.TestConnectionAsync(server);
        
        if (result.Success && activate)
        {
            await _serverService.UpdateServerAsync(id, new UpdateServerRequest(null, null, null, null, true, null));
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/setup-ssh-key")]
    public async Task<ActionResult<SetupSshKeyResult>> SetupSshKey(int id, [FromBody] SetupSshKeyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required" });

        var serverDto = await _serverService.GetServerByIdAsync(id);
        if (serverDto == null)
            return NotFound(new { message = $"Server with ID {id} not found" });

        if (string.IsNullOrEmpty(serverDto.PublicKey))
            return BadRequest(new { message = "Server does not have a public key generated" });

        var server = new Server
        {
            Id = serverDto.Id,
            Name = serverDto.Name,
            Host = serverDto.Host,
            Port = serverDto.Port,
            Username = serverDto.Username
        };

        var result = await _sshService.SetupSshKeyAsync(server, request.Password, serverDto.PublicKey);
        return Ok(result);
    }
}
