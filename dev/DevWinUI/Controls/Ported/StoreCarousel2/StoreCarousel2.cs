using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace DevWinUI;

/// <summary>
/// The product-page screenshots carousel panel. Hosts <see cref="ScreenshotsViewer"/>
/// directly (mirroring Store's ScreenshotsResponsive -> ScreenshotsViewer)
/// and coordinates with <see cref="PreviewViewerHelper"/>, <see cref="PreviewViewerHelperListViewSource"/>,
/// and the fullscreen <see cref="PreviewViewer"/> with connected animations.
/// Ported from the Microsoft Store PDP screenshots carousel
/// (<c>WinStore.UX.Controls.PDP.ScreenshotsViewer</c> + <c>PreviewViewer</c>).
/// </summary>
[TemplatePart(Name = "ScreenshotsControl", Type = typeof(ScreenshotsViewer))]
public partial class StoreCarousel2 : Control
{
    private ScreenshotsViewer _card;
    private PreviewViewer _viewer;
    private PreviewViewerHelper _helper;
    private PreviewViewerHelperListViewSource _listViewSource;
    private PreviewItemsViewModel _viewerVm;
    private PreviewListFacade _popupList;
    private int _viewerTransitionVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreCarousel2"/> class.
    /// </summary>
    public StoreCarousel2()
    {
        DefaultStyleKey = typeof(StoreCarousel2);
    }

    /// <summary>
    /// Occurs when a screenshot or video tile is clicked. The built-in viewer still opens unless
    /// <see cref="IsViewerEnabled"/> is <see langword="false"/>.
    /// </summary>
    public event EventHandler ScreenshotClicked;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachCard();

        _card = GetTemplateChild("ScreenshotsControl") as ScreenshotsViewer;
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

            EnsureHelper();
        }
    }

    private void EnsureHelper()
    {
        if (_card?.TileList == null)
        {
            return;
        }

        if (_viewer == null)
        {
            _viewer = new PreviewViewer();
            _viewer.Closed += OnViewerClosed;
        }

        // The overlay lives in a standalone popup, so it inherits no theme: mirror this control's
        // ActualTheme onto it (DevWinUI sets RequestedTheme on window.Content only).
        _viewer.SetThemeSource(this);

        _listViewSource ??= new PreviewViewerHelperListViewSource(_card.TileList);
        _listViewSource.InnerListView = _card.TileList;

        // Store keeps one preview list for the lifetime of the helper. OpenPreviewViewer
        // (0x18209E820) only writes SelectedIndex (0x1823F0170) — it does not rebuild items.
        EnsureViewerViewModel();

        if (_helper == null)
        {
            _popupList = new PreviewListFacade();
            _popupList.BindToViewModel(_viewerVm);

            _helper = new PreviewViewerHelper
            {
                PreviewViewer = _viewer,
                ListViewSource = _listViewSource,
                Popup = new PreviewPopupHost
                {
                    List = _popupList,
                    Items = _viewerVm.Items
                }
            };
        }
        else
        {
            _helper.PreviewViewer = _viewer;
            _helper.ListViewSource = _listViewSource;
            _popupList ??= new PreviewListFacade();
            _popupList.BindToViewModel(_viewerVm);
            if (_helper.Popup == null)
            {
                _helper.Popup = new PreviewPopupHost();
            }

            _helper.Popup.List = _popupList;
            _helper.Popup.Items = _viewerVm.Items;
        }

        // IndexOf (0x1823F0A40) searches list VM items at [list+0x20], not ListView.ItemCollection.
        _popupList.Items = null;
        _popupList.BindToViewModel(_viewerVm);

        if (!ReferenceEquals(_viewer.ViewModel, _viewerVm))
        {
            _viewer.ViewModel = _viewerVm;
        }

        _viewer.AgeRestricted = AgeRestricted;
        _card.Helper = IsViewerEnabled ? _helper : null;
    }

    /// <summary>
    /// Creates/fills the shared preview list once. Matching Store: items are not cleared on each click.
    /// </summary>
    private void EnsureViewerViewModel()
    {
        if (_viewerVm == null)
        {
            _viewerVm = new PreviewItemsViewModel();
            FillViewerViewModelItems();
            return;
        }

        // Keep existing instance; only repopulate when empty (initial) — ItemsSource changes
        // go through OnItemsSourceChanged → ReplaceViewerViewModelItems.
        if (_viewerVm.Items.Count == 0)
        {
            FillViewerViewModelItems();
        }
    }

    private void FillViewerViewModelItems()
    {
        if (ItemsSource != null)
        {
            foreach (var item in ItemsSource)
            {
                _viewerVm.Items.Add(item);
            }
        }
        else if (_card?.PreviewViewModel?.Items != null)
        {
            foreach (var item in _card.PreviewViewModel.Items)
            {
                _viewerVm.Items.Add(item);
            }
        }
    }

    private void ReplaceViewerViewModelItems()
    {
        _viewerVm ??= new PreviewItemsViewModel();
        _viewerVm.Items.Clear();
        FillViewerViewModelItems();
        _popupList?.BindToViewModel(_viewerVm);
        if (_helper?.Popup != null)
        {
            _helper.Popup.Items = _viewerVm.Items;
        }

        if (_viewer != null && !ReferenceEquals(_viewer.ViewModel, _viewerVm))
        {
            _viewer.ViewModel = _viewerVm;
        }
        else if (_viewer != null)
        {
            // Same VM instance: FlipView already bound to Items; no ItemsSource rebind on click path.
            _viewer.ViewModel = _viewerVm;
        }
    }

    private void DetachCard()
    {
        if (_card != null)
        {
            _card.ScreenshotClicked -= OnScreenshotClicked;
            _card.Helper = null;
            _card = null;
        }
    }

    private void OnScreenshotClicked(object sender, object e)
    {
        // Event order in ScreenshotsViewer.OnTileListClick:
        //   1) ScreenshotClicked (this handler) — EnsureHelper so Helper is non-null
        //   2) Helper.SetUpPreviewViewer(ClickedItem, flag: true)  [ASM sub_1821817F0]
        ScreenshotClicked?.Invoke(this, EventArgs.Empty);

        if (!IsViewerEnabled)
        {
            if (_card != null)
            {
                _card.Helper = null;
            }

            return;
        }

        EnsureHelper();
        if (_viewer != null)
        {
            _viewer.XamlRoot = XamlRoot;
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

        _listViewSource?.RestoreSourceVisibility();

        if (finalIndex >= 0)
        {
            var focusState = _viewer.WasKeyboardFocusActive ? FocusState.Keyboard : FocusState.Programmatic;
            _card.FocusItem(finalIndex, focusState);
        }

        if (_card.TileList != null)
        {
            if (_listViewSource != null && finalIndex >= 0)
            {
                _listViewSource.ScrollToIndex(finalIndex);
            }
            else if (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count)
            {
                _card.TileList.ScrollIntoView(_card.TileList.Items[finalIndex]);
            }

            _ = _card.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                if (transitionVersion != _viewerTransitionVersion || _viewer?.IsOpen == true)
                {
                    return;
                }

                object item = (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count)
                    ? _card.TileList.Items[finalIndex]
                    : null;

                if (_listViewSource != null && item != null)
                {
                    await _listViewSource.StartBackwardConnectedAnimationAsync(item);
                }
                else
                {
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
                }
            });
        }
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        if (root == null) return null;
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var desc = FindDescendant<T>(child);
            if (desc != null) return desc;
        }
        return null;
    }
}
