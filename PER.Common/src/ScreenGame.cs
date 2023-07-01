using System.Globalization;

using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Common.Effects;
using PER.Util;

namespace PER.Common;

[PublicAPI]
public abstract class ScreenGame : IGame {
    private const float StartupWaitTime = 0.5f;
    private const float StartupFadeTime = 2f;
    private const float ShutdownFadeTime = 2f;
    private const float FadeTime = 0.3f;

    protected abstract FrameTime? frameTime { get; }
    protected abstract IRenderer renderer { get; }

    public IScreen? currentScreen { get; private set; }
    private readonly FadeEffect _screenFade = new();

    private Func<char, Formatting> _frameTimeFormatter = _ => new Formatting(Color.white, Color.transparent);

    public void SwitchScreen(IScreen? screen, Func<bool>? middleCallback = null) {
        if(currentScreen is null)
            SwitchScreen(screen, StartupWaitTime, StartupFadeTime, middleCallback);
        else if(screen is null)
            SwitchScreen(screen, ShutdownFadeTime, 0f, middleCallback);
        else
            SwitchScreen(screen, FadeTime, FadeTime, middleCallback);
    }

    public void SwitchScreen(IScreen? screen, float fadeOutTime, float fadeInTime, Func<bool>? middleCallback = null) =>
        FadeScreen(fadeOutTime, fadeInTime, () => {
            if(middleCallback is not null && !middleCallback.Invoke())
                return;
            currentScreen?.Close();
            currentScreen = screen;
            currentScreen?.Open();
            if(currentScreen is null)
                renderer.Close();
        });

    public void FadeScreen(Action middleCallback) =>
        _screenFade.Start(FadeTime, FadeTime, middleCallback);

    public void FadeScreen(float fadeOutTime, float fadeInTime, Action middleCallback) =>
        _screenFade.Start(fadeOutTime, fadeInTime, middleCallback);

    public abstract void Unload();
    public abstract void Load();
    public abstract RendererSettings Loaded();

    public virtual void Setup() {
        renderer.closed += (_, _) => SwitchScreen(null);
        _frameTimeFormatter = flag =>
            frameTime is null ? new Formatting(Color.white, Color.transparent) : FrameTimeFormatter(frameTime, flag);
    }

    public virtual void Update(TimeSpan time) {
        if(_screenFade.fading)
            renderer.AddEffect(_screenFade);

        currentScreen?.Update(time);

        if(currentScreen != null)
            DrawFrameTime();
    }

    private void DrawFrameTime() {
        if(frameTime is null)
            return;
        CultureInfo c = CultureInfo.InvariantCulture;
        const int maxFrameTimeLength = 6;
        const int maxFpsLength = 8;
        Span<char> finalFrameTime = stackalloc char[12 + maxFrameTimeLength + maxFrameTimeLength];
        Span<char> finalFps = stackalloc char[13 + maxFpsLength + maxFpsLength];

        int frameTimeLength = 2;
        finalFrameTime[0] = '\f';
        finalFrameTime[1] = '1';

        frameTime.frameTime.TotalMilliseconds.TryFormat(finalFrameTime[frameTimeLength..], out int written, "F2", c);
        if(written > maxFrameTimeLength) {
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
                "\f1ERROR\f\0/\f2ERROR\f\0 ms", _frameTimeFormatter, HorizontalAlignment.Right);
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
                "\faERROR\f\0/\fbERROR\f\0 FPS", _frameTimeFormatter, HorizontalAlignment.Right);
            return;
        }
        frameTimeLength += written;
        finalFrameTime[frameTimeLength++] = '\f';
        finalFrameTime[frameTimeLength++] = '\0';
        finalFrameTime[frameTimeLength++] = '/';
        finalFrameTime[frameTimeLength++] = '\f';
        finalFrameTime[frameTimeLength++] = '2';

        frameTime.averageFrameTime.TotalMilliseconds.TryFormat(finalFrameTime[frameTimeLength..], out written, "F2", c);
        if(written > maxFrameTimeLength) {
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
                "\f1ERROR\f\0/\f2ERROR\f\0 ms", _frameTimeFormatter, HorizontalAlignment.Right);
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
                "\faERROR\f\0/\fbERROR\f\0 FPS", _frameTimeFormatter, HorizontalAlignment.Right);
            return;
        }
        frameTimeLength += written;
        finalFrameTime[frameTimeLength++] = '\f';
        finalFrameTime[frameTimeLength++] = '\0';
        finalFrameTime[frameTimeLength++] = ' ';
        finalFrameTime[frameTimeLength++] = 'm';
        finalFrameTime[frameTimeLength++] = 's';

        int fpsLength = 2;
        finalFps[0] = '\f';
        finalFps[1] = 'a';

        frameTime.fps.TryFormat(finalFps[fpsLength..], out written, "F1", c);
        if(written > maxFpsLength) {
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
                "\f1ERROR\f\0/\f2ERROR\f\0 ms", _frameTimeFormatter, HorizontalAlignment.Right);
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
                "\faERROR\f\0/\fbERROR\f\0 FPS", _frameTimeFormatter, HorizontalAlignment.Right);
            return;
        }
        fpsLength += written;
        finalFps[fpsLength++] = '\f';
        finalFps[fpsLength++] = '\0';
        finalFps[fpsLength++] = '/';
        finalFps[fpsLength++] = '\f';
        finalFps[fpsLength++] = 'b';

        frameTime.averageFps.TryFormat(finalFps[fpsLength..], out written, "F1", c);
        if(written > maxFpsLength) {
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
                "\f1ERROR\f\0/\f2ERROR\f\0 ms", _frameTimeFormatter, HorizontalAlignment.Right);
            renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
                "\faERROR\f\0/\fbERROR\f\0 FPS", _frameTimeFormatter, HorizontalAlignment.Right);
            return;
        }
        fpsLength += written;
        finalFps[fpsLength++] = '\f';
        finalFps[fpsLength++] = '\0';
        finalFps[fpsLength++] = ' ';
        finalFps[fpsLength++] = 'F';
        finalFps[fpsLength++] = 'P';
        finalFps[fpsLength++] = 'S';

        renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 2),
            finalFrameTime[..frameTimeLength], _frameTimeFormatter, HorizontalAlignment.Right);
        renderer.DrawText(new Vector2Int(renderer.width - 1, renderer.height - 1),
            finalFps[..fpsLength], _frameTimeFormatter, HorizontalAlignment.Right);
    }

    protected virtual Formatting FrameTimeFormatter(FrameTime frameTime, char flag) =>
        new(Color.white, Color.transparent);

    public virtual void Tick(TimeSpan time) => currentScreen?.Tick(time);

    public virtual void Finish() { }
}
