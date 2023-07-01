using System;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;
using PER.Headless;
using PER.Util;

namespace PER;

[PublicAPI]
public class Engine {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public FrameTime frameTime { get; } = new();

    public TimeSpan updateInterval { get; set; }
    public TimeSpan tickInterval { get; set; }
    public IResources resources { get; }
    public IScreens screens { get; }
    public IGame game { get; }
    public IRenderer renderer { get; }
    public IInput input { get; }
    public IAudio audio { get; }

    private bool _headless;

    private readonly Stopwatch _clock = new();
    private TimeSpan _lastUpdateTime;
    private TimeSpan _lastTickTime;

    public Engine(IResources resources, IScreens screens, IGame game, IRenderer renderer, IInput input, IAudio audio) {
        this.resources = resources;
        this.screens = screens;
        this.game = game;
        this.renderer = renderer;
        this.input = input;
        this.audio = audio;
    }

    // runs the engine in headless mode
    // intended for servers
    public Engine(IResources resources, IGame game) {
        _headless = true;
        this.resources = resources;
        this.game = game;
        renderer = new HeadlessRenderer();
        // crashing with nre is fine in headless mode
        // games are supposed to implement the server in a different project
        // and not use any of the client-side stuff
        screens = null!;
        input = null!;
        audio = null!;
    }

    public void Reload() {
        try {
            if(resources.loaded) {
                logger.Info("Reloading game");
                resources.Unload();
                game.Unload();
            }
            else {
                logger.Info($"PER v{version}");
                if(_headless)
                    logger.Info("RUNNING IN HEADLESS MODE");
                logger.Info("Loading game");
            }

            game.Load();
            resources.Load();
            RendererSettings rendererSettings = game.Loaded();

            if(_headless) {
                RunHeadless(rendererSettings);
                return;
            }

            if(renderer.open && renderer.Reset(rendererSettings))
                input.Reset();
            else
                Run(rendererSettings);
        }
        catch(Exception exception) {
            logger.Fatal(exception, "Uncaught exception! Please, report this file to the developer of the game.");
            throw;
        }
    }

    public void SoftReload() {
        logger.Info("Starting soft reload");
        resources.SoftReload();
        RendererSettings rendererSettings = game.Loaded();
        if(!_headless && renderer.open && renderer.Reset(rendererSettings))
            input.Reset();
    }

    private void RunHeadless(RendererSettings rendererSettings) {
        logger.Info("Starting game");
        _clock.Reset();
        renderer.Setup(rendererSettings);
        if(screens is ISetupable setupableScreens)
            setupableScreens.Setup();
        if(game is ISetupable setupableGame)
            setupableGame.Setup();
        logger.Info("Setup finished");
        while(renderer.open) {
            TryTick(_clock.time);
            if(updateInterval <= TimeSpan.Zero)
                continue;
            TimeSpan time = _clock.time;
            if(updateInterval > TimeSpan.Zero && time - _lastUpdateTime < updateInterval)
                System.Threading.Thread.Sleep(updateInterval - (time - _lastUpdateTime));
            _lastUpdateTime = _clock.time;
        }
        Finish();
    }

    private void Run(RendererSettings rendererSettings) {
        logger.Info("Starting game");
        Setup(rendererSettings);
        while(Update()) { }
        Finish();
    }

    private void Setup(RendererSettings rendererSettings) {
        _clock.Reset();

        renderer.Setup(rendererSettings);
        input.Reset();
        if(screens is ISetupable setupableScreens)
            setupableScreens.Setup();
        if(game is ISetupable setupableGame)
            setupableGame.Setup();

        logger.Info("Setup finished");
    }

    private bool Update() {
        // 1. vsync handles limiting for us
        // 2. updateInterval <= 0 means no limit
        if(!renderer.verticalSync && updateInterval > TimeSpan.Zero) {
            // not using Thread.Sleep here because it's so inaccurate like holy fuck look at this
            // https://media.discordapp.net/attachments/1119585041203347496/1124699614491181117/image.png
            // (that was with a 60 fps limit)
            TimeSpan updateTime = _clock.time;
            if(updateTime - _lastUpdateTime < updateInterval)
                return renderer.open;
            _lastUpdateTime = updateTime;
        }

        renderer.Clear();

        TimeSpan time = _clock.time;
        input.Update(time);
        renderer.Update(time);
        if(screens is IUpdatable updatableScreens)
            updatableScreens.Update(time);
        if(game is IUpdatable updatableGame)
            updatableGame.Update(time);
        TryTick(time);

        renderer.Draw();

        frameTime.Update(_clock.time);
        return renderer.open;
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

    private void Finish() {
        resources.Unload();
        game.Unload();
        if(!_headless) {
            input.Finish();
            renderer.Finish();
        }
        game.Finish();
        if(!_headless)
            audio.Finish();

        logger.Info("nooooooo *dies*");
        LogManager.Shutdown();
    }
}
