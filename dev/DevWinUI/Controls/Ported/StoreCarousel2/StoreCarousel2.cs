namespace DevWinUI;

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

    public StoreCarousel2()
    {
        DefaultStyleKey = typeof(StoreCarousel2);
    }

    public event EventHandler ScreenshotClicked;

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

        _viewer.SetThemeSource(this);

        _listViewSource ??= new PreviewViewerHelperListViewSource(_card.TileList);
        _listViewSource.InnerListView = _card.TileList;

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
            _helper.Popup ??= new PreviewPopupHost();
            _helper.Popup.List = _popupList;
            _helper.Popup.Items = _viewerVm.Items;
        }

        _popupList.Items = null;
        _popupList.BindToViewModel(_viewerVm);

        if (!ReferenceEquals(_viewer.ViewModel, _viewerVm))
        {
            _viewer.ViewModel = _viewerVm;
        }

        _viewer.AgeRestricted = AgeRestricted;
        _card.Helper = IsViewerEnabled ? _helper : null;
    }

    private void EnsureViewerViewModel()
    {
        if (_viewerVm == null)
        {
            _viewerVm = new PreviewItemsViewModel();
            FillViewerViewModelItems();
            return;
        }

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
        else
        {
            _viewer?.ViewModel = _viewerVm;
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

        ScreenshotClicked?.Invoke(this, EventArgs.Empty);

        if (!IsViewerEnabled)
        {
            _card?.Helper = null;

            return;
        }

        EnsureHelper();
        _viewer?.XamlRoot = XamlRoot;
    }

    public void OpenPreview(int index = 0)
    {
        if (!IsViewerEnabled || _card == null)
        {
            return;
        }

        EnsureHelper();

        _viewer?.XamlRoot = XamlRoot;

        if (_helper == null || _viewerVm == null || _viewerVm.Items.Count == 0)
        {
            return;
        }

        if (index < 0 || index >= _viewerVm.Items.Count)
        {
            index = 0;
        }

        var dataItem = _viewerVm.Items[index];
        _helper.SetUpPreviewViewer(dataItem);
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
            _card.FocusItem(finalIndex, FocusState.Programmatic);
        }

        if (_card.TileList != null)
        {
            _ = _card.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                if (transitionVersion != _viewerTransitionVersion || _viewer?.IsOpen == true)
                {
                    return;
                }

                object item = (finalIndex >= 0 && finalIndex < _card.TileList.Items.Count) ? _card.TileList.Items[finalIndex] : null;

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

                if (finalIndex >= 0)
                {
                    _card.FocusItem(finalIndex, FocusState.Programmatic);
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
