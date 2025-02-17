using System;
using JetBrains.Annotations;
using NLog;
using PER.Abstractions;
using PER.Abstractions.Meta;
using PER.Util;

namespace PER.Headless;

[PublicAPI]
public static class Engine {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public static bool running { get; set; }

    public static FrameTime frameTime { get; } = new();

    public static TimeSpan tickInterval { get; set; }

    private static readonly Stopwatch clock = new();
    private static TimeSpan _lastUpdateTime;
    private static TimeSpan _lastTickTime;

    [RequiresBody]
    public static void Run() {
        AppDomain.CurrentDomain.UnhandledException += (_, args) => {
            logger.Fatal(args.ExceptionObject as Exception,
                "Uncaught exception! Please, report this file to the developer of the game.");
        };

        logger.Info($"PER v{version}");
        logger.Info("RUNNING IN HEADLESS MODE");
        logger.Info("Loading");

        game.Load();
        resources.Load();
        game.Loaded();

        logger.Info("Setting up");
        (game as ISetupable)?.Setup();

        logger.Info("Starting");
        running = true;
        clock.Reset();
        while (running) {
            TryTick(clock.time);
            if (tickInterval <= TimeSpan.Zero)
                continue;
            TimeSpan time = clock.time;
            if (tickInterval > TimeSpan.Zero && time - _lastUpdateTime < tickInterval)
                System.Threading.Thread.Sleep(tickInterval - (time - _lastUpdateTime));
            _lastUpdateTime = clock.time;
        }

        logger.Info("Unloading");
        resources.Unload();
        game.Unload();

        game.Finish();
        logger.Info("nooooooo *dies*");
        LogManager.Shutdown();
    }

    [RequiresBody]
    public static void SoftReload() {
        logger.Info("Starting soft reload");
        resources.SoftReload();
        game.Loaded();
    }

    [RequiresBody]
    private static void TryTick(TimeSpan time) {
        if (tickInterval < TimeSpan.Zero) {
            _lastTickTime = time;
            return;
        }

        while (time - _lastTickTime >= tickInterval) {
            _lastTickTime += tickInterval;
            // ReSharper disable once SuspiciousTypeConversion.Global
            (game as ITickable)?.Tick(_lastTickTime);
        }
    }
}
