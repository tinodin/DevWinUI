using System;
using System.Collections;
using System.Collections.Generic;
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
/// Store hosting contract (byte-verified): the viewer is created once per ScreenshotsViewer
/// data-wire (<c>sub_182181650</c>, single <c>RoActivateInstance</c> site), XAML-loaded once
/// (<c>sub_182172890</c>, guarded init), and reused across opens; open/close only
/// register/unregister it with the overlay manager — the instance is never unloaded.
/// The overlay is hosted by a standalone <see cref="Popup"/> (so it covers the whole window,
/// not the control's bounds) whose content is this viewer — set on open and cleared on close,
/// matching Store's host (<c>[manager+0x158]</c>, property slot 6/7 with
/// <c>sub_1801CA150</c>) whose visibility is property slot 22 (arg 0 on open / 1 on remove).
/// WinUI 3 adaptation: the theme is mirrored from the owning control via
/// <see cref="SetThemeSource"/>, because DevWinUI assigns <c>RequestedTheme</c> to
/// <c>window.Content</c> only and a standalone popup inherits nothing. Status .md §N.
/// </summary>
public sealed partial class PreviewViewer : OverlayPopupBase
{
    private const string ForwardAnimationKey = "screenshotForwardAnimation";
    private const string BackAnimationKey = "screenshotBackAnimation";

    private Popup _popup;
    private FrameworkElement _themeSource;

    /// <summary>
    /// Store <c>sub_1801CA150(host, viewer)</c>: sets the overlay host's content to this viewer.
    /// While detached nothing is rendered, and on the next open the tree is built for the first
    /// time with the FlipView selection already applied — Store's reason there is no scroll
    /// animation into the clicked item.
    /// </summary>
    internal void AttachToOverlayHost()
    {
        if (_popup != null && _popup.Child == null)
        {
            _popup.Child = this;
        }
    }

    /// <summary>
    /// Store <c>sub_1801CA150(host, 0)</c>: clears the overlay host's content, detaching the
    /// tree. Projected to <see cref="Popup.Child"/>.
    /// </summary>
    internal void DetachFromOverlayHost()
    {
        if (_popup != null && _popup.Child != null)
        {
            _popup.Child = null;
        }
    }

    /// <summary>
    /// Mirrors the owning control's <see cref="UIElement.ActualTheme"/> onto this overlay.
    /// WinUI 3 adaptation, not Store behaviour: Store's overlay host is an element inside the
    /// app's tree, so it inherits the theme for free. DevWinUI, however, applies the theme by
    /// assigning <c>RequestedTheme</c> to <c>window.Content</c> and never sets
    /// <c>Application.Current.RequestedTheme</c> (<c>ThemeService.ElementTheme.cs:79</c>), so
    /// theme travels only by inheritance — and a standalone popup hosting this overlay has no
    /// ancestor to inherit from. Without this the overlay's <c>{ThemeResource}</c> values resolve
    /// against the application default and stay stale. Status .md §N.
    /// </summary>
    internal void SetThemeSource(FrameworkElement source)
    {
        if (source == null)
        {
            return;
        }

        if (!ReferenceEquals(_themeSource, source))
        {
            if (_themeSource != null)
            {
                _themeSource.ActualThemeChanged -= OnThemeSourceActualThemeChanged;
            }

            _themeSource = source;
            _themeSource.ActualThemeChanged += OnThemeSourceActualThemeChanged;
        }

        ApplyThemeFromSource();
    }

    private void OnThemeSourceActualThemeChanged(FrameworkElement sender, object args) => ApplyThemeFromSource();

    private void ApplyThemeFromSource()
    {
        if (_themeSource != null)
        {
            RequestedTheme = _themeSource.ActualTheme;
        }
    }

    private void UpdateOverlaySize()
    {
        if (XamlRoot == null)
        {
            return;
        }

        Width = XamlRoot.Size.Width;
        Height = XamlRoot.Size.Height;
    }

