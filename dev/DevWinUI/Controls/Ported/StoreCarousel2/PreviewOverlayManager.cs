using Microsoft.UI.Xaml;

namespace DevWinUI;

/// <summary>
/// Per-instance overlay host manager. Projection of the Store app-global overlay service
/// (state globals around <c>qword_18632BB30</c>): tracks the currently shown overlay viewer.
/// Only Store-verified transitions are implemented:
/// <list type="bullet">
/// <item>Show = close the current overlay when it is a <see cref="PreviewViewer"/>
/// (<c>sub_180D8FDF0</c> EEType dispatch calls <c>Close 0x182172170</c>), register the
/// viewer as current (<c>sub_1801CA150(…, a1)</c>), host show (manager call, arg 0 path).</item>
/// <item>Hide = host hide (manager call, arg 1 path) + unregister
/// (<c>sub_1801CA150(…, 0)</c> in <c>0x180D91CA0</c>), returning the verified removed bool
/// (<c>false</c> only when the viewer is null — the <c>a1 == null</c> path).</item>
/// </list>
/// The host show/hide is applied to the hosted viewer element. Verified: a manager call
/// with arg 0 runs on open and arg 1 on remove; the <c>Visibility</c> enum mapping
/// (<c>Visible = 0</c>, <c>Collapsed = 1</c>) is a framework fact. The exact Store
/// hide primitive (vtable slot on an unidentified manager object) has no symbols and is
/// flagged as the single unverified line (see status .md §L). Other Store overlay
/// EETypes have no port equivalent, so no branches exist for them.
/// </summary>
public sealed class PreviewOverlayManager
{
    /// <summary>
    /// The currently shown overlay viewer (Store current-overlay slot). Null when none.
    /// </summary>
    public PreviewViewer Current { get; private set; }

    /// <summary>
    /// Registers <paramref name="viewer"/> as the current overlay and shows it.
    /// Store <c>OpenOverlayPopup 0x182172020</c> → <c>sub_180D8FDF0</c>.
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
        viewer.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides <paramref name="viewer"/> and unregisters it. Store
    /// <c>RemoveOverlayPopup 0x180D91CA0</c>. Returns <c>false</c> only for a null viewer.
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
        }

        return true;
    }
}
