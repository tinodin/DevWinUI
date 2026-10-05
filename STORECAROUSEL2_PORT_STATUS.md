# Microsoft Store Reconstruction & DevWinUI Port Status

**Document Purpose**: Tracks the static analysis findings, completed reverse-engineering steps, active work items, and roadmap for porting Microsoft Store's `ScreenshotsViewer`, `PreviewViewer`, and related controls into DevWinUI's `StoreCarousel2`.

---

## 1. Executive Summary & Architecture

The Microsoft Store (WinUI 2.8 / NativeAOT C#) implements screenshot and preview viewing as a coordinated multi-component system rather than a single monolithic control:

```
+-----------------------------------------------------------------------------------+
| PDP / Host Layer                                                                  |
|   Coordinates PreviewViewerHelper                                                 |
+-----------------------------------------+-----------------------------------------+
                                          |
        +---------------------------------+---------------------------------+
        |                                                                   |
        v                                                                   v
+-------------------------------+                         +---------------------------------+
| ScreenshotsViewer (UserControl|                         | PreviewViewer (OverlayPopupBase)|
| - Horizontal carousel strip   |                         | - Fullscreen modal viewer       |
| - NoPointerWheelListView      |                         | - ItemsFlipView (FlipView)      |
| - ScrollButtonsHostBehavior   |                         | - CaptionTextBlock (TextBlock)  |
| - Left/Right ScrollButtons    |                         | - IndexTextBlock (Grid / Runs)  |
| - ScrollCommand (Left/Right)  |                         | - CloseButton (Button)          |
| - ScreenshotDataTemplateSelect|                         +---------------------------------+
+---------------+---------------+                                           ^
                |                                                           |
                v                                                           |
+-------------------------------+                                           |
| PreviewViewerHelperListView...|                                           |
| - GetAnimationSourceElementName                                           |
| - PrepareForwardConnectedAnim | ---> "screenshotForwardAnimation" --------+
| - StartBackwardConnectedAnim  | <--- "screenshotBackAnimation" -----------+
| - UpdateSelectedIndex (Scroll)| <--- Syncs strip when FlipView changes ---+
+-------------------------------+
```

---

## 2. Reverse Engineering Findings & Decompilation Discoveries

### A. Instant Strip Synchronization via `sub_18225A1B0`
* **Address**: `0x18225A1B0..0x18225A59D` (1005 bytes).
* **Identity**: Recovered `PropertyChanged` handler on `PreviewViewer` / `PreviewItemsViewModel`.
* **Behavior**:
  1. `e.PropertyName == "SelectedIndex"`:
     * Reads `ViewModel.SelectedIndex` (`a2 + 0x38`).
     * Sets `FlipView.SelectedIndex = index`.
  2. `e.PropertyName == "Caption"`:
     * Reads `ViewModel.Caption` via `sub_1823F03D0`.
     * Sets `CaptionTextBlock.Text = caption`.
  3. `e.PropertyName == "CurrentIndex"`:
     * Reads `ViewModel.CurrentIndex` via `sub_1823F0480`.
     * Sets `IndexTextBlock` current-index Run text (`"N"`).
  4. `e.PropertyName == "TotalIndex"`:
     * Reads total count via `sub_18097B930`.
     * Sets `IndexTextBlock` total-index Run text (`"M"`).
  5. `string.IsNullOrEmpty(e.PropertyName)` (Initial Bind / Refresh):
     * Executes all four updates in sequence.

### B. Helper Method Identification: `UpdateSelectedIndex`
* **Address**: `0x1821B9AC3..0x1821B9B1C` (line 107 / `0x6B` in `PreviewViewerHelperListViewSource.cs`).
* **Recovered String**: `"UpdateSelectedIndex"` (`0x186085FE8`, 19 UTF-16 characters).
* **Behavior**: Previously noted in README v7 as unknown/unrecovered. Fully identified and implemented as `UpdateSelectedIndex`, which smoothly scrolls the strip `ListView` to the given index.

### C. Connected Animation Flow
1. **Forward Opening**:
   * User clicks item in `ScreenshotsViewer`.
   * `PreviewViewerHelper.SetUpPreviewViewer(item, flag)`:
     * Validates item type via `IsValidDataItem` (`0x18209E3E0`).
     * Calls `_listViewSource.PrepareForwardConnectedAnimation(dataItem)` (`0x18209EDA0`):
       * Resolves target element name (`"TileImageRootGrid"`, `"VideoPreviewImage"`, or `"VideoPlayerPreviewGrid"`).
       * Retrieves container from item via `ContainerFromItem`.
       * Runs visibility / non-zero bounds check (`0x1814F2140`).
       * Calls `service.PrepareToAnimate("screenshotForwardAnimation", container)`.
     * Sets `popup.List.CurrentItem = dataItem`.
     * Sets `popup.List.SelectedIndex = index` (fires `PropertyChanged`).
     * Calls `_previewViewer.OpenOverlayPopup()`.
2. **Forward Starting**:
   * `PreviewViewer.TryStartForwardConnectedAnimation()` (`0x18220B920`):
     * Retrieves `"screenshotForwardAnimation"`.
     * Applies `DirectConnectedAnimationConfiguration`.
     * Checks container type: if container is `PreviewViewerVideoItem` and `MediaPlayer.NaturalDuration.TotalSeconds > 0`, targets `MediaPlayerElement`; else targets image/container.
     * Calls `Focus(FocusState.Programmatic)` (`Focus(3)`).
3. **Backward Closing**:
   * `PreviewViewer.Close()` (`0x182172170`):
     * Calls `RemoveOverlayPopup()` (`0x180D91CA0`).
     * Calls `PrepareBackAnimation()` (`0x1821722D0`): prepares `"screenshotBackAnimation"` on current FlipView container.
     * Invokes `_closed` delegate.
   * `_listViewSource.StartBackwardConnectedAnimationAsync(item)` (`0x1821B9660`):
     * Retrieves `"screenshotBackAnimation"`.
     * Starts animation onto the strip tile descendant element (`"TileImageRootGrid"`, `"VideoPreviewImage"`, or `"VideoPlayerPreviewGrid"`).

### D. WinUI 2.8 vs WinUI 3 Differences
1. **Inlines & Verbatim / IndexTextBlock Run indices**:
   * WinUI 2.8 XAML used `<Verbatim Value=" " />` between `<Run>` elements in `IndexTextBlock`.
   * WinUI 3 XAML does not support `<Verbatim>`. The port keeps three `<Run>` elements (current / `"/"` / total) in `PreviewViewer.xaml`.
   * **WinUI 3 runtime quirk (verified)**: each XAML `<Run>` statement yields **two** entries in `TextBlock.Inlines` (the `Run` plus a trailing spacer inline). With three Runs, `Inlines.Count >= 5`.
     * Current index: `Inlines[0]` as `Run`
     * Total index: `Inlines[4]` as `Run`
   * Store / naive WinUI 2 indexing (`Inlines[0]` + `Inlines[2]`, `Count >= 3`) does **not** work on WinUI 3 — total stays unbound. Port must use `[0]`/`[4]` in `PreviewViewer.AttachParts`.
2. **Namespaces**:
   * MS Store uses `Windows.UI.Xaml` and NativeAOT WinRT thunks.
   * DevWinUI uses `Microsoft.UI.Xaml` (Windows App SDK / WinUI 3).
3. **Popup Hosting**:
   * MS Store PDP uses an overlay popup service (`OverlayPopupBase`).
   * DevWinUI uses WinUI 3 `Popup` with `XamlRoot` hosting, matching the behavior cleanly.
4. **BringIntoViewRequestedEventArgs**:
   * In WinUI 3 (`Microsoft.UI.Xaml`), `BringIntoViewRequestedEventArgs.HorizontalAlignmentRatio` is a read-only property (`get` only).
   * Do **not** blanket-route `BringIntoViewRequested` through SmoothScroll Center — that also ran on Escape focus restore. In-strip keyboard Center is gated in `GettingFocus` (see §I).

### E. Smooth Scrolling Algorithm & State Machine (`sub_180080120` / `sub_18007FC00` / `0x182D8EA00`)
* **Addresses**:
  * `0x180080120`: Decompiled as `CommunityToolkit.WinUI.ListViewExtensions.SmoothScrollIntoViewWithIndexAsync` (`<SmoothScrollIntoViewWithIndexAsync>d__22.MoveNext()`).
  * `0x18007FC00`: Decompiled as `ScrollViewer.ChangeViewAsync` (`<ChangeViewAsync>d__24.MoveNext()`).
  * `0x1833E5134`: Frozen UTF-16 string confirming both state machines: `",<ChangeViewAsync>d__24R<SmoothScrollIntoViewWithIndexAsync>d__22"`.
  * `0x182D8EA00`: Invocation wrapper for the `SmoothScrollIntoViewWithIndexAsync` state machine.
  * `0x182D8E7F0`: Invocation wrapper for the `ChangeViewAsync` state machine.
* **Sole WinStore.App code caller of `0x182D8EA00`**: `UpdateSelectedIndex` SM `0x1821B9980` (from `ScrollToIndex` / FlipView `SelectedIndex` sync). Call-site args [ASM]:
  * `itemPlacement = 0` (`ScrollItemPlacement.Default`) at stack offset matching Hex-Rays `*((_DWORD*)a1+14)`.
  * Both bools = `1` → `disableAnimation: true`, `scrollIfVisible: true`.
* **Algorithm & Switch Table** (`sub_180080120`):
  * **Case 0 (`ScrollItemPlacement.Default`)**: flush to closer edge if not fully visible; no-op if already fully visible. Used by `UpdateSelectedIndex`.
  * **Case 3 (`ScrollItemPlacement.Center`)**: centers horizontally (`* 0.5`). Present in the embedded Toolkit MoveNext; **no WinStore.App call site** passes placement=3 (global scan of SmoothScroll callers + placement imm). Port uses case 3 for **in-strip keyboard** tile-to-tile moves only (see §I).

### F. Container Layout Preservation During Connected Animation (`0x1804FD330` / `0x1804FCFD0`)
* **Addresses**:
  * `0x1804FD330` called from `PrepareForwardConnectedAnimation` (`0x18209EDA0`):
    * Decompiled as `ListViewBase.PrepareConnectedAnimation(string key, object item, string elementName)`.
    * In MS Store, `ListViewBase` coordinates the connected animation for the named child (`"TileImageRootGrid"`), preserving container layout and sizing within `ItemsStackPanel`.
    * In WinUI 3, directly calling `ConnectedAnimationService.PrepareToAnimate` on a container child causes the item and following tiles to collapse to width 0 unless container dimensions are explicitly locked.
  * `0x1804FCFD0` called from `StartBackwardConnectedAnimationAsync` (`0x1821B9660`):
    * Decompiled as `ListViewBase.TryStartConnectedAnimationAsync(ConnectedAnimation animation, object item, string elementName)`.

### G. Strip Selection Synchronization During FlipView Swipe
* **Wiring Mechanism**:
  * In MS Store, `FlipView.SelectionChanged` sets `ViewModel.SelectedIndex = idx`.
  * `ViewModel` raises `PropertyChanged("SelectedIndex")`.
  * `PreviewViewerHelper` listens to `_popup.List.PropertyChanged` matching `"SelectedIndex"`.
  * `OnPopupPreviewSelectionChanged` (`0x18209E5D0`) executes and calls `_listViewSource.ScrollToIndex(index)`.
  * `ScrollToIndex` runs `UpdateSelectedIndex` (`sub_180080120`), smoothly scrolling the underlying strip to keep the selected item in view aligned flush to the edge as the user swipes fullscreen screenshots.

### H. 15ms Layout Delay & Source Opacity Management (`0x18220B920` / `0x180A5CA00`)
* **Address**: `0x18220B920` (`TryStartForwardConnectedAnimationAsync`) calling `sub_180A5CA00(v22, 15, &off_1862C87F0)`.
* **Behavior**:
  * Decompiled `sub_180A5CA00` executes a 15ms delay (`Task.Delay(15)`, 150,000 100ns ticks = 1 frame) to allow the FlipView container to realize and measure before retrieving `FlipView.ContainerFromIndex(SelectedIndex)` (`sub_1804F9990`).
  * In WinUI 3, when `PrepareToAnimate` takes a snapshot of the source element in `ScreenshotsViewer`, DirectComposition captures the visual snapshot synchronously. The source element must be hidden (`Opacity = 0`) during the transition so that it does not remain visible behind the translucent `AcrylicInAppFillColorThinBrush` backdrop of `PreviewViewer`. When the viewer is closed or backward connected animation finishes, the source element opacity is restored to 1.

### I. Preview close / Escape vs in-strip keyboard Center (Hex-Rays / IDA 9.2, 22607 `.i64`) — **revised 2026-09-14**
* **Close path (verified ASM)**:
  * `0x182172170` (`PreviewViewer.Close`) — raises Closed; no strip Focus / no SmoothScroll.
  * `0x18209E7D0` → `0x1821B94F0` (`<OnPreviewViewerClosed>d__30`): await `StartBackwardConnectedAnimationAsync` only; **no Focus, no SmoothScroll**.
  * String `FocusItem`: **0 hits** — port helper name only.
* **SmoothScroll call sites (verified ASM)**:
  * Sole `0x182D8EA00` code caller = `UpdateSelectedIndex` with **Default** + `disableAnimation: true` (FlipView/strip sync / close `ScrollToIndex`).
  * Placement=3 (`Center`) has **no** WinStore.App caller; Center math still lives in Toolkit `sub_180080120` case 3.
* **ScreenshotsViewer.xaml**: no `GettingFocus` / `BringIntoViewRequested` / `PreviewKeyDown` handlers declared.
* **Port behavior (matches Store UX split)**:
  * **In-strip Left/Right**: `GettingFocus` with `InputDevice=Keyboard` **and** `OldFocusedElement` already a tile under `ScreenshotTileList` → `SmoothScrollIntoViewWithIndexAsync(..., Center, disableAnimation: false)`.
  * **Escape / PreviewViewer close**: `FocusItem` is Focus-only; GettingFocus gate skips Center because old focus is outside the strip; position sync remains `ScrollToIndex` (Default / instant).
  * Do **not** route all `BringIntoViewRequested` through Center — that also fired on Escape focus restore.

### J. ItemClick → Index → Open → Focus (Hex-Rays / IDA 9.2, 22607 `.i64`) — 2026-09-14
Verified against IDA database `WinStore.App.dll.i64` (on-disk PE RVAs at these addresses do **not** match; use the `.i64` / exported ASMs, not raw file bytes).

#### J.1 `ScreenshotsViewer` ItemClick handler = `sub_1821817F0`
* **Address**: `0x1821817F0..0x182181951`.
* **Xref**: sole static caller of `SetUpPreviewViewer` (`0x18209E190`).
* **Recovered body** (Hex-Rays):
  1. Cast `ItemClickEventArgs` / read `ClickedItem` via WinRT getter `sub_18046F130`.
  2. Raise host `Clicked` / telemetry (`sub_18117F630(..., edx=2, ...)` is an **event/telemetry** raise with payload field `= 2`, **not** `FocusState.Keyboard`).
  3. Tail-call:
     ```c
     return SetUpPreviewViewer(helper /* this+0x178 */, ClickedItem, /* flag */ 1u);
     ```
* **Correction**: prior reconstruction used `flag: false`. ASM passes **`1` (`true`)**.

#### J.2 `SetUpPreviewViewer` (`0x18209E190`) → index → `OpenPreviewViewer`
* Hex-Rays confirms:
  1. `IsValidDataItem(this, item, flag)`.
  2. `PrepareForwardConnectedAnimation` (`0x18209EDA0`) — **no Focus call**.
  3. `popup.List.CurrentItem = item` (`[list+0x30]`).
  4. `index = IndexOf(popup.List, item)` (`0x1823F0A40`).
  5. Tail `OpenPreviewViewer(this, index, itemKey)` (`0x18209E820`).

#### J.3 `IndexOf` (`0x1823F0A40`)
* Operates on **preview list VM items** (`[list+0x20]` collection), not a separate ad-hoc index.
* EEType-specific paths; on failure returns **`0xFFFFFFFF` (−1)**.
* `OpenPreviewViewer`: `index < 0` → return; `count <= index` → **`index = 0`** (`cmovle`).

#### J.4 `OpenPreviewViewer` (`0x18209E820`) — **no Focus**
* Sets `popup.List.SelectedIndex = index` via setter `0x1823F0170` (raises `PropertyChanged` for `SelectedIndex` / `Caption` / `CurrentIndex` / `TotalIndex`).
* Calls `OpenOverlayPopup` (`0x182172020`).
* Does **not** call `Control.Focus`.

#### J.5 Focus removal on open = `Focus(FocusState.Programmatic)` on FlipView (not `Unfocused`)
* **`sub_180D8FDF0`** is invoked from **`OpenOverlayPopup`** (`0x182172020`), i.e. the **open** path (prior note labeling it a close handler was wrong).
* Hex-Rays at Focus site:
  * Target = control at preview viewer field **`+0x108`** (`ItemsFlipView`).
  * ABI vtable slot **`+0x170`** (368), call **`Focus(FocusState=3)`** (`mov edx, 3` at `0x180D90006`).
* Second Focus(3) on FlipView in `TryStartForwardConnectedAnimation` (`0x18220BAFB`) after connected animation `TryStart`.
* **No** `Focus(FocusState.Unfocused)` / `xor edx,edx` Focus call exists on the ItemClick → SetUp → Open → TryStart path.
* Prior scratch conclusion that `xor edx,edx` inside mis-bounded “OpenPreviewViewer” was `Focus(Unfocused)` is **rejected**: those sites are other WinRT puts / null stores; real Focus uses vtable `+0x170` with `edx∈{1,3}` in the dumps above.
* Effect: keyboard focus leaves the strip `ListViewItem` (focus visual clears) because focus moves to FlipView.

#### J.6 Port bugs implied by J.1–J.5
1. **Wrong open index on first load**: Hex-Rays `OpenPreviewViewer` (`0x18209E820`) only calls `SelectedIndex` setter (`0x1823F0170`) then `OpenOverlayPopup` (`0x182172020`). It does **not** rebind items. FlipView already has `ItemsSource` from ViewModel wire-up (`0x18225A0A0`). Our port rebuilt the preview list and rebound `ItemsSource` on each click / first `Loaded`, so FlipView defaulted to **0** and `SelectionChanged` wrote **0** back into the list VM after a correct `SelectedIndex` had been set. Fix: one shared list VM; `IndexOf`/`count` use that list’s items (`[list+0x20]`); bind `ItemsSource` once; on open only `SelectedIndex = index` then show.
2. **Keyboard focus border in forward animation**: Store clears strip focus by **`ItemsFlipView.Focus(Programmatic)` during `OpenOverlayPopup`**, not by focusing the close button or calling `Focus(Unfocused)` on the tile.

#### J.7 `CurrentItem` write is a raw field put — not a SelectedIndex update
* In `SetUpPreviewViewer` (`0x18209E190`), CurrentItem is assigned as:
  `sub_1830F19C0(list + 0x30 /*48*/, ClickedItem)` — WinRT/`IInspectable` field put only.
* **No** IndexOf and **no** `SelectedIndex` write happen in that step.
* Index is computed next via `IndexOf` (`0x1823F0A40`) on `[list+0x20]`; then **only** `OpenPreviewViewer` → `SelectedIndex` setter (`0x1823F0170`) raises `PropertyChanged("SelectedIndex")` → FlipView (`0x18225A1B0`).
* Port bug: `PreviewItemsViewModel.CurrentItem` also did `_items.IndexOf` → `SelectedIndex = idx`. That pre-armed `SelectedIndex` to the clicked index, so `OpenPreviewViewer`’s `SelectedIndex = index` became a **no-op** (setter returns when value unchanged → **no** PropertyChanged). If FlipView had not yet applied that early write (or still sat at 0), overlay opened on item 0.

#### J.8 ScreenshotsViewer Left/Right buttons = `ScrollCommand` (not code-behind Click)
* **XAML** (`ScreenshotsViewer.xaml`): each arrow button hosts
  `<Button.Command><ScrollCommand ScrollDirection="Left|Right" /></Button.Command>`.
  Visibility starts `Collapsed`; `ScrollButtonsHostBehavior` is on the strip Grid.
* **DPs** [DPT/RT]: `ScrollDirection` (`WinStore.UX.Common.ScrollDirection`, register `0x1821232EB`), `TargetElement` (`FrameworkElement`, `0x1820EDA8B`). PDP XAML does **not** bind `TargetElement`; runtime wires it to the strip `ScrollViewer` (connect / `0x18150B100`).
* **Enum** from switch cases in `0x18150C860` / `0x18150C930`: `None=0`, `Left=1`, `Right=2`, `Up=3`, `Down=4`.
* **ICommand EEType slots** (`unk` map at `0x183ECFA00`):
  * CanExecute = `0x18150AF10` → requires cached ScrollViewer at command `+0x40`, then `0x18150C860(direction)`.
  * Execute = `0x18150AF40` → `0x18150C840(sv, direction)` → horizontal `0x18150C930` / vertical `0x18150CA30`.
* **Horizontal scroll** (`0x18150C930`):
  * `delta = ViewportWidth * 0.9` (`sub_1804FFBB0` / vtable +328 `* 0.9`).
  * Left: `ChangeView(HorizontalOffset - delta, null, null, disableAnimation: false)` via `sub_1804FFDA0`.
  * Right: `ChangeView(HorizontalOffset + delta, ...)`.
* **CanExecute horizontal** (`0x18150C860`) — exact getters:
  * Left: `HorizontalOffset > 0` (`0x1804FF840`, vtable +320).
  * Right: `ScrollableWidth > HorizontalOffset` (`0x1804FF9A0`, vtable +336). **Not** `ExtentWidth - ViewportWidth - epsilon`.
  * Vertical pair: `0x1804FFA50` / `0x1804FF8F0` (ScrollableHeight).
* **Attach** (`0x18150B1F0`): `ViewChanged` + `SizeChanged` on the ScrollViewer **and** `SizeChanged` on the content child (SV field +344 → UIElement `unk_183DCA040`). Content SizeChanged refreshes CanExecute when the strip measures **before** any scroll.
* **Port**: `ScrollCommand.cs` + XAML Commands; behavior sets `TargetElement` to strip `ScrollViewer`.
* **WinUI 3 note**: Store assigns a custom DO+`ICommand` as a `Button.Command` property-element. Plain/`System.Windows.Input.ICommand` property-elements throw `XamlParseException` (0x802B000A) on WinUI 3. Port derives `ScrollCommand` from `XamlUICommand` so the same Store XAML shape works.

#### J.9 Hover show Left/Right **before** first scroll (Host Visibility vs Disabled style)
* **Bug (port)**: arrows stayed hidden on hover until the strip was scrolled once.
* **Store XAML** (`ButtonResourceDictionary.xaml`):
  * `ArrowScrollViewerButtonDisabledVisiblity` = `Collapsed`.
  * `HorizontalArrowScrollViewerButtonStyle` Disabled state sets template Root `Visibility` to that resource.
  * Therefore an edge that `CanExecute` rejects is hidden by **Disabled**, not by Host collapsing the `Button` from offset math.
* **`ScrollButtonsHostBehavior`**:
  * DPs [DPT/RT]: `IsEnabled` (Boolean, `0x180D248FE`), `LeftScrollButton` / `RightScrollButton` (Button).
  * Method bodies live in R2R merge regions — **not** decompiled as named functions (string xrefs only hit DP register / type tables).
  * Contract from XAML + J.8: on pointer-over set **both** buttons `Visible` (when behavior `IsEnabled`); on pointer-exit both `Collapsed`. Do **not** invent Host `ExtentWidth > ViewportWidth + 1` / per-edge `Visibility` (prior port did; that only re-evaluated on `ViewChanged` → looked like “need first scroll”).
* **Port fix (2026-09-14)**: Host pointer-over Visibility only; edge hide via CanExecute → IsEnabled → Disabled style; CanExecute uses `ScrollableWidth`; Attach also content `SizeChanged` like `0x18150B1F0`.
* **Note**: Store `StaticListViewStyle` wraps `ItemsPresenter` in `StaticListItemsPresenterWrapper` (XAML). Wrapper method bodies not recovered (type string only). Port still omits the wrapper; hover-before-scroll is restored via J.8 Attach + J.9 Host/Disabled split, which are the pieces with ASM/XAML proof.

#### J.10 Same-item reopen freezes Caption / index while FlipView navigates
* **Bug (port)**: Open preview → close → click the **same** strip tile again → inside that second session, Caption + index stay stuck on the reopened item even when FlipView moves to other items. Opening a **different** tile after close looks fine again.
* **Store SelectedIndex setter** (`0x1823F0170`):
  * Early-out when `list+56` already equals the new value — **no** PropertyChanged.
  * On change only: raises PropertyChanged for SelectedIndex, Caption, CurrentIndex, TotalIndex (four name offs at `0x1860382D0` / `0x185F726E0` / `0x185F88F00` / `0x18607B658`).
* **Store Caption / CurrentIndex getters** (computed, not cached strings):
  * Caption `0x1823F03D0`: derive text from the item at SelectedIndex.
  * CurrentIndex `0x1823F0480`: `sub_18097B930(SelectedIndex + 1)` (ToString).
  * TotalIndex path in `0x18225A1B0`: items collection count via `sub_18097B930`.
* **Store PropertyChanged handler** (`0x18225A1B0`): on those names (or empty name) writes FlipView.SelectedIndex / CaptionTextBlock / index Runs from the getters above.
* **Store FlipView → list**: PreviewViewer `SelectionChanged` must keep writing `ViewModel.SelectedIndex` so Caption/CurrentIndex PropertyChanged fire on swipe. Store does not tear that subscription down on close (no WinUI 3 Popup unload in the PDP host).
* **Port root cause (WinUI 3 Popup adaptation)**: `Hide` → `Unloaded` → `DetachParts` did `SelectionChanged -=`. `AttachParts` only re-subscribed when the FlipView **instance** changed. Popup reuses the same FlipView → after first close, FlipView navigation no longer updated the list VM → Caption/index frozen. Same-item reopen made this obvious because `SelectedIndex` setter also early-outs (no open-path PropertyChanged to “rescue” the UI).
* **Port fix**: `AttachParts` always `-=`/`+=` `SelectionChanged` after unload; `PreviewItemsViewModel.Caption` / `CurrentIndex` / `TotalIndex` match Store computed getters (no stale string caches).

---

### K. Theme change propagation: Store keeps theme-dependent aliases INSIDE `ThemeDictionaries`, port froze them as top-level `StaticResource` — 2026-09-15
Verified only against Store `.xbf`-extracted XAML (`Microsoft.WindowsStore_22606...xaml\xbf\...`) and the port sources below. No ASM inference; no invented keys.

* **Store pattern [XAML]** — theme-dependent brush aliases live **inside** `ResourceDictionary.ThemeDictionaries` (`Default`/`Light`/`HighContrast`), one entry per theme:
  * `Resources\ButtonResourceDictionary.xaml`: `SubtleIconButton*` (12 brushes + `SubtleIconButtonBorderThemeThickness`, Default lines 18–30 / Light 120–132 / HC 222–234), `PlayButton*` (12 brushes, Default 42–53 / Light 144–155 / HC 246–257), `ArrowScrollViewer*` (7 brushes, Default 108–116 / Light 210–218 / HC 312–320; `ArrowScrollViewerButtonBorderThemeThickness` Default/Light `0`, HC `1`, lines 107/209/311).
  * `WinStore.Resources\Themes\Dark.xaml:32,2,34` / `Light.xaml:32,2,34` / `HighContrast.xaml:33,2,35`: `ImageOpacityOverlayBackgroundBrush` (Opacity `0.4`, `#FF000000`, all themes), `WhiteTextForegroundBrush` (`SystemChromeWhiteColor` on Dark/Light, `SystemColorWindowTextColor` on HC), `HighContrastBackgroundBrush` (`#00FFFFFF` on Dark/Light, `SystemColorWindowColor` on HC).
  * Theme-**invariant** keys stay **outside** `ThemeDictionaries` in Store and must stay `StaticResource`: `ArrowScrollViewerButtonWidth/FontSize/DisabledOpacity/DisabledVisiblity/CornerRadius` (`ButtonResourceDictionary.xaml:3462–3473`), `SubtleIconButtonWidth/Height/Padding` (lines 592,598–599), `DefaultBackgroundTransition` (`App.xaml:691`). The styles themselves (`SubtleButtonStyle`, `VideoPlayButtonStyle`, arrow button styles) sit outside and reference state brushes via `{ThemeResource ...}`.
  * Consumer reference is theme-aware: Store `PreviewViewer.xaml:533` uses `Style="{ThemeResource SubtleIconButtonStyle}"` for the CloseButton; strip arrow buttons stay `Style="{StaticResource Left|RightArrowScrollViewerButtonStyle}"` (`ScreenshotsViewer.xaml:142,147`) because only their *inner* template brushes are theme-dependent.
* **Port bugs [XAML]** (`StoreCarousel2.ThemeResources.xaml`, `StoreCarousel2.xaml`):
  1. `PlayButton*` (12), `ArrowScrollViewer*` brushes (7) + `ArrowScrollViewerButtonBorderThemeThickness`, and the 12 `SubtleIconButton*` brushes (`StoreCarousel2.xaml:105–116`) were top-level `<StaticResource ... ResourceKey="..."/>` aliases — resolved once at first load, so `{ThemeResource ...}` lookups against them kept returning the first theme's brush. That is exactly the reported symptom: strip back/forward buttons stuck until relaunch; PreviewViewer CloseButton (via `SubtleIconButtonStyle`) and all `PlayButton*` states stuck on the theme that was active at first open.
  2. Wrong values vs Store (now corrected to the lines above): port had all four `PlayButtonBorderBrush*` = `ControlStrokeColorDefaultBrush` (Store: Pressed `ControlStrokeColorOnAccentDefaultBrush`, Hover `ControlStrokeColorSecondaryBrush`, Disabled `ControlStrokeColorOnAccentDisabledBrush`) and `PlayButtonBackgroundDisabled` = `ControlOnImageFillColorDefaultBrush` (Store: `ControlOnImageFillColorDisabledBrush`); port had `SubtleIconButtonForegroundPressed` = Primary on all themes (Store Light = `TextFillColorSecondaryBrush`, `ButtonResourceDictionary.xaml:121`).
  3. Dangling aliases: port `ImageOpacityOverlayBackgroundBrush` / `WhiteTextForegroundBrush` / `HighContrastBackgroundBrush` pointed at never-defined `ScreenshotsCarouselImageOpacityOverlayBrush` / `...WhiteTextForegroundBrush` / `...HighContrastBackgroundBrush` keys (no definition anywhere in `dev/`). Replaced with direct per-theme definitions from Store `Dark/Light/HighContrast.xaml` above.
* **Port fix (2026-09-15)**:
  * `StoreCarousel2.ThemeResources.xaml`: the three `ThemeDictionaries` entries (`Default`/`HighContrast`/`Light`) now each contain `SubtleIconButton*` (13), `PlayButton*` (12), `ArrowScrollViewer*` brushes + border thickness (10), and the three AgeRestricted-tile brushes, with the exact per-theme Store values cited above. Top-level frozen aliases removed (theme-invariant `DisabledOpacity`/`DisabledVisiblity` kept, Store `ButtonResourceDictionary.xaml:3471–3472`).
  * `StoreCarousel2.xaml`: `SubtleIconButton*` brush aliases removed; sizes + `SubtleIconButtonStyle` (which only uses `{ThemeResource ...}` for state brushes) stay, so the requested "move to ThemeResources completely" is done without changing the style's shape. `PreviewViewer.xaml:484` already used `{ThemeResource SubtleIconButtonStyle}` (matches Store `:533`) — no change needed there.
  * Not touched (not verifiable in Store `.xbf`): `ExpanderChevron*` aliases (no `x:Key="ExpanderChevronBackground|BorderBrush|Foreground"` definition found in Store XAML — likely framework-provided).
* **Note**: `dev/DevWinUI/Themes/Generic.xaml` is build-generated (`XAMLTools.MSBuild`, `DevWinUI.csproj:27–34`) from these sources — not edited by hand.

## 3. Completed Tasks

### Phase 1: Reconstructed Codebase (`G:\Reverse Engineering\Microsoft Store\reconstructed\newer\reconstructed\`)
- [x] Static analysis of all dump assemblies in `dumps/22607/` and `dumps/22606/`.
- [x] Targeted Hex-Rays decompilation of:
  - `0x18225A1B0` (PropertyChanged event handler for FlipView & Caption/Index sync).
  - `0x18225A0A0` (ViewModel subscription wire-up).
  - `0x18225A140` (ViewModel accessor / validity check).
  - `0x182172020` (`OpenOverlayPopup`).
  - `0x182172170` (`Close`).
  - `0x1821722D0` (`PrepareBackAnimation`).
  - `0x18220B920` (`TryStartForwardConnectedAnimationAsync`).
  - `0x1823F2760`, `0x182451D40`, `0x1824520A0` (`PreviewModuleViewModel`).
  - `0x180080120` (scroll alignment algorithm to fit items in view with edge).
  - `0x1804FD330` (`ListViewBase.PrepareConnectedAnimation`).
  - `0x1804FCFD0` (`ListViewBase.TryStartConnectedAnimationAsync`).
- [x] Recovered unknown line-107 method name in `PreviewViewerHelperListViewSource.cs` (`UpdateSelectedIndex`).
- [x] Completed `PreviewViewer.cs` with exact PropertyChanged sync (`sub_18225A1B0`), forward/back connected animation logic, programmatic focus (`Focus(3)`), and overlay popup management.
- [x] Completed `PreviewViewerHelper.cs` with `SetUpPreviewViewer`, `OnPopupPreviewSelectionChanged`, `OpenPreviewViewer`, `IsValidDataItem`, and `GetItemKey`.
- [x] Completed `PreviewViewerHelperListViewSource.cs` with `GetAnimationSourceElementName`, `PrepareForwardConnectedAnimation` (with container dimension preservation and `ListViewBase.PrepareConnectedAnimation`), `UpdateSelectedIndex` (faithful `sub_180080120` scroll-to-edge), `ScrollToIndex`, and `StartBackwardConnectedAnimationAsync`.
- [x] Completed `ScreenshotsViewer.cs` with full DP fidelity, `PreviewViewModel` integration, immediate scroll-to-edge on `ItemClick`, and `ItemClick` delegation.

### Phase 2: DevWinUI Port (`g:\GitHub\My Repositories\DevWinUI\dev\DevWinUI\Controls\Ported\StoreCarousel2\`)
- [x] Updated `StoreViewModels.cs`:
  - Added `SelectedIndex`, `Caption`, `CurrentIndex`, and `TotalIndex` properties raising `PropertyChanged` to support the exact Store binding contract.
  - Added `OnFullscreenParentShown` and `FullscreenParentShown` event to `PreviewModuleViewModel`.
- [x] Created `PreviewViewerHelper.cs` in DevWinUI conforming to WinUI 3 with `IList<object>` ItemCollection compatibility.
- [x] Created `PreviewViewerHelperListViewSource.cs` in DevWinUI conforming to WinUI 3 with:
  - MS Store `sub_180080120` flush-to-edge scrolling logic.
  - Native `ListViewBase.PrepareConnectedAnimation` and container size locking to prevent items collapsing on click.
  - Native `ListViewBase.TryStartConnectedAnimationAsync` for back animations.
- [x] Updated `PreviewViewer.cs` in DevWinUI to implement the exact `sub_18225A1B0` PropertyChanged handler for caption/index runs, video duration check (`duration > 0 ? player : container`), programmatic focus, and direct connected animation configurations.
- [x] Updated `ScreenshotsViewer.cs` in DevWinUI with `Helper` property and immediate scroll-to-edge on item click.
- [x] Updated `StoreCarousel2.cs` to integrate and coordinate `ScreenshotsViewer`, `PreviewViewerHelper`, `PreviewViewerHelperListViewSource`, and `PreviewViewer`:
  - Synchronized `_viewer.ViewModel` with `_helper.Popup.List` so that FlipView selection changes in `PreviewViewer` smoothly scroll the `ScreenshotsViewer` strip to keep the active item flush in view.
  - Connected `OnViewerClosed` to restore position via `ScrollToIndex` (UpdateSelectedIndex Default / disableAnimation true).
  - WinUI 3 Popup adaptation: `FocusItem(finalIndex, Keyboard|Programmatic)` on close for tile focus only — **no** SmoothScroll Center (Store `OnPreviewViewerClosed` has no Focus/SmoothScroll).
  - Restored strip tile source visibility upon viewer exit.
- [x] Updated `PreviewViewerHelperListViewSource.cs` with `RestoreSourceVisibility` and immediate source tile opacity management (`Opacity = 0`) upon forward animation preparation, eliminating ghost tiles visible behind the translucent acrylic popup.
- [x] Updated `PreviewViewer.cs` with the decompiled 15ms layout wait loop (`sub_180A5CA00` from `0x18220B920`) and active keyboard navigation detection (`WasKeyboardFocusActive`).
- [x] Created `ListViewExtensions.SmoothScrollIntoView.cs` implementing reverse-engineered `SmoothScrollIntoViewWithIndexAsync` (`sub_180080120`) and `ChangeViewAsync` (`sub_18007FC00`) supporting `ScrollItemPlacement.Center` and `Default`.
- [x] **2026-09-14 (Escape / strip center)**: IDA: sole `0x182D8EA00` caller is `UpdateSelectedIndex` (**Default**); `OnPreviewViewerClosed` has no SmoothScroll. Restored in-strip keyboard **Center** via `GettingFocus` gated on old focus already in the strip; Escape restore Focus-only (no Center). Removed blanket `BringIntoViewRequested`→Center (that also fired on close).
- [x] **2026-09-14**: IDA Hex-Rays confirmation of ItemClick `sub_1821817F0` (`SetUpPreviewViewer(..., flag:1)`), rejection of `Focus(Unfocused)` on open, and `OpenOverlayPopup` → `Focus(FlipView, Programmatic)`. Port updated: shared preview list VM, ItemClick → `SetUpPreviewViewer`, FlipView programmatic focus on open (clears strip keyboard focus visual).
- [x] **2026-09-14 (index)**: Hex-Rays `OpenPreviewViewer` is SelectedIndex-only (`0x1823F0170`) + `OpenOverlayPopup`; IndexOf/count use list VM items at `[list+0x20]`. Port no longer rebuilds/rebinds items on each click; binds FlipView `ItemsSource` once so first click SelectedIndex is not clobbered back to 0.
- [x] **2026-09-14 (index/CurrentItem)**: Hex-Rays `SetUpPreviewViewer` writes CurrentItem only via `sub_1830F19C0(list+0x30)` (no SelectedIndex). Removed port `CurrentItem`→`SelectedIndex` IndexOf coupling so open-path `0x1823F0170` always raises PropertyChanged for FlipView.
- [x] **2026-09-14 (ScrollCommand)**: Recovered Store Left/Right strip buttons: XAML `ScrollCommand` + EEType Execute `0x18150AF40` → `0x18150C930` (`ChangeView` by `ViewportWidth*0.9`). Port adds `ScrollCommand.cs`, wires Commands in `ScreenshotsViewer.xaml`, behavior sets `TargetElement` to strip `ScrollViewer`.
- [x] **2026-09-14 (hover arrows before scroll)**: IDA `0x18150C860` = `ScrollableWidth > HorizontalOffset`; Attach `0x18150B1F0` includes content SizeChanged; XAML Disabled → `ArrowScrollViewerButtonDisabledVisiblity=Collapsed`. Removed invented Host ExtentWidth Visibility gate; Host only toggles both buttons on pointer-over.
- [x] **2026-09-15 (theme propagation)**: verified Store keeps `SubtleIconButton*`/`PlayButton*`/`ArrowScrollViewer*` aliases inside `ThemeDictionaries` per theme (`ButtonResourceDictionary.xaml`) and tile brushes per theme (`Dark/Light/HighContrast.xaml`); port had them as frozen top-level `StaticResource`. Moved all of them into `StoreCarousel2.ThemeResources.xaml` `Default`/`Light`/`HighContrast` with exact Store values; fixed wrong `PlayButtonBorderBrush*`/`PlayButtonBackgroundDisabled`/`SubtleIconButtonForegroundPressed(Light)` values; replaced dangling `ScreenshotsCarousel*` aliases with direct Store definitions.
- [x] **2026-09-14 (same-item reopen caption/index)**: IDA `0x1823F0170` early-outs on same SelectedIndex; Caption `0x1823F03D0` / CurrentIndex `0x1823F0480` are computed getters. Port bug was Popup `DetachParts` dropping FlipView `SelectionChanged` without re-subscribe on reused FlipView — navigation no longer raised Caption/CurrentIndex. Fixed AttachParts always rewires SelectionChanged; VM getters match Store.

---

## 4. Notes for Succeeding Agents
1. All changes adhere strictly to static reverse-engineering findings (`[STR]`, `[ASM]`, `[DPT]`, `[XAML]`). Prefer the **22607 `.i64` / exported ASMs** over raw on-disk PE bytes at historical RVAs (file image can disagree with the IDA DB).
2. WinUI 3 `IndexTextBlock`: each XAML `<Run>` yields two `Inlines`; use `Inlines[0]` / `Inlines[4]` for current/total (not `[0]`/`[2]`). Do not reintroduce WinUI 2.8 `<Verbatim>`.
3. Note that `dotnet build` is not supported on this project and should not be run.
4. Do not reintroduce `Focus(FocusState.Unfocused)` on strip items for open-path focus clearing unless a new ASM site with Focus vtable `+0x170` and `edx=0` is found.
5. Strip arrow buttons must use `ScrollCommand` (`ViewportWidth * 0.9` via `0x18150C930`), not invented Click handlers. Do not revive the old reconstructed `0.8` factor — that was `[INF]`, not decompiled.
6. In-strip keyboard Center: `GettingFocus` + old focus already in strip → SmoothScroll Center. Do **not** Center on Escape/`OnPreviewViewerClosed` (ASM: no SmoothScroll there). Do **not** blanket-handle `BringIntoViewRequested`→Center (that also fired on close restore).
7. Strip hover arrows: Host sets both `Visible` on pointer-over; edge hide is CanExecute → Disabled → `ArrowScrollViewerButtonDisabledVisiblity`. Do **not** reintroduce Host `ExtentWidth`/`HorizontalOffset` Visibility math. Keep content `SizeChanged` on `ScrollCommand` Attach (`0x18150B1F0`).
8. After Popup unload, `AttachParts` must always re-subscribe FlipView `SelectionChanged` even if the FlipView instance is reused. Caption/CurrentIndex/TotalIndex stay computed from SelectedIndex/items (Store `0x1823F03D0` / `0x1823F0480` / count) — do not cache separate caption/index strings.
9. Theme-dependent brush aliases (`SubtleIconButton*`, `PlayButton*`, `ArrowScrollViewer*` state brushes, AgeRestricted tile brushes) must live **inside** `ThemeDictionaries` per theme (Store `ButtonResourceDictionary.xaml` + `Dark/Light/HighContrast.xaml`). Do **not** reintroduce top-level `<StaticResource x:Key="PlayButton..." ResourceKey="..."/>` aliases — they freeze to the first-loaded theme. Theme-invariant sizes (`...Width/Height/FontSize/DisabledOpacity/DisabledVisiblity/CornerRadius/Padding`, `DefaultBackgroundTransition`) stay outside as `StaticResource` per Store `ButtonResourceDictionary.xaml:3462–3473`.
