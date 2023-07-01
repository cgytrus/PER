using System.Globalization;

using JetBrains.Annotations;

using PER.Abstractions;
using PER.Abstractions.Rendering;
using PER.Util;

namespace PER.Common;

[PublicAPI]
public class FrameTimeDisplay : IUpdatable {
    protected FrameTime frameTime { get; }
    protected IRenderer renderer { get; }

    private Func<char, Formatting> _frameTimeFormatter;

    public FrameTimeDisplay(FrameTime frameTime, IRenderer renderer,
        Func<FrameTime, char, Formatting> frameTimeFormatter) {
        this.frameTime = frameTime;
        this.renderer = renderer;
        _frameTimeFormatter = flag => frameTimeFormatter(this.frameTime, flag);
    }

    public void Update(TimeSpan time) {
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
}
