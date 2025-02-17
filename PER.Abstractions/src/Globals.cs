using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using PER.Abstractions.Audio;
using PER.Abstractions.Input;
using PER.Abstractions.Meta;
using PER.Abstractions.Rendering;
using PER.Abstractions.Resources;
using PER.Abstractions.Screens;

namespace PER.Abstractions;

public static class Globals {
    [RequiresBody]
    public static IResources resources { get; set; } = null!;
    [RequiresHead]
    public static IRenderer renderer { get; set; } = null!;
    [RequiresHead]
    public static IScreens screens { get; set; } = null!;
    [RequiresHead]
    public static IInput input { get; set; } = null!;
    [RequiresHead]
    public static IAudio audio { get; set; } = null!;
    [RequiresBody]
    public static IGame game { get; set; } = null!;
}
