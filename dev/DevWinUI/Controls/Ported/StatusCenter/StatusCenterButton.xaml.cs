// Copyright (c) Files Community
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace DevWinUI
{
	public sealed partial class StatusCenterButton : UserControl
	{
		public static readonly DependencyProperty FlyoutPlacementProperty =
			DependencyProperty.Register(nameof(FlyoutPlacement), typeof(FlyoutPlacementMode), typeof(StatusCenterButton), new PropertyMetadata(FlyoutPlacementMode.BottomEdgeAlignedRight, OnFlyoutPlacementChanged));

		public StatusCenterViewModel ViewModel { get; } = StatusCenterViewModel.Instance;

		/// <summary>
		/// Gets or sets the placement of the status center flyout.
		/// </summary>
		public FlyoutPlacementMode FlyoutPlacement
		{
			get => (FlyoutPlacementMode)GetValue(FlyoutPlacementProperty);
			set => SetValue(FlyoutPlacementProperty, value);
		}

		public StatusCenterButton()
		{
			InitializeComponent();

			StatusCenterFlyout.Placement = FlyoutPlacement;

			Loaded += (_, _) => ViewModel.WindowHandle = WindowHelper.GetWindowHandle(this);
		}

		private static void OnFlyoutPlacementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is StatusCenterButton { StatusCenterFlyout: not null } statusCenterButton)
				statusCenterButton.StatusCenterFlyout.Placement = (FlyoutPlacementMode)e.NewValue;
		}

		private void Button_AccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// Suppress access key invocation if any dialog is open
			if (XamlRoot is not null && VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any())
				args.Handled = true;
		}
	}
}
