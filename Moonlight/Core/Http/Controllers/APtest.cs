using Microsoft.AspNetCore.Mvc;
using MoonCore.Attributes;

using Moonlight.Features.Servers.Entities;
using Moonlight.Core.Database.Entities;
using Moonlight.Core.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Moonlight.Core.Models.Abstractions;
using MoonCore.Abstractions;
using Mono.Unix.Native;
using MoonCore.Helpers;
using Moonlight.Features.Servers.Services;
using Moonlight.Core.UI.Views.Admin.Sys;
using MoonCore.Services;
using Moonlight.Core.Configuration;

namespace Moonlight.ApiServer.Http.Controllers.Admin.Sys;

public class SystemOverviewResponse
{
    public int CpuUsage { get; set; }
    public long MemoryUsage { get; set; }
    public string OperatingSystem { get; set; }
    public TimeSpan Uptime { get; set; }
}



[Singleton]
public class ApplicationService
{

    private ILogger<ApplicationService> Logger;
    private readonly IHost Host;

    public ApplicationService(ILogger<ApplicationService> logger, IHost host)
    {
        Logger = logger;
        Host = host;
    }

    public Task<string> GetOsName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows platform detected
            var osVersion = Environment.OSVersion.Version;
            return Task.FromResult($"Windows {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var releaseRaw = File
                .ReadAllLines("/etc/os-release")
                .FirstOrDefault(x => x.StartsWith("PRETTY_NAME="));

            if (string.IsNullOrEmpty(releaseRaw))
                return Task.FromResult("Linux (unknown release)");

            var release = releaseRaw
                .Replace("PRETTY_NAME=", "")
                .Replace("\"", "");

            if (string.IsNullOrEmpty(release))
                return Task.FromResult("Linux (unknown release)");

            return Task.FromResult(release);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS platform detected
            var osVersion = Environment.OSVersion.Version;
            return Task.FromResult($"macOS {osVersion.Major}.{osVersion.Minor}.{osVersion.Build}");
        }

        // Unknown platform
        return Task.FromResult("N/A");
    }

    public Task<long> GetMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var bytes = process.PrivateMemorySize64;
        return Task.FromResult(bytes);
    }

    public Task<TimeSpan> GetUptime()
    {
        var process = Process.GetCurrentProcess();
        var uptime = DateTime.Now - process.StartTime;
        return Task.FromResult(uptime);
    }

    public Task<int> GetCpuUsage()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime;
        var wallClockTime = DateTime.UtcNow - process.StartTime.ToUniversalTime();

        var cpuUsage = (int)(100.0 * cpuTime.TotalMilliseconds / wallClockTime.TotalMilliseconds / Environment.ProcessorCount);

        return Task.FromResult(cpuUsage);
    }

    public Task Shutdown()
    {
        Logger.LogCritical("Restart of api server has been requested");

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await Host.StopAsync(CancellationToken.None);
        });

        return Task.CompletedTask;
    }
}


// System Stuff
[ApiController]
[Route("api/admin/system")]
public class SystemController : Controller
{
    //@inject Repository<ServerAllocation> AllocRepository

    private readonly Repository<User> _userRepository;
    private readonly ApplicationService _applicationService;
    private readonly Repository<Server> _serverRepository;
    private readonly ConfigService<CoreConfiguration> _configService;
    private readonly Repository<ServerNode> _nodeRepository;
    private readonly ServerService _serverService;
    private readonly Repository<ServerAllocation> _allocRepository;

    //private IEnumerable<ServerAllocation> GetAllocation(Server server)
    //{
    //    if (server == null)
    //        return Array.Empty<ServerAllocation>();

    //    if (server.Node == null)
    //        return Array.Empty<ServerAllocation>();

    //    return server.Allocations.Concat(
    //        _allocRepository
    //            .Get().
    //            .FromSqlRaw($"SELECT * FROM `ServerAllocations` WHERE ServerId IS NULL AND ServerNodeId = {server.Node.Id}"));
    //}
    //Repository<Server> ServerRepository

    // Single constructor for dependency injection
    public SystemController(Repository<User> userRepository, ApplicationService applicationService, Repository<Server> serverRepository, ConfigService<CoreConfiguration> configService, Repository<ServerNode> nodeRepository, ServerService serverService, Repository<ServerAllocation> allocRepository)

    {
        _serverService = serverService;
        _userRepository = userRepository;
        _applicationService = applicationService;
        _serverRepository = serverRepository;
        _configService = new ConfigService<CoreConfiguration>(
            PathBuilder.File("storage", "configs", "core.json")
        );
        _nodeRepository = nodeRepository;
        _allocRepository = allocRepository;
    }

    [HttpGet("info")]
    public async Task<SystemOverviewResponse> GetOverview()
    {
        return new SystemOverviewResponse
        {
            Uptime = await _applicationService.GetUptime(),
            CpuUsage = await _applicationService.GetCpuUsage(),
            MemoryUsage = await _applicationService.GetMemoryUsage(),
            OperatingSystem = await _applicationService.GetOsName()
        };
    }
    [HttpGet("serverlist")]
    public async Task<IActionResult> ListServers()
    {
        var Servers = _serverRepository
            .Get()
            .ToArray();
        return Ok(Servers);
    }
    [HttpGet("nodelist")]
    public async Task<IActionResult> ListNodes()
    {
        var Nodes = _nodeRepository
            .Get()
            .ToArray();
        return Ok(Nodes);
    }

    [HttpGet("servercreate")]
    public async Task<IActionResult> CreateServer()
    {
        var ServerImages = new ServerImage();

        var server = new Server
        {
            Name = "MyServer",
            Owner = new User
            {
                Id = 2,
                Username = "zachlh",
                Email = "admin@zarc.dev"
            },
            Image = new ServerImage
            {
                Id = 1,
            },
            //DockerImageIndex = 0,
            OverrideStartupCommand = "/bin/bash",
            Cpu = 200,
            Memory = 1024,
            Disk = 10,
            UseVirtualDisk = false,
            Node = new ServerNode
            {
                Id = 1,
                Name = "MainNode"
            },
            DisablePublicNetwork = false,
            MainAllocation = new ServerAllocation
            {
                Port = 8080
            },
            //Allocations = new List<ServerAllocation>
            //{
             
            //},
            //Variables = new List<ServerVariable>
            //{

            //},
            //Backups = new List<ServerBackup>
            //{

            //},
            //Schedules = new List<ServerSchedule>
            //{

            //}
        };
        var result = await _serverService.Create(server);

        return Ok(result);
    }

    [HttpPost("shutdown")]
    public async Task<IActionResult> Shutdown()
    {

        await _applicationService.Shutdown();
        return Ok("System shutdown initiated.");
    }


    // User Stuff

    [HttpPost("users/register")]
    public async Task<IActionResult> Register([FromBody] User registrationModel)
    {
        if (registrationModel == null)
        {
            return BadRequest("Invalid request data.");
        }
        registrationModel.Password = HashHelper.HashToString(registrationModel.Password);
        var finishedUser = _userRepository.Add(registrationModel);

        var Response = await Task.FromResult<User?>(finishedUser);

        return Ok("User registered successfully, "+ Response);
    }

    [HttpGet("users/userlist")]
    public async Task<IActionResult> ListUsers()
    {
        var Users = _userRepository
            .Get()
            .ToArray();
        return Ok(Users);
    }




}