    public PreviewViewer()
    {
        InitializeComponent();
        // Store wires x:Name fields once in InitializeComponent (0x18225…, the compiled-XAML
        // Connect step 0x182172B70) and never tears those handlers down again — there is no
        // remove/add-again site anywhere in the binary for the two handlers it wires. So the
        // port wires its parts exactly once, here, and never unsubscribes on Unloaded.
        AttachParts();
        Loaded += OnLoaded;
        // Overlay host: a standalone popup, as before, so the overlay covers the whole window
        // instead of being anchored to the control's position. Its content is this viewer, set on
        // open and cleared on close (Store host.Content) — not pre-attached, so even the first
        // open builds the tree with the selection already applied. Theme is mirrored explicitly
        // via SetThemeSource, since a standalone popup inherits nothing.
        _popup = new Popup { IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
    }

    private bool _isOpen;
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
        ApplyViewModelState(e?.PropertyName);
    }

    /// <summary>
    /// Applies the view-model state to the named parts; empty <paramref name="prop"/> means all.
    /// </summary>
    private void ApplyViewModelState(string prop)
    {
        var vm = ViewModel;
        if (vm == null) return;

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

        SyncCaptionAndIndexText(updateAll, prop);
    }

    /// <summary>
    /// Caption / CurrentIndex / TotalIndex only — the presentation half of the Store handler,
    /// with no FlipView write. Used on re-attach (<see cref="OnLoaded"/>), where the selection is
    /// already correct: writing it there would land while the overlay is visible and animate a
    /// scroll from the previously selected item.
    /// </summary>
    private void SyncCaptionAndIndexText(bool updateAll, string prop)
    {
        var vm = ViewModel;
        if (vm == null) return;

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
            // Always re-subscribe. Store keeps this wiring for the lifetime of the tree
            // (compiled-XAML Connect, never removed); re-subscribing here keeps
            // AttachParts() idempotent for the first-load refresh in OnLoaded.
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
        // Fires on every open: the host content is cleared on close (Store host.Content = null,
        // sub_1801CA150(host, 0) in 0x180D91CA0), so this is the re-attach hook. Part wiring was
        // already done in the constructor and is never torn down, matching Store's one-time
        // compiled-XAML Connect step.
        AttachParts();
        ApplyAgeRestriction(AgeRestricted);

        // Bind ItemsSource when the FlipView is ready (Store OnViewModelChanged 0x18225A0A0 path).
        if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
        {
            BindFlipViewItemsSource(ViewModel);
        }

        // Presentation only. The FlipView selection was written before the content was attached,
        // so writing it again here would land while the overlay is visible and animate a scroll
        // from the previously selected item.
        SyncCaptionAndIndexText(true, null);
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

    // No Unloaded teardown, deliberately: Store's compiled-XAML wiring (sub_182172B70) is
    // created once and never removed — there is no remove/add-again site in the binary for
    // the handlers it wires. Unsubscribing on unload left the viewer visible but inert
    // (dead close button, FlipView selection no longer syncing) whenever Loaded did not
    // re-fire. See status .md §N.

    public bool WasKeyboardFocusActive { get; private set; }
    private bool _isKeyboardNavigationActive;

    public event EventHandler Closed;

    /// <summary>
    /// Whether this viewer is the currently shown overlay (Store manager-registration state).
    /// Backed by the open/remove transitions, not by <see cref="Popup.IsOpen"/>: the host
    /// popup is opened once and stays open, and its content is never detached (Store
    /// toggles the host's Visibility only; status .md §N).
    /// </summary>
    public bool IsOpen => _isOpen;

    /// <summary>
    /// The overlay host manager (Store app-global overlay service, per-instance projection).
    /// Each viewer owns a private manager instance, mirroring the single-viewer Store lifetime.
    /// </summary>
    public PreviewOverlayManager OverlayManager { get; set; } = new PreviewOverlayManager();
    public FlipView PreviewFlipView => _flipView;

    public void StartConnectedAnimation(ConnectedAnimation animation)
    {
        _pendingAnimation = animation;
        TryStartPendingAnimation();
    }

    /// <summary>
    /// Opens the overlay popup (matching MS Store's <c>OpenOverlayPopup 0x182172020</c>).
    /// Verbatim order: FlipView selection write (done by <c>OpenPreviewViewer</c> via
    /// <c>0x1823F0170</c> while the viewer is still collapsed) → overlay-manager show
    /// (close-current, host show via <c>sub_180D8FDF0</c>) →
    /// <c>Focus(ItemsFlipView, Programmatic)</c> (end of <c>sub_180D8FDF0</c>,
    /// <c>mov edx, 3</c> at <c>0x180D90006</c>) → forward connected animation.
    /// </summary>
    private bool _forwardAnimationPending;

    public void OpenOverlayPopup()
    {
        Show();
    }

    public void Show()
    {
        _isKeyboardNavigationActive = false;
        WasKeyboardFocusActive = false;
        _forwardAnimationPending = true;

        if (XamlRoot != null)
        {
            _popup.XamlRoot = XamlRoot;
            UpdateOverlaySize();
            XamlRoot.Changed -= OnXamlRootChanged;
            XamlRoot.Changed += OnXamlRootChanged;
        }

        if (!_popup.IsOpen)
        {
            _popup.IsOpen = true;
        }

        // Store OpenOverlayPopup does not rebind ItemsSource; only show + Focus FlipView.
        // SelectedIndex was already written by OpenPreviewViewer via 0x1823F0170.
        if (_flipView != null && ViewModel != null && _flipView.ItemsSource != ViewModel.Items)
        {
            BindFlipViewItemsSource(ViewModel);
        }
        else
        {
            ApplyViewModelState(null);
        }

        // Store order (0x18209E820): the selection is written BEFORE OpenOverlayPopup, while the
        // content is still detached. The tree is then built for the first time with the target
        // index already set, so the FlipView does not animate a scroll from the previously
        // selected item.
        ApplySelection();

        // sub_180D8FDF0: close current, host.Content = viewer, host.Visibility = Visible.
        OverlayManager.ShowOverlay(this);
        _isOpen = true;

        // sub_180D8FDF0 from OpenOverlayPopup 0x182172020:
        // Focus(ItemsFlipView, FocusState.Programmatic) — clears strip keyboard focus visual.
        FocusFlipViewProgrammatic();

        TryStartForwardConnectedAnimation();
        TryStartPendingAnimation();
        if (_flipView != null)
        {
            _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => EnsureCurrentVideoStarted());
        }
    }

