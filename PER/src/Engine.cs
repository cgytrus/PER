using System;
using JetBrains.Annotations;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER;

[PublicAPI]
public static class Engine {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public static bool running { get; set; }

    public static FrameTime frameTime { get; } = new();

    public static TimeSpan updateInterval { get; set; }
    public static TimeSpan tickInterval { get; set; }

    // TODO: not sure what's the best way to force Game.Loaded() to set rendererSettings here
    public static RendererSettings rendererSettings { get; set; }

    private static readonly Stopwatch clock = new();
    private static TimeSpan _lastUpdateTime;
    private static TimeSpan _lastTickTime;

    [RequiresBody, RequiresHead]
    public static void Run() {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            logger.Fatal(args.ExceptionObject as Exception,
                "Uncaught exception! Please, report this file to the developer of the game.");
        };

        logger.Info($"PER v{version}");

        renderer.closed += (_, _) => running = false;
        running = true;
        while (running) {
            logger.Info("Loading");
            game.PreLoad();
            resources.Load();
            game.Load();

            logger.Info("Setting up");
            renderer.Setup(rendererSettings);
            input.Setup();
            audio.Setup();
            (screens as ISetupable)?.Setup();
            (game as ISetupable)?.Setup();

            logger.Info("Starting");
            clock.Reset();
            while (renderer.open)
                Update();

            game.Unload();

            input.Finish();
            renderer.Finish();
            audio.Finish();
            game.Finish();
        }

        logger.Info("nooooooo *dies*");
        LogManager.Shutdown();
    }

    [RequiresHead]
    public static void Reload() {
        logger.Info("Starting full reload");
        renderer.Close();
        running = true;
    }

    [RequiresBody, RequiresHead]
    public static void SoftReload() {
        logger.Info("Starting soft reload");
        renderer.Finish();
        input.Finish();
        resources.SoftReload();
        game.Loaded();
        renderer.Setup(rendererSettings);
        input.Setup();
    }

    [RequiresBody, RequiresHead]
    private static void Update() {
        // 1. vsync handles limiting for us
        // 2. updateInterval <= 0 means no limit
        if (!renderer.verticalSync && updateInterval > TimeSpan.Zero) {
            // not using Thread.Sleep here because it's so inaccurate like holy fuck look at this
            // https://media.discordapp.net/attachments/1119585041203347496/1124699614491181117/image.png
            // (that was with a 60 fps limit)
            TimeSpan updateTime = clock.time;
            if (updateTime - _lastUpdateTime < updateInterval)
                return;
            _lastUpdateTime = updateTime;
        }

        TimeSpan time = clock.time;
        input.Update(time);

        renderer.BeginDraw();

        TryTick(time);
        (screens as IUpdatable)?.Update(time);
        (game as IUpdatable)?.Update(time);

        renderer.EndDraw();

        frameTime.Update(clock.time);
    }

    [RequiresBody, RequiresHead]
    private static void TryTick(TimeSpan time) {
        if (tickInterval < TimeSpan.Zero) {
            _lastTickTime = time;
            return;
        }

        while (time - _lastTickTime >= tickInterval) {
            _lastTickTime += tickInterval;
            (screens as ITickable)?.Tick(_lastTickTime);
            // ReSharper disable once SuspiciousTypeConversion.Global
            (game as ITickable)?.Tick(_lastTickTime);
        }
    }
}
