using System;
using System.Collections;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace DevWinUI;

/// <summary>
/// Fullscreen overlay viewer for screenshots and preview videos.
/// Ported faithfully from Microsoft Store PDP's <c>WinStore.UX.Controls.PDP.PreviewViewer</c>.
/// </summary>
public sealed partial class PreviewViewer : UserControl
{
    private const string ForwardAnimationKey = "screenshotForwardAnimation";
    private const string BackAnimationKey = "screenshotBackAnimation";

    private readonly Popup _popup;
    private Grid _viewerRoot;
    private Button _closeButton;
    private FlipView _flipView;
    private TextBlock _captionTextBlock;
    private TextBlock _counterTextBlock;
    private Run _currentIndexRun;
    private Run _totalIndexRun;
    private ScreenshotDataTemplateSelector _templateSelector;

    private int _pendingSelectedIndex;
    private int _selectionRequestVersion;
    private bool _pendingSelectionSet;
    private ConnectedAnimation _pendingAnimation;

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
        {
            newVm.PropertyChanged += c.OnViewModelPropertyChanged;
            // Store 0x18225A0A0 / OnViewModelChanged: ItemsSource bind + full PropertyChanged sync.
            if (c._flipView != null)
            {
                c.BindFlipViewItemsSource(newVm);
            }
            else
            {
                c.OnViewModelPropertyChanged(newVm, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }

    private void RefreshItems()
    {
        if (_flipView == null || ViewModel == null) return;
        if (_flipView.ItemsSource != ViewModel.Items)
        {
            BindFlipViewItemsSource(ViewModel);
        }
        else
        {
            OnViewModelPropertyChanged(ViewModel, new PropertyChangedEventArgs(string.Empty));
        }
    }

    // 0x18225A1B0..0x18225A59D [ASM] - exact PropertyChanged handler recovered from Store binary
    private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var vm = ViewModel;
        if (vm == null) return;

        string prop = e?.PropertyName;
        bool updateAll = string.IsNullOrEmpty(prop);

        // 1. "SelectedIndex" -> FlipView.SelectedIndex [ASM 0x18225A216]
        if (updateAll || string.Equals(prop, "SelectedIndex", StringComparison.Ordinal))
        {
            if (_flipView != null && vm.SelectedIndex >= 0 && vm.SelectedIndex < (_flipView.Items?.Count ?? 0))
            {
                if (_flipView.SelectedIndex != vm.SelectedIndex)
                {
                    _flipView.SelectionChanged -= OnSelectionChanged;
                    try
                    {
                        _flipView.SelectedIndex = vm.SelectedIndex;
                    }
                    finally
                    {
                        _flipView.SelectionChanged += OnSelectionChanged;
                    }
                }
            }
        }

        // 2. "Caption" -> CaptionTextBlock.Text [ASM 0x18225A2FC]
        if (updateAll || string.Equals(prop, "Caption", StringComparison.Ordinal))
        {
            if (_captionTextBlock != null)
            {
                var caption = vm.Caption;
                _captionTextBlock.Text = caption ?? string.Empty;
                _captionTextBlock.Visibility = string.IsNullOrEmpty(caption) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // 3. "CurrentIndex" -> Run text [ASM 0x18225A366]; getter 0x1823F0480 = SelectedIndex+1
        if (updateAll || string.Equals(prop, "CurrentIndex", StringComparison.Ordinal))
        {
            if (_currentIndexRun != null)
            {
                _currentIndexRun.Text = vm.CurrentIndex;
            }
        }

        // 4. "TotalIndex" -> Run text [ASM 0x18225A3E3]; WinUI 3 inline index [4] (see §D)
        if (updateAll || string.Equals(prop, "TotalIndex", StringComparison.Ordinal))
        {
            if (_totalIndexRun != null)
            {
                _totalIndexRun.Text = vm.TotalIndex;
            }
        }
    }

    private void ApplySelection()
    {
        if (_flipView == null || _flipView.Items == null || _flipView.Items.Count == 0)
            return;

        // Store PropertyChanged path uses ViewModel.SelectedIndex only (0x18225A216).
        var index = -1;
        if (ViewModel != null && ViewModel.SelectedIndex >= 0)
            index = Math.Min(ViewModel.SelectedIndex, _flipView.Items.Count - 1);
        if (index < 0 && ViewModel?.CurrentItem != null)
            index = _flipView.Items.IndexOf(ViewModel.CurrentItem);
        if (index < 0 && _pendingSelectionSet)
            index = Math.Min(_pendingSelectedIndex, _flipView.Items.Count - 1);
        if (index < 0)
            return;

        try
        {
            if (_flipView.SelectedIndex != index)
            {
                _flipView.SelectionChanged -= OnSelectionChanged;
                try
                {
                    _flipView.SelectedIndex = index;
                }
                finally
                {
                    _flipView.SelectionChanged += OnSelectionChanged;
                }
            }
            _pendingSelectedIndex = index;
            _pendingSelectionSet = false;
        }
        catch (ArgumentException)
        {
            _pendingSelectedIndex = index;
            _pendingSelectionSet = true;
        }
    }

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
        // Store wires x:Name fields in InitializeComponent (0x18225…); resolve parts here so
        // ViewModel.ItemsSource can bind before the first OpenOverlayPopup (FlipView already exists).
        AttachParts();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _popup = new Popup { IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        _popup.Child = this;
    }

    private void AttachParts()
    {
        _viewerRoot = FindName("ViewerGrid") as Grid;
        _closeButton = FindName("CloseButton") as Button;
        var flipView = FindName("ItemsFlipView") as FlipView;
        _captionTextBlock = FindName("CaptionTextBlock") as TextBlock;
        var indexGrid = FindName("IndexTextBlock") as Grid;
        _counterTextBlock = indexGrid != null ? FindDescendant<TextBlock>(indexGrid) : null;
        if (_counterTextBlock != null && _counterTextBlock.Inlines.Count >= 5)
        {
            _currentIndexRun = _counterTextBlock.Inlines[0] as Run;
            _totalIndexRun = _counterTextBlock.Inlines[4] as Run;
        }

        _templateSelector = FindResource("FlipViewDataTemplateSelector") as ScreenshotDataTemplateSelector;

        if (_flipView != null)
        {
            _flipView.SelectionChanged -= OnSelectionChanged;
        }

        _flipView = flipView;
        if (_flipView != null)
        {
            // Always re-subscribe. DetachParts removes SelectionChanged on Popup unload;
            // the FlipView instance is often reused, so ReferenceEquals-only wiring left
            // navigation without VM updates (caption/index frozen after same-item reopen).
            // Store keeps FlipView → SelectedIndex (0x1823F0170) → Caption/CurrentIndex PC live.
            _flipView.SelectionChanged += OnSelectionChanged;
        }

        if (_viewerRoot != null)
        {
            _viewerRoot.KeyDown -= OnViewerKeyDown;
            _viewerRoot.KeyDown += OnViewerKeyDown;
        }

        if (_closeButton != null)
        {
            _closeButton.Click -= OnCloseClicked;
            _closeButton.Click += OnCloseClicked;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AttachParts();
        ApplyAgeRestriction(AgeRestricted);

        // Bind ItemsSource once when FlipView is ready (Store OnViewModelChanged 0x18225A0A0 path).
        if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
        {
            BindFlipViewItemsSource(ViewModel);
        }

        OnViewModelPropertyChanged(ViewModel, new PropertyChangedEventArgs(string.Empty));

        ApplyOpenFocusAndAnimation();
        TryStartPendingAnimation();
        if (_flipView != null)
        {
            _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => PlayCurrentVideo());
        }
    }

    /// <summary>
    /// Matches reconstructed OnViewModelChanged: set ItemsSource then run full PropertyChanged sync
    /// so FlipView.SelectedIndex follows the list VM without a later ItemsSource rebind on click.
    /// </summary>
    private void BindFlipViewItemsSource(PreviewItemsViewModel vm)
    {
        if (_flipView == null || vm == null)
        {
            return;
        }

        // Detach SelectionChanged while assigning ItemsSource so the default index 0 does not
        // write back into the list VM (Store binds ItemsSource once before click SelectedIndex).
        _flipView.SelectionChanged -= OnSelectionChanged;
        try
        {
            _flipView.ItemsSource = vm.Items;
            OnViewModelPropertyChanged(vm, new PropertyChangedEventArgs(string.Empty));
        }
        finally
        {
            _flipView.SelectionChanged += OnSelectionChanged;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        DetachParts();
    }

    public bool WasKeyboardFocusActive { get; private set; }
    private bool _isKeyboardNavigationActive;

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

    /// <summary>
    /// Opens the overlay popup (matching MS Store's <c>OpenOverlayPopup 0x182172020</c>).
    /// </summary>
    private bool _openFocusPending;
    private bool _forwardAnimationPending;

    public void OpenOverlayPopup()
    {
        Show();
    }

    public void Show()
    {
        _isKeyboardNavigationActive = false;
        WasKeyboardFocusActive = false;
        _openFocusPending = true;
        _forwardAnimationPending = true;

        if (XamlRoot != null)
        {
            _popup.XamlRoot = XamlRoot;
            Width = XamlRoot.Size.Width;
            Height = XamlRoot.Size.Height;
            XamlRoot.Changed -= OnXamlRootChanged;
            XamlRoot.Changed += OnXamlRootChanged;
        }

        // Store OpenOverlayPopup does not rebind ItemsSource; only show + Focus FlipView.
        // SelectedIndex was already written by OpenPreviewViewer via 0x1823F0170.
        if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
        {
            BindFlipViewItemsSource(ViewModel);
        }
        else
        {
            OnViewModelPropertyChanged(ViewModel, new PropertyChangedEventArgs(string.Empty));
        }

        _popup.IsOpen = true;

        // sub_180D8FDF0 from OpenOverlayPopup 0x182172020:
        // Focus(ItemsFlipView, FocusState.Programmatic) — clears strip keyboard focus visual.
        ApplyOpenFocusAndAnimation();
    }

    private void ApplyOpenFocusAndAnimation()
    {
        if (_flipView == null)
        {
            return;
        }

        ApplySelection();
        OnViewModelPropertyChanged(ViewModel, new PropertyChangedEventArgs(string.Empty));

        if (_openFocusPending)
        {
            _openFocusPending = false;
            FocusFlipViewProgrammatic();
        }

        if (_forwardAnimationPending)
        {
            _forwardAnimationPending = false;
            TryStartForwardConnectedAnimation();
        }
    }

    /// <summary>
    /// Closes the overlay popup (matching MS Store's <c>Close 0x182172170</c>).
    /// </summary>
    public void Close()
    {
        Hide();
    }

    public void Hide()
    {
        if (!_popup.IsOpen) return;
        _pendingAnimation = null;

        var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        WasKeyboardFocusActive = _isKeyboardNavigationActive
            || (focused is Control ctrl && ctrl.FocusState == FocusState.Keyboard);

        PrepareBackAnimation();

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

    /// <summary>
    /// Prepares backward connected animation from FlipView container.
    /// Recovered from <c>0x1821722D0</c>.
    /// </summary>
    public void PrepareBackAnimation()
    {
        if (_flipView == null || _flipView.SelectedIndex < 0 || _flipView.Items == null || _flipView.SelectedIndex >= _flipView.Items.Count)
            return;

        var container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement;
        if (container == null) return;

        UIElement target = FindDescendant<Image>(container)
                        ?? FindDescendant<TileImage>(container)
                        ?? (UIElement)container;

        try
        {
            var backAnim = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(BackAnimationKey, target);
            if (backAnim != null)
            {
                backAnim.Configuration = new DirectConnectedAnimationConfiguration(); // 0x18042de20 [ASM]
            }
        }
        catch { }
    }

    /// <summary>
    /// Starts forward connected animation, preferring MediaPlayerElement if video duration > 0.
    /// Recovered from <c>0x18220B920</c> (which awaits 15ms <c>sub_180A5CA00</c> for FlipView layout realization).
    /// </summary>
    public async void TryStartForwardConnectedAnimation()
    {
        if (_flipView == null)
        {
            _forwardAnimationPending = true;
            return;
        }

        var animation = ConnectedAnimationService.GetForCurrentView().GetAnimation(ForwardAnimationKey);
        if (animation == null)
        {
            FocusFlipViewProgrammatic();
            return;
        }

        animation.Configuration = new DirectConnectedAnimationConfiguration();

        // Faithful reconstruction of 0x18220B920 awaiting sub_180A5CA00 (15ms layout pass await)
        FrameworkElement container = null;
        for (int attempt = 0; attempt < 6; attempt++)
        {
            await System.Threading.Tasks.Task.Delay(15);
            int idx = _flipView != null && _flipView.SelectedIndex >= 0 ? _flipView.SelectedIndex : SelectedIndex;
            container = SafeContainerFromIndex(idx) as FrameworkElement;
            if (container != null && container.ActualWidth > 0 && container.ActualHeight > 0)
            {
                break;
            }
        }

        if (container != null)
        {
            UIElement target = container;
            var player = FindDescendant<MediaPlayerElement>(container);
            var duration = player != null && player.MediaPlayer != null
                ? player.MediaPlayer.NaturalDuration.TotalSeconds
                : 0;

            if (player != null && duration > 0)
            {
                target = player; // [ASM 0x18220BD93] ucomisd ja -> TryStart(player)
            }
            else
            {
                target = FindDescendant<Image>(container) ?? FindDescendant<TileImage>(container) ?? container;
            }

            try
            {
                animation.TryStart(target);
            }
            catch { }
        }
        else
        {
            // Do not Cancel on first-open races; leave animation for a later Loaded retry.
            _forwardAnimationPending = true;
        }

        // [ASM 0x18220BAFB] Focus(FocusState.Programmatic) on FlipView
        FocusFlipViewProgrammatic();
        if (_flipView != null)
        {
            _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => PlayCurrentVideo());
        }
    }

    private void FocusFlipViewProgrammatic()
    {
        _flipView?.Focus(FocusState.Programmatic); // [ASM] edx=3
    }

    private DependencyObject SafeContainerFromIndex(int index)
    {
        if (_flipView == null || index < 0 || _flipView.Items == null || index >= _flipView.Items.Count) return null;
        try { return _flipView.ContainerFromIndex(index); } catch { return null; }
    }

    private void TryStartPendingAnimation()
    {
        if (_pendingAnimation == null) return;
        var animation = _pendingAnimation;
        _pendingAnimation = null;
        void TryStart()
        {
            UIElement target = null;
            if (SafeContainerFromIndex(SelectedIndex) is FrameworkElement container)
                target = FindDescendant<Image>(container) ?? FindDescendant<TileImage>(container) ?? container;
            target ??= _flipView;
            try
            {
                animation.Configuration = new DirectConnectedAnimationConfiguration();
                _ = animation.TryStart(target);
            }
            catch { }
        }
        if (SafeContainerFromIndex(SelectedIndex) != null) TryStart();
        else if (_flipView != null) _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, TryStart);
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
    private void OnViewerKeyDown(object sender, KeyRoutedEventArgs e)
    {
        _isKeyboardNavigationActive = true;
        if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            Hide();
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_flipView != null && ViewModel != null)
        {
            int idx = _flipView.SelectedIndex;
            if (idx >= 0 && idx != ViewModel.SelectedIndex)
            {
                ViewModel.SelectedIndex = idx;
            }
        }

        StopOtherVideos();
        PlayCurrentVideo();
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
