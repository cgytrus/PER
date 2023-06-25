using System;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Headless;
using PER.Util;

namespace PER;

[PublicAPI]
public class Engine {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public FrameTime frameTime { get; } = new();

    public TimeSpan tickInterval { get; set; }
    public IResources resources { get; }
    public IGame game { get; }
    public IRenderer renderer { get; }
    public IInput input { get; }
    public IAudio audio { get; }

    private bool _headless;

    private readonly Stopwatch _clock = new();
    private TimeSpan _lastTickTime;

    public Engine(IResources resources, IGame game, IRenderer renderer, IInput input, IAudio audio) {
        this.resources = resources;
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
        game.Setup();
        logger.Info("Setup finished");
        while(renderer.open) {
            TimeSpan time = _clock.time;
            TryTick(time);
            frameTime.Update(time);
        }
        Finish();
    }

    private void Run(RendererSettings rendererSettings) {
        logger.Info("Starting game");
        Setup(rendererSettings);
        while(Update(_clock.time)) { }
        Finish();
    }

    private void Setup(RendererSettings rendererSettings) {
        _clock.Reset();

        renderer.Setup(rendererSettings);
        input.Reset();
        game.Setup();

        logger.Info("Setup finished");
    }

    private bool Update(TimeSpan time) {
        renderer.Clear();

        input.Update(time);
        renderer.Update(time);
        game.Update(time);
        TryTick(time);

        renderer.Draw();
        frameTime.Update(time);
        return renderer.open;
    }

    private void TryTick(TimeSpan time) {
        if(tickInterval < TimeSpan.Zero) {
            _lastTickTime = time;
            return;
        }

        while(time - _lastTickTime >= tickInterval) {
            _lastTickTime += tickInterval;
            Tick(_lastTickTime);
        }
    }

    private void Tick(TimeSpan time) => game.Tick(time);

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
