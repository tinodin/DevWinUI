using Microsoft.UI.Xaml;

namespace DevWinUI;

/// <summary>
/// Per-instance overlay host manager. Projection of the Store app-global overlay service
/// (state globals around <c>qword_18632BB30</c>): tracks the currently shown overlay viewer.
/// Only Store-verified transitions are implemented:
/// <list type="bullet">
/// <item>Show = close the current overlay when it is a <see cref="PreviewViewer"/>
/// (<c>sub_180D8FDF0</c> EEType dispatch calls <c>Close 0x182172170</c>), then register the
/// viewer as current (<c>sub_1801CA150(…, a1)</c>) and the host show (property slot 22 with
/// arg <c>0</c>).</item>
/// <item>Hide = host hide (property slot 22 with arg <c>1</c>) + unregister, returning the
/// verified removed bool (<c>false</c> only when the viewer is null — the <c>a1 == null</c> path).</item>
/// </list>
/// The host is the cached overlay host object (<c>[manager+0x158]</c>, created by
/// <c>sub_180440ED0</c>); its hosted-content accessor is property slot 6/7 (getter
/// <c>sub_1801C7600</c>, setter <c>sub_1801CA150</c>).
/// <b>Host placement:</b> the host popup is standalone (created by the viewer), so the overlay
/// covers the whole window rather than the control's bounds. Because such a popup inherits no
/// theme, the viewer mirrors the owning control's <c>ActualTheme</c> onto itself
/// (<c>PreviewViewer.SetThemeSource</c>); DevWinUI assigns <c>RequestedTheme</c> to
/// <c>window.Content</c> only (<c>ThemeService.ElementTheme.cs:79</c>) and never sets
/// <c>Application.Current.RequestedTheme</c>, so without the mirror the overlay's
/// <c>{ThemeResource}</c> values stay frozen until the page is reloaded.
/// Other Store overlay EETypes have no port equivalent, so no branches exist for them.
/// </summary>
public sealed class PreviewOverlayManager
{
    /// <summary>
    /// The currently shown overlay viewer (Store current-overlay slot). Null when none.
    /// </summary>
    public PreviewViewer Current { get; private set; }

    /// <summary>
    /// Registers <paramref name="viewer"/> as the current overlay and shows it.
    /// Store <c>OpenOverlayPopup 0x182172020</c> → <c>sub_180D8FDF0</c>, in order:
    /// close current overlay, <c>host.Content = viewer</c> (<c>sub_1801CA150</c>),
    /// <c>host.Visibility = Visible</c> (property slot 22, arg 0).
    /// </summary>
    public void ShowOverlay(PreviewViewer viewer)
    {
        if (viewer == null)
        {
            return;
        }

        var current = Current;
        if (current != null)
        {
            current.Close();
        }

        Current = viewer;
        viewer.AttachToOverlayHost();
        viewer.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides <paramref name="viewer"/>, unregisters it and clears the host content.
    /// Store <c>RemoveOverlayPopup 0x180D91CA0</c>: property slot 22 with arg <c>1</c>
    /// (<c>Collapsed</c>) first, then <c>sub_1801CA150(host, 0)</c>. Returns <c>false</c> only
    /// for a null viewer.
    /// </summary>
    public bool HideOverlay(PreviewViewer viewer)
    {
        if (viewer == null)
        {
            return false;
        }

        if (ReferenceEquals(Current, viewer))
        {
            viewer.Visibility = Visibility.Collapsed;
            Current = null;
            viewer.DetachFromOverlayHost();
        }

        return true;
    }
}
