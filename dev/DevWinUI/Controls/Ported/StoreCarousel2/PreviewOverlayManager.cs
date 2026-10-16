namespace DevWinUI;

public sealed class PreviewOverlayManager
{
    public PreviewViewer Current { get; private set; }

    public void ShowOverlay(PreviewViewer viewer)
    {
        if (viewer == null)
        {
            return;
        }

        var current = Current;
        current?.Close();

        Current = viewer;
        viewer.AttachToOverlayHost();
        viewer.Visibility = Visibility.Visible;
    }

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
