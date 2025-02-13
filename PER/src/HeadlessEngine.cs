using System;

using JetBrains.Annotations;

using NLog;

using PER.Abstractions;
using PER.Abstractions.Resources;
using PER.Util;

namespace PER;

[PublicAPI]
public class HeadlessEngine(IResources resources, IGame game) {
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static readonly string version = Helper.GetVersion();
    public static readonly string abstractionsVersion = Helper.GetVersion(typeof(IGame));

    public bool running { get; set; }

    public FrameTime frameTime { get; } = new();

    public TimeSpan tickInterval { get; set; }

    public IResources resources { get; } = resources;
    public IGame game { get; } = game;

    private readonly Stopwatch _clock = new();
    private TimeSpan _lastUpdateTime;
    private TimeSpan _lastTickTime;

    public void Run() {
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
        if(game is ISetupable setupableGame)
            setupableGame.Setup();

        logger.Info("Starting");
        running = true;
        _clock.Reset();
        while(running) {
            TryTick(_clock.time);
            if(tickInterval <= TimeSpan.Zero)
                continue;
            TimeSpan time = _clock.time;
            if(tickInterval > TimeSpan.Zero && time - _lastUpdateTime < tickInterval)
                System.Threading.Thread.Sleep(tickInterval - (time - _lastUpdateTime));
            _lastUpdateTime = _clock.time;
        }

        logger.Info("Unloading");
        resources.Unload();
        game.Unload();

        game.Finish();
        logger.Info("nooooooo *dies*");
        LogManager.Shutdown();
    }

    public void SoftReload() {
        logger.Info("Starting soft reload");
        resources.SoftReload();
        game.Loaded();
    }

    private void TryTick(TimeSpan time) {
        if(tickInterval < TimeSpan.Zero) {
            _lastTickTime = time;
            return;
        }

        while(time - _lastTickTime >= tickInterval) {
            _lastTickTime += tickInterval;
            // ReSharper disable once SuspiciousTypeConversion.Global
            if(game is ITickable tickableGame)
                tickableGame.Tick(_lastTickTime);
        }
    }
}
