﻿using Moonlight.App.Event;
using Moonlight.App.Extensions;
using Moonlight.App.Helpers;

namespace Moonlight.App.Services.Sys;

public class MoonlightService // This service can be used to perform strictly panel specific actions
{
    private readonly ConfigService ConfigService;
    private readonly IServiceProvider ServiceProvider;
    
    public WebApplication Application { get; set; } // Do NOT modify using a plugin
    public MoonlightThemeService Theme { get; set; }
    
    public MoonlightService(ConfigService configService, IServiceProvider serviceProvider)
    {
        ConfigService = configService;
        ServiceProvider = serviceProvider;
    }

    public async Task Restart()
    {
        Logger.Info("Restarting moonlight");
        
        // Notify all users that this instance will restart
        await Events.OnMoonlightRestart.InvokeAsync();
        await Task.Delay(TimeSpan.FromSeconds(3));

        await Application.StopAsync();
    }
}