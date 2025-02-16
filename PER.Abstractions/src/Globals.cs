using System;
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
    public static IResources? resources { get; set; }
    public static IRenderer? renderer { get; set; }
    public static IScreens? screens { get; set; }
    public static IInput? input { get; set; }
    public static IAudio? audio { get; set; }
    public static IGame? game { get; set; }

    // TODO: wait for https://youtrack.jetbrains.com/issue/RIDER-45021 or not use nullables for this at all
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
    [RequiresBody, MemberNotNull(nameof(resources), nameof(game))]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RequireBody() { }

    [RequiresHead, MemberNotNull(nameof(renderer), nameof(screens), nameof(input), nameof(audio))]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RequireHead() { }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.
}
