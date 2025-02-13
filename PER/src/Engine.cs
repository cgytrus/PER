using System;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Util;

namespace PER;

[PublicAPI]
public class Engine(
    IResources resources,
    IScreens screens,
    IGame game,
    IRenderer renderer,
    IInput input,
    IAudio audio
) {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public bool running { get; set; }

    public FrameTime frameTime { get; } = new();

    public TimeSpan updateInterval { get; set; }
    public TimeSpan tickInterval { get; set; }

    // TODO: not sure what's the best way to force Game.Loaded() to set rendererSettings here
    public RendererSettings rendererSettings { get; set; }

    private readonly Stopwatch _clock = new();
    private TimeSpan _lastUpdateTime;
    private TimeSpan _lastTickTime;

    public void Run() {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            logger.Fatal(args.ExceptionObject as Exception,
                "Uncaught exception! Please, report this file to the developer of the game.");
        };

        logger.Info($"PER v{version}");

        renderer.closed += (_, _) => running = false;
        running = true;
        while(running) {
            logger.Info("Loading");
            game.Load();
            resources.Load();
            game.Loaded();

            logger.Info("Setting up");
            renderer.Setup(rendererSettings);
            input.Setup();
            if(screens is ISetupable setupableScreens)
                setupableScreens.Setup();
            if(game is ISetupable setupableGame)
                setupableGame.Setup();

            logger.Info("Starting");
            _clock.Reset();
            while(renderer.open)
                Update();

            resources.Unload();
            game.Unload();

            input.Finish();
            renderer.Finish();
            game.Finish();
        }

        audio.Finish();
        logger.Info("nooooooo *dies*");
        LogManager.Shutdown();
    }

    public void Reload() {
        logger.Info("Starting full reload");
        renderer.Close();
        running = true;
    }

    public void SoftReload() {
        logger.Info("Starting soft reload");
        renderer.Finish();
        input.Finish();
        resources.SoftReload();
        game.Loaded();
        renderer.Setup(rendererSettings);
        input.Setup();
    }

    private void Update() {
        // 1. vsync handles limiting for us
        // 2. updateInterval <= 0 means no limit
        if(!renderer.verticalSync && updateInterval > TimeSpan.Zero) {
            // not using Thread.Sleep here because it's so inaccurate like holy fuck look at this
            // https://media.discordapp.net/attachments/1119585041203347496/1124699614491181117/image.png
            // (that was with a 60 fps limit)
            TimeSpan updateTime = _clock.time;
            if(updateTime - _lastUpdateTime < updateInterval)
                return;
            _lastUpdateTime = updateTime;
        }

        TimeSpan time = _clock.time;
        input.Update(time);

        renderer.BeginDraw();

        TryTick(time);
        if(screens is IUpdatable updatableScreens)
            updatableScreens.Update(time);
        if(game is IUpdatable updatableGame)
            updatableGame.Update(time);

        renderer.EndDraw();

        frameTime.Update(_clock.time);
    }

    private void TryTick(TimeSpan time) {
        if(tickInterval < TimeSpan.Zero) {
            _lastTickTime = time;
            return;
        }

        while(time - _lastTickTime >= tickInterval) {
            _lastTickTime += tickInterval;
            if(screens is ITickable tickableScreens)
                tickableScreens.Tick(_lastTickTime);
            // ReSharper disable once SuspiciousTypeConversion.Global
            if(game is ITickable tickableGame)
                tickableGame.Tick(_lastTickTime);
        }
    }
}