    /// <summary>
    /// Closes the overlay popup (matching MS Store's <c>Close 0x182172170</c>).
    /// Verbatim order: <c>RemoveOverlayPopup (0x180D91CA0)</c> (early-out when not
    /// removed) → <c>PrepareBackAnimation (0x1821722D0)</c> → closed callbacks.
    /// The keyboard-focus probe stays before removal: it must read the focused element
    /// while the tree is intact (WinUI 3 adaptation; Store has no equivalent probe).
    /// Per-video media teardown mirrors Store's <c>sub_1823F0D20</c>, which walks the
    /// view-model items backwards on close and tears down every <c>VideoPlayerSource</c>
    /// (<c>sub_1823F6200</c>); <see cref="PreviewViewerVideoItem.ResetSource"/> is its
    /// WinUI 3 counterpart, and <see cref="PlayCurrentVideo"/> re-applies the source on the
    /// next open the way <c>sub_182172450</c> re-applies it for the current video item.
    /// </summary>
    public void Close()
    {
        Hide();
    }

    public void Hide()
    {
        if (!IsOpen) return;
        _pendingAnimation = null;

        var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
        WasKeyboardFocusActive = _isKeyboardNavigationActive
            || (focused is Control ctrl && ctrl.FocusState == FocusState.Keyboard);

        // Store's close order is Remove → Prepare → per-item teardown, and both of those read a
        // container list cached on the FlipView object, which survives the content being nulled.
        // WinUI 3 has no such cache: ContainerFromIndex only answers while the tree is attached.
        // Snapshot what the two steps below need while it is still attached, so Store's order is
        // preserved unchanged.
        var currentContainer = _flipView != null && _flipView.SelectedIndex >= 0
            ? SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement
            : null;
        var containers = SnapshotRealizedContainers();

        if (!RemoveOverlayPopup())
        {
            return;
        }

        PrepareBackAnimation(currentContainer);

        // Store sub_1823F0D20 runs here: walk the view-model items backwards and tear down
        // every VideoPlayerSource (sub_1823F6200). Port: the same walk over the containers
        // captured above, clearing each video item's media element — so the next open starts the
        // clip from the beginning (Store re-assigns the source in sub_182172450) and no audio
        // survives the close (WinUI 3 keeps playing when an element merely goes invisible).
        TeardownAllVideos(containers);

        Closed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The realized FlipView containers, captured while the tree is still attached.
    /// </summary>
    private List<FrameworkElement> SnapshotRealizedContainers()
    {
        var result = new List<FrameworkElement>();
        if (_flipView?.Items == null)
        {
            return result;
        }

        for (var i = 0; i < _flipView.Items.Count; i++)
        {
            if (SafeContainerFromIndex(i) is FrameworkElement container)
            {
                result.Add(container);
            }
        }

        return result;
    }

    /// <summary>
    /// Per-video teardown for <see cref="Hide"/> — the port of Store <c>sub_1823F0D20</c>.
    /// </summary>
    private void TeardownAllVideos(List<FrameworkElement> containers)
    {
        foreach (var container in containers)
        {
            if (FindDescendant<PreviewViewerVideoItem>(container) is { } videoItem)
            {
                videoItem.ResetSource();
                continue;
            }

            var player = FindDescendant<MediaPlayerElement>(container);
            if (player?.MediaPlayer != null)
            {
                try { player.MediaPlayer.Pause(); } catch { }
            }
        }
    }

    /// <summary>
    /// Unregisters the viewer from the overlay manager (Store <c>RemoveOverlayPopup</c>).
    /// Returns the verified removed bool (<c>false</c> when there is no manager or the
    /// manager refuses removal). Telemetry (<c>0x180E71EE0</c>) and the
    /// <c>[+0x180]</c> closed delegate have no verifiable port projection and are omitted.
    /// </summary>
    private bool RemoveOverlayPopup()
    {
        var manager = OverlayManager;
        if (manager == null)
        {
            return false;
        }

        if (!manager.HideOverlay(this))
        {
            return false;
        }

        _isOpen = false;
        if (XamlRoot != null) XamlRoot.Changed -= OnXamlRootChanged;
        return true;
    }

    /// <summary>
    /// Prepares backward connected animation from FlipView container.
    /// Recovered from <c>0x1821722D0</c>. Takes the container captured before the content was
    /// detached, since <c>ContainerFromIndex</c> stops answering once the tree is unparented.
    /// </summary>
    private void PrepareBackAnimation(FrameworkElement container)
    {
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
            _ = _flipView.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => EnsureCurrentVideoStarted());
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

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateOverlaySize();
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
        EnsureCurrentVideoStarted();
    }

