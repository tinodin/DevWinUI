// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.UI;

namespace DevWinUI.Animations;

/// <summary>
/// A <see cref="KeyFrame{TValue,TKeyFrame}"/> type for color animations.
/// </summary>
public sealed partial class ColorKeyFrame : KeyFrame<Color?, Color>
{
    /// <inheritdoc/>
    protected override Color GetParsedValue()
    {
        return Value!.Value;
    }
}
