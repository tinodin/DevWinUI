using System.Collections;

namespace DevWinUI;

/// <summary>
/// The product-page screenshots carousel panel. Hosts <see cref="ScreenshotsViewer"/>
/// directly (mirroring Store's ScreenshotsResponsive → ScreenshotsViewer, no ColumnedGrid/CrossFade)
/// and opens the fullscreen <see cref="PreviewViewer"/> with a connected animation when the card is clicked.
/// Ported from the Microsoft Store PDP screenshots carousel
/// (<c>WinStore.UX.Controls.PDP.ScreenshotsViewer</c> + <c>PreviewViewer</c>).
/// </summary>
[TemplatePart(Name = "ScreenshotsViewerPart", Type = typeof(ScreenshotsViewer))]
public partial class StoreCarousel2 : Control
{
    private ScreenshotsViewer _card;
    private PreviewViewer _viewer;
    private int _viewerTransitionVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreCarousel2"/> class.
    /// </summary>
    public StoreCarousel2()
    {
        DefaultStyleKey = typeof(StoreCarousel2);
    }

    /// <summary>
    /// Occurs when the card is clicked. The built-in viewer still opens unless
    /// <see cref="IsViewerEnabled"/> is <see langword="false"/>.
    /// </summary>
    public event EventHandler ScreenshotClicked;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCard();

        _card = GetTemplateChild("ScreenshotsViewerPart") as ScreenshotsViewer;
        if (_card != null)
        {
            _card.ScreenshotClicked += OnScreenshotClicked;
            if (ItemsSource != null)
            {
                var vm = new PreviewModuleViewModel();
                foreach (var item in ItemsSource) vm.Items.Add(item);
                _card.PreviewViewModel = vm;
            }
            _card.AgeRestricted = AgeRestricted;
        }
    }

    private void DetachCard()
    {
        if (_card != null)
        {
            _card.ScreenshotClicked -= OnScreenshotClicked;
            _card = null;
        }
    }

    private void OnScreenshotClicked(object sender, object e)
    {
        var index = e is int clickedIndex ? clickedIndex : _card?.LastClickedIndex ?? 0;
        OnCardClicked(sender, EventArgs.Empty, index);
    }

    private void OnCardClicked(object sender, EventArgs e, int index)
    {
        ScreenshotClicked?.Invoke(this, EventArgs.Empty);

        if (IsViewerEnabled)
        {
            OpenViewer(index);
        }
    }

    private void OpenViewer(int requestedIndex)
    {
        var transitionVersion = ++_viewerTransitionVersion;
        if (_viewer == null)
        {
            _viewer = new PreviewViewer();
            _viewer.Closed += OnViewerClosed;
        }

        _viewer.XamlRoot = XamlRoot;
        var vm = new PreviewItemsViewModel();
        if (ItemsSource != null)
            foreach (var item in ItemsSource) vm.Items.Add(item);
        var itemCount = vm.Items.Count;
        var idx = itemCount == 0 ? 0 : Math.Clamp(requestedIndex, 0, itemCount - 1);
        if (itemCount > 0)
            vm.CurrentItem = vm.Items[idx];
        _viewer.ViewModel = vm;
        _viewer.AgeRestricted = AgeRestricted;

        UIElement source = null;
        if (_card?.TileList != null)
        {
            try
            {
                if (idx >= 0 && idx < _card.TileList.Items.Count && _card.TileList.ContainerFromIndex(idx) is FrameworkElement container)
                {
                    source = FindDescendant<Image>(container)
                          ?? FindDescendant<TileImage>(container)
                          ?? container;
                }
            }
            catch { }
            source ??= _card.TileList as UIElement ?? _card as UIElement;
        }
        else
        {
            source = _card as UIElement;
        }

        _viewer.Show();

        if (source != null)
        {
            try
            {
                var animation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("screenshotForwardAnimation", source);
                if (animation != null)
                {
                    animation.Configuration = new DirectConnectedAnimationConfiguration();
                    _viewer.StartConnectedAnimation(animation);
                }
            }
            catch { }
        }
    }

    private void OnViewerClosed(object sender, EventArgs e)
    {
        if (_viewer == null || _card == null)
        {
            return;
        }

        var finalIndex = _viewer.SelectedIndex;
        _card.SelectedIndex = finalIndex;
        var transitionVersion = ++_viewerTransitionVersion;

        if (_card.TileList != null)
        {
            if (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count)
            {
                _card.TileList.ScrollIntoView(_card.TileList.Items[finalIndex]);
            }

            _ = _card.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                // A fast close/open can leave this callback queued behind the
                // next open. Do not let an old back animation steal input or
                // move the card after a newer viewer session has started.
                if (transitionVersion != _viewerTransitionVersion || _viewer?.IsOpen == true)
                    return;

                UIElement target = null;
                try
                {
                    if (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count && _card.TileList.ContainerFromIndex(finalIndex) is FrameworkElement container)
                    {
                        target = FindDescendant<Image>(container)
                              ?? FindDescendant<TileImage>(container)
                              ?? container;
                    }
                }
                catch { }
                target ??= _card.TileList as UIElement ?? _card as UIElement;

                if (target != null)
                {
                    try
                    {
                        var backAnim = ConnectedAnimationService.GetForCurrentView().GetAnimation("screenshotBackAnimation");
                        if (backAnim != null)
                        {
                            backAnim.Configuration = new DirectConnectedAnimationConfiguration();
                            backAnim.TryStart(target);
                        }
                    }
                    catch { }
                }
            });
        }
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var desc = FindDescendant<T>(child);
            if (desc != null) return desc;
        }
        return null;
    }
}
