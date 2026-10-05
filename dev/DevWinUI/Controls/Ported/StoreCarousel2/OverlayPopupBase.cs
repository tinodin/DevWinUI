using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevWinUI;

/// <summary>
/// Base class for fullscreen overlay viewers. Faithful to
/// <c>WinStore.UX.Common.OverlayPopupBase</c>: a <c>UserControl</c> with two Boolean
/// dependency properties, both default <c>false</c>, no property-changed callbacks
/// (binary metatable <c>0x181F87F41..0x181F87FF2</c>; DP name <c>IsFullScreenPopup</c>
/// via string xrefs; <c>CloseOnNavigateBack</c> per reconstruction).
/// No behavior is ported on top: none is verified in the Store open/close paths,
/// so neither DP is read or written by the port.
/// </summary>
public class OverlayPopupBase : UserControl
{
    public static readonly DependencyProperty CloseOnNavigateBackProperty =
        DependencyProperty.Register(nameof(CloseOnNavigateBack), typeof(bool), typeof(OverlayPopupBase), new PropertyMetadata(false));

    public bool CloseOnNavigateBack
    {
        get => (bool)GetValue(CloseOnNavigateBackProperty);
        set => SetValue(CloseOnNavigateBackProperty, value);
    }

    public static readonly DependencyProperty IsFullScreenPopupProperty =
        DependencyProperty.Register(nameof(IsFullScreenPopup), typeof(bool), typeof(OverlayPopupBase), new PropertyMetadata(false));

    public bool IsFullScreenPopup
    {
        get => (bool)GetValue(IsFullScreenPopupProperty);
        set => SetValue(IsFullScreenPopupProperty, value);
    }
}