    /// <summary>
    /// Ensures the current video item has its source and is playing, waiting for the FlipView
    /// container to be realized. Open and selection change both land before the new container
    /// exists, and after a close-time teardown the source has to be re-applied, so this polls
    /// the same 15 ms layout cadence the forward connected animation uses (0x18220B920 →
    /// <c>sub_180A5CA00</c>).
    /// </summary>
    private async void EnsureCurrentVideoStarted()
    {
        if (_flipView == null)
        {
            return;
        }

        for (var attempt = 0; attempt < 6; attempt++)
        {
            if (attempt > 0)
            {
                await System.Threading.Tasks.Task.Delay(15);
            }

            if (_flipView == null)
            {
                return;
            }

            if (SafeContainerFromIndex(_flipView.SelectedIndex) is FrameworkElement container
                && FindDescendant<MediaPlayerElement>(container) != null)
            {
                PlayCurrentVideo();
                return;
            }
        }

        PlayCurrentVideo();
    }

    private void PlayCurrentVideo()
    {
        if (_flipView == null || _flipView.SelectedIndex < 0 || _flipView.Items == null || _flipView.SelectedIndex >= _flipView.Items.Count) return;
        var container = SafeContainerFromIndex(_flipView.SelectedIndex) as FrameworkElement;
        if (container == null) return;

        // Store sub_182172450 (handler wired on the FlipView by the compiled-XAML Connect step
        // 0x182172B70): when the current item is a VideoPlayerSource, assign its media to the
        // item's media element (slot 21) and set autoplay from a feature check. Re-applying
        // here is what makes a reopen start the clip from the beginning after the close-time
        // teardown — the same observable Store gets from its host content being nulled.
        if (FindDescendant<PreviewViewerVideoItem>(container) is { } videoItem)
        {
            videoItem.ApplySource();
        }

        var player = FindDescendant<MediaPlayerElement>(container);
        try { player?.MediaPlayer?.Play(); } catch { }
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
