using System.Collections;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace DevWinUI;

public sealed partial class PreviewViewer : UserControl
{
    private readonly Popup _popup;
    private Grid _viewerRoot;
    private Button _closeButton;
    private FlipView _flipView;
    private TextBlock _captionTextBlock;
    private TextBlock _counterTextBlock;
    private ScreenshotDataTemplateSelector _templateSelector;

    private int _pendingSelectedIndex;
    private int _selectionRequestVersion;
    private bool _pendingSelectionSet;
    private ConnectedAnimation _pendingAnimation;

    private bool _isLoadedHandled;

    public static readonly DependencyProperty AgeRestrictedProperty =
        DependencyProperty.Register(nameof(AgeRestricted), typeof(bool), typeof(PreviewViewer), new PropertyMetadata(false, OnAgeRestrictedChanged));

    public bool AgeRestricted
    {
        get => (bool)GetValue(AgeRestrictedProperty);
        set => SetValue(AgeRestrictedProperty, value);
    }

    private static void OnAgeRestrictedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (PreviewViewer)d;
        viewer.ApplyAgeRestriction((bool)e.NewValue);
    }

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(PreviewItemsViewModel), typeof(PreviewViewer), new PropertyMetadata(null, OnViewModelChanged));

    public PreviewItemsViewModel ViewModel
    {
        get => (PreviewItemsViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (PreviewViewer)d;
        if (e.OldValue is PreviewItemsViewModel oldVm)
            oldVm.PropertyChanged -= c.OnViewModelPropertyChanged;
        if (e.NewValue is PreviewItemsViewModel newVm)
            newVm.PropertyChanged += c.OnViewModelPropertyChanged;
        c.RefreshItems();
    }

    private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => RefreshItems();

    private void RefreshItems()
    {
        if (_flipView == null || ViewModel == null) return;
        _flipView.ItemsSource = ViewModel.Items;
        ApplySelection();
        UpdateCaptionAndCounter();
    }

    // The Store supplies the current item before opening the popup. Apply it while
    // the FlipView is still hidden so the popup never opens at item zero and then
    // visibly scrolls to the requested item.
    private void ApplySelection()
    {
        if (_flipView == null || _flipView.Items == null || _flipView.Items.Count == 0)
            return;

        var index = -1;
        if (ViewModel?.CurrentItem != null)
            index = _flipView.Items.IndexOf(ViewModel.CurrentItem);
        if (index < 0 && _pendingSelectionSet)
            index = Math.Min(_pendingSelectedIndex, _flipView.Items.Count - 1);
        if (index < 0)
            return;

        try
        {
            _flipView.SelectedIndex = index;
            _pendingSelectedIndex = index;
            _pendingSelectionSet = false;
        }
        catch (ArgumentException)
        {
            // WinUI can reject selection while a source is being replaced. The
            // source/property callback will call ApplySelection again.
            _pendingSelectedIndex = index;
            _pendingSelectionSet = true;
        }
    }

    private void ScheduleCurrentItemSelection() => ApplySelection();

    public IEnumerable ItemsSource
    {
        get => ViewModel?.Items;
        set
        {
            var vm = new PreviewItemsViewModel();
            if (value != null)
                foreach (var item in value) vm.Items.Add(item);
            ViewModel = vm;
        }
    }

    public int SelectedIndex
    {
        get => _pendingSelectionSet
            ? _pendingSelectedIndex
            : (_flipView != null && _flipView.SelectedIndex >= 0 ? _flipView.SelectedIndex : _pendingSelectedIndex);
        set
        {
            _pendingSelectedIndex = Math.Max(0, value);
            _pendingSelectionSet = true;
            _selectionRequestVersion++;
            SchedulePendingSelection();
        }
    }

    private void SchedulePendingSelection()
    {
        if (_flipView == null || !_pendingSelectionSet)
            return;

        ApplyPendingSelection(_selectionRequestVersion);
    }

    private void ApplyPendingSelection(int requestVersion)
    {
        if (_flipView == null || !_pendingSelectionSet || requestVersion != _selectionRequestVersion)
            return;

        if (_flipView.Items == null || _flipView.Items.Count == 0)
            return;

        var index = Math.Min(_pendingSelectedIndex, _flipView.Items.Count - 1);
        try
        {
            _flipView.SelectedIndex = index;
            _pendingSelectedIndex = index;
            _pendingSelectionSet = false;
        }
        catch (ArgumentException)
        {
            SchedulePendingSelection();
        }
    }

    public PreviewViewer()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _popup = new Popup { IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        _popup.Child = this;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewerRoot = FindName("ViewerGrid") as Grid;
        _closeButton = FindName("CloseButton") as Button;
        _flipView = FindName("ItemsFlipView") as FlipView;
        var suppressInitialSelection = ViewModel?.CurrentItem != null;
        if (suppressInitialSelection && _viewerRoot != null)
            _viewerRoot.Opacity = 0;
        _captionTextBlock = FindName("CaptionTextBlock") as TextBlock;
        var indexGrid = FindName("IndexTextBlock") as Grid;
        _counterTextBlock = indexGrid != null ? FindDescendant<TextBlock>(indexGrid) : null;
        _templateSelector = FindResource("FlipViewDataTemplateSelector") as ScreenshotDataTemplateSelector;
        ApplyAgeRestriction(AgeRestricted);
        if (_viewerRoot != null) _viewerRoot.KeyDown += OnViewerKeyDown;
        if (_closeButton != null) _closeButton.Click += OnCloseClicked;
        if (_flipView != null)
        {
            _flipView.SelectionChanged += OnSelectionChanged;
            if (ViewModel != null) _flipView.ItemsSource = ViewModel.Items;
            else if (ItemsSource != null) _flipView.ItemsSource = ViewModel?.Items;
            ApplySelection();
        }
        UpdateCaptionAndCounter();
        if (suppressInitialSelection && _viewerRoot != null)
            _viewerRoot.Opacity = 1;
        TryStartPendingAnimation();
        if (_flipView != null) _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => PlayCurrentVideo());
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachParts();
    }

    public event EventHandler Closed;
    public bool IsOpen => _popup != null && _popup.IsOpen;
    public FlipView PreviewFlipView => _flipView;

    private void DetachParts()
    {
        if (_viewerRoot != null) _viewerRoot.KeyDown -= OnViewerKeyDown;
        if (_closeButton != null) _closeButton.Click -= OnCloseClicked;
        if (_flipView != null) _flipView.SelectionChanged -= OnSelectionChanged;
    }

    public void StartConnectedAnimation(ConnectedAnimation animation)
    {
        _pendingAnimation = animation;
        TryStartPendingAnimation();
    }

    public void Show()
    {
        if (XamlRoot != null)
        {
            _popup.XamlRoot = XamlRoot;
            Width = XamlRoot.Size.Width;
            Height = XamlRoot.Size.Height;
            XamlRoot.Changed -= OnXamlRootChanged;
            XamlRoot.Changed += OnXamlRootChanged;
        }
        if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
            _flipView.ItemsSource = ViewModel.Items;
        ApplySelection();
        UpdateCaptionAndCounter();
        _popup.IsOpen = true;
        if (_closeButton != null) _closeButton.Focus(FocusState.Programmatic);
        if (_flipView != null) _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => PlayCurrentVideo());
    }

    public void Hide()
    {
        if (!_popup.IsOpen) return;
        _pendingAnimation = null;
        UIElement currentFlipElement = null;
        try
        {
            if (_flipView != null && _flipView.SelectedIndex >= 0 && _flipView.Items != null && _flipView.SelectedIndex < _flipView.Items.Count)
            {
                var container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement;
                if (container != null)
                    currentFlipElement = FindDescendant<Image>(container) ?? FindDescendant<TileImage>(container) ?? container;
                currentFlipElement ??= _flipView;
            }
            else
            {
                currentFlipElement = _flipView;
            }
        }
        catch { currentFlipElement = _flipView; }
        if (currentFlipElement != null)
        {
            try
            {
                var backAnim = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("screenshotBackAnimation", currentFlipElement);
                if (backAnim != null) backAnim.Configuration = new DirectConnectedAnimationConfiguration();
            }
            catch { }
        }
        if (_flipView != null && _flipView.Items != null)
        {
            for (var i = 0; i < _flipView.Items.Count; i++)
            {
                if (SafeContainerFromIndex(i) is FrameworkElement container)
                {
                    var player = FindDescendant<MediaPlayerElement>(container);
                    if (player?.MediaPlayer != null) try { player.MediaPlayer.Pause(); } catch { }
                }
            }
        }
        _popup.IsOpen = false;
        if (XamlRoot != null) XamlRoot.Changed -= OnXamlRootChanged;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private DependencyObject SafeContainerFromIndex(int index)
    {
        if (_flipView == null || index < 0 || _flipView.Items == null || index >= _flipView.Items.Count) return null;
        try { return _flipView.ContainerFromIndex(index); } catch { return null; }
    }

    private void TryStartPendingAnimation()
    {
        if (_flipView == null || _pendingAnimation == null) return;
        var animation = _pendingAnimation;
        void TryStart()
        {
            if (_pendingAnimation != null && _pendingAnimation != animation) return;
            _pendingAnimation = null;
            UIElement target = null;
            if (SafeContainerFromIndex(SelectedIndex) is FrameworkElement container)
                target = FindDescendant<Image>(container) ?? FindDescendant<TileImage>(container) ?? container;
            target ??= _flipView;
            try { animation.Configuration = new DirectConnectedAnimationConfiguration(); _ = animation.TryStart(target); } catch { }
        }
        if (SafeContainerFromIndex(SelectedIndex) != null) TryStart();
        else _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, TryStart);
    }

    private void ApplyAgeRestriction(bool ageRestricted)
    {
        if (_templateSelector == null)
            return;

        _templateSelector.AgeRestricted = ageRestricted;
        if (_flipView != null && ViewModel != null)
            ApplySelection();
    }

    private object FindResource(object key)
    {
        if (Resources.ContainsKey(key))
            return Resources[key];
        return null;
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { Width = sender.Size.Width; Height = sender.Size.Height; }
    private void OnCloseClicked(object sender, RoutedEventArgs e) => Hide();
    private void OnViewerKeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == VirtualKey.Escape) { e.Handled = true; Hide(); } }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) { UpdateCaptionAndCounter(); StopOtherVideos(); PlayCurrentVideo(); }

    private void UpdateCaptionAndCounter()
    {
        if (_flipView == null) return;
        string title = null;
        var item = _flipView.SelectedItem;
        if (item is ScreenshotTileItem s) title = s.Title;
        else if (item is VideoPlayerSource v) title = v.Title;
        else if (item is IImageItem i) title = (i as ScreenshotTileItem)?.Title;
        if (_captionTextBlock != null) { _captionTextBlock.Text = title ?? string.Empty; _captionTextBlock.Visibility = string.IsNullOrEmpty(title) ? Visibility.Collapsed : Visibility.Visible; }
        if (_counterTextBlock != null && _flipView.Items != null)
        {
            if (_counterTextBlock.Inlines.Count >= 3)
            {
                if (_counterTextBlock.Inlines[0] is Run indexRun) indexRun.Text = (_flipView.SelectedIndex + 1).ToString();
                if (_counterTextBlock.Inlines[2] is Run totalRun) totalRun.Text = _flipView.Items.Count.ToString();
            }
        }
    }

    private void PlayCurrentVideo()
    {
        if (_flipView == null || _flipView.SelectedIndex < 0 || _flipView.Items == null || _flipView.SelectedIndex >= _flipView.Items.Count) return;
        var container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement;
        if (container != null) { var player = FindDescendant<MediaPlayerElement>(container); try { player?.MediaPlayer?.Play(); } catch { } }
    }

    private void StopOtherVideos()
    {
        if (_flipView == null || _flipView.Items == null) return;
        for (var i = 0; i < _flipView.Items.Count; i++)
        {
            if (i == _flipView.SelectedIndex) continue;
            if (SafeContainerFromIndex(i) is FrameworkElement container)
            {
                var player = FindDescendant<MediaPlayerElement>(container);
                try { player?.MediaPlayer?.Pause(); } catch { }
            }
        }
    }

    private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T m) return m;
            var d = FindDescendant<T>(child);
            if (d != null) return d;
        }
        return null;
    }
}
