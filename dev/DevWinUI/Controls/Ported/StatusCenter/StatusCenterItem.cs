// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using WinRT;

namespace DevWinUI
{
	/// <summary>
	/// Represents an item for Status Center operation tasks.
	/// <br/>
	/// Handles all operation's functionality and UI.
	/// </summary>
	public sealed partial class StatusCenterItem : ObservableObject
	{
		private readonly StatusCenterViewModel _viewModel = StatusCenterViewModel.Instance;

		private int _ProgressPercentage;
		public int ProgressPercentage
		{
			get => _ProgressPercentage;
			set
			{
				ProgressPercentageText = $"{value}%";
				SetProperty(ref _ProgressPercentage, value);
			}
		}

		private string? _Header;
		public string? Header
		{
			get => _Header;
			set => SetProperty(ref _Header, value);
		}

		// Currently, shown on the tooltip
		private string? _SubHeader;
		public string? SubHeader
		{
			get => _SubHeader;
			set => SetProperty(ref _SubHeader, value);
		}

		private string? _Message;
		public string? Message
		{
			get => _Message;
			set => SetProperty(ref _Message, value);
		}

		private string? _SpeedText;
		public string? SpeedText
		{
			get => _SpeedText;
			set => SetProperty(ref _SpeedText, value);
		}

		private string? _ProgressPercentageText;
		public string? ProgressPercentageText
		{
			get => _ProgressPercentageText;
			set => SetProperty(ref _ProgressPercentageText, value);
		}

		// Gets or sets the value that represents the current processing item name.
		private string? _CurrentProcessingItemName;
		public string? CurrentProcessingItemName
		{
			get => _CurrentProcessingItemName;
			set => SetProperty(ref _CurrentProcessingItemName, value);
		}

		// TODO: Remove and replace with Message
		private string? _CurrentProcessedSizeText;
		public string? CurrentProcessedSizeHumanized
		{
			get => _CurrentProcessedSizeText;
			set => SetProperty(ref _CurrentProcessedSizeText, value);
		}

		// This property is basically handled by an UI element - ToggleButton
		private bool _IsExpanded;
		public bool IsExpanded
		{
			get => _IsExpanded;
			set
			{
				AnimatedIconState = value ? "NormalOn" : "NormalOff";

				SetProperty(ref _IsExpanded, value);
			}
		}

		// This property is used for AnimatedIcon state
		private string? _AnimatedIconState;
		public string? AnimatedIconState
		{
			get => _AnimatedIconState;
			set => SetProperty(ref _AnimatedIconState, value);
		}

		// If true, the chevron won't be shown.
		// This property will be false basically if the proper progress report is not supported in the operation.
		private bool _IsSpeedAndProgressAvailable;
		public bool IsSpeedAndProgressAvailable
		{
			get => _IsSpeedAndProgressAvailable;
			set => SetProperty(ref _IsSpeedAndProgressAvailable, value);
		}

		// This property will be true basically if the operation was canceled or the operation doesn't support proper progress update.
		private bool _IsIndeterminateProgress;
		public bool IsIndeterminateProgress
		{
			get => _IsIndeterminateProgress;
			set => SetProperty(ref _IsIndeterminateProgress, value);
		}

		// This property will be true if the item card is for in-progress and the operation supports cancellation token also.
		private bool _IsCancelable;
		public bool IsCancelable
		{
			get => _IsCancelable;
			set => SetProperty(ref _IsCancelable, value);
		}

		// This property is not updated for now. Should be removed.
		private StatusCenterItemProgressModel _Progress = null!;
		public StatusCenterItemProgressModel Progress
		{
			get => _Progress;
			set => SetProperty(ref _Progress, value);
		}

		public ReturnResult FileSystemOperationReturnResult { get; private set; }

		public FileOperationType Operation { get; private set; }

		public StatusCenterItemKind ItemKind { get; private set; }

		public StatusCenterItemIconKind ItemIconKind { get; private set; }

		public long TotalSize { get; private set; }

		public long TotalItemsCount { get; private set; }

		public bool IsInProgress { get; private set; }

		public bool IsDiscovering { get; private set; } = true;

		public IEnumerable<string>? Source { get; private set; }

		public IEnumerable<string>? Destination { get; private set; }

		public string? HeaderStringResource { get; private set; }

		public string? SubHeaderStringResource { get; private set; }

		public double IconBackgroundCircleBorderOpacity { get; private set; }

		public CancellationToken CancellationToken
			=> _operationCancellationToken?.Token ?? default;

		public string? HeaderTooltip
			=> string.IsNullOrWhiteSpace(SubHeader) ? SubHeader : Header;

		public readonly Progress<StatusCenterItemProgressModel> ProgressEventSource;

		private readonly CancellationTokenSource? _operationCancellationToken;

		public readonly ObservableCollection<Vector2> SpeedGraphValues;

		public ICommand CancelCommand { get; }

		[DynamicWindowsRuntimeCast(typeof(SolidColorBrush))]
		public StatusCenterItem(
			string headerResource,
			string subHeaderResource,
			ReturnResult status,
			FileOperationType operation,
			IEnumerable<string>? source,
			IEnumerable<string>? destination,
			bool canProvideProgress = false,
			long itemsCount = 0,
			long totalSize = 0,
			CancellationTokenSource? operationCancellationToken = default)
		{
			_operationCancellationToken = operationCancellationToken;
			Header = headerResource == string.Empty ? headerResource : headerResource.GetStatusCenterResource();
			HeaderStringResource = headerResource;
			SubHeader = subHeaderResource == string.Empty ? subHeaderResource : subHeaderResource.GetStatusCenterResource();
			SubHeaderStringResource = subHeaderResource;
			FileSystemOperationReturnResult = status;
			Operation = operation;
			ProgressEventSource = new Progress<StatusCenterItemProgressModel>(ReportProgress);
			Progress = new(ProgressEventSource, status: FileSystemStatusCode.InProgress);
			IsCancelable = _operationCancellationToken is not null;
			TotalItemsCount = itemsCount;
			TotalSize = totalSize;
			IconBackgroundCircleBorderOpacity = 1;
			AnimatedIconState = "NormalOff";
			SpeedGraphValues = [];
			CancelCommand = new RelayCommand(ExecuteCancelCommand);
			Message = Strings.DiscoveringItems.GetStatusCenterResource();
			Source = source;
			Destination = destination;

			if (Operation is FileOperationType.Git)
			{
				Message = Strings.StatusCenter_GitOperationProgress.GetStatusCenterFormatResource(0);
				IsDiscovering = false;
			}

			// Get the graph color
			if (Application.Current.Resources["App.Theme.FillColorAttentionBrush"] is not SolidColorBrush accentBrush)
				return;

			// Set icon and initialize string resources
			switch (FileSystemOperationReturnResult)
			{
				case ReturnResult.InProgress:
					{
						IsSpeedAndProgressAvailable = canProvideProgress && Operation is not FileOperationType.Git;
						IsInProgress = true;
						IsIndeterminateProgress = !canProvideProgress;
						IconBackgroundCircleBorderOpacity = 0.1d;

						if (Operation is FileOperationType.Prepare)
							Header = "StatusCenter_PrepareInProgress".GetStatusCenterResource();

						ItemKind = StatusCenterItemKind.InProgress;
						ItemIconKind = Operation switch
						{
							FileOperationType.Extract => StatusCenterItemIconKind.Extract,
							FileOperationType.Copy => StatusCenterItemIconKind.Copy,
							FileOperationType.Move => StatusCenterItemIconKind.Move,
							FileOperationType.Delete => StatusCenterItemIconKind.Delete,
							FileOperationType.Recycle => StatusCenterItemIconKind.Recycle,
							FileOperationType.Compressed => StatusCenterItemIconKind.Compress,
							FileOperationType.Git => StatusCenterItemIconKind.Git,
							FileOperationType.InstallFont => StatusCenterItemIconKind.InstallFont,
							_ => StatusCenterItemIconKind.Delete,
						};

						break;
					}
				case ReturnResult.Success:
					{
						ItemKind = StatusCenterItemKind.Successful;
						ItemIconKind = StatusCenterItemIconKind.Successful;

						break;
					}
				case ReturnResult.Failed:
					{
						ItemKind = StatusCenterItemKind.Error;
						ItemIconKind = StatusCenterItemIconKind.Error;

						break;
					}
				case ReturnResult.Cancelled:
					{
						IconBackgroundCircleBorderOpacity = 0.1d;

						ItemKind = StatusCenterItemKind.Canceled;
						ItemIconKind = Operation switch
						{
							FileOperationType.Extract => StatusCenterItemIconKind.Extract,
							FileOperationType.Copy => StatusCenterItemIconKind.Copy,
							FileOperationType.Move => StatusCenterItemIconKind.Move,
							FileOperationType.Delete => StatusCenterItemIconKind.Delete,
							FileOperationType.Recycle => StatusCenterItemIconKind.Recycle,
							FileOperationType.Compressed => StatusCenterItemIconKind.Compress,
							FileOperationType.Git => StatusCenterItemIconKind.Git,
							FileOperationType.InstallFont => StatusCenterItemIconKind.InstallFont,
							_ => StatusCenterItemIconKind.Delete,
						};

						break;
					}
			}

			StatusCenterHelper.UpdateCardStrings(this);
			OnPropertyChanged(nameof(HeaderTooltip));
		}

		private void ReportProgress(StatusCenterItemProgressModel value)
		{
			// The operation has been canceled.
			// Do update neither progress value nor text.
			if (CancellationToken.IsCancellationRequested)
				return;

			// Update status code
			if (value.Status is FileSystemStatusCode status)
				FileSystemOperationReturnResult = status.ToStatus();

			// Update the footer message, percentage, processing item name
			if (value.Percentage is double p)
			{
				if (ProgressPercentage != value.Percentage)
				{
					ProgressPercentage = (int)p;

					if (Operation == FileOperationType.Git)
					{
						Message = Strings.StatusCenter_GitOperationProgress.GetStatusCenterFormatResource((int)p);
					}
					else if (Operation == FileOperationType.Recycle ||
						Operation == FileOperationType.Delete ||
						Operation == FileOperationType.Compressed)
					{
						Message = string.Format(
							Strings.StatusCenter_ProcessedItems_Header.GetStatusCenterFormatResource(value.ProcessedItemsCount, value.ItemsCount),
							value.ProcessedItemsCount,
							value.ItemsCount);
					}
					else
					{
						Message = string.Format(
							Strings.StatusCenter_ProcessedSize_Header.GetStatusCenterResource(),
							value.ProcessedSize.ToSizeString(),
							value.TotalSize.ToSizeString());
					}
				}

				if (CurrentProcessingItemName != value.FileName)
					CurrentProcessingItemName = value.FileName;
			}

			// Set total count
			if (TotalItemsCount < value.ItemsCount)
				TotalItemsCount = value.ItemsCount;

			// Set total size
			if (TotalSize < value.TotalSize)
				TotalSize = value.TotalSize;

			if (value.EnumerationCompleted && IsDiscovering)
			{
				IsDiscovering = false;
				Message = Strings.ProcessingItems.GetStatusCenterResource();
			}

			// Update UI for strings
			StatusCenterHelper.UpdateCardStrings(this, value);
			OnPropertyChanged(nameof(HeaderTooltip));

			if (Operation is FileOperationType.Git)
			{
				if (!IsIndeterminateProgress && value.EnumerationCompleted)
					Header = $"{Header} ({ProgressPercentage}%)";

				_viewModel.NotifyChanges();
				return;
			}

			// Graph item point
			Vector2 point;

			// Set speed text and percentage
			switch (value.TotalSize, value.ItemsCount)
			{
				// In progress, displaying items count & processed size
				case (not 0, not 0):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedSize * 100.0 / value.TotalSize), 0, 100);

					SpeedText = $"{value.ProcessingSizeSpeed.ToSizeString()}/s";

					point = new((float)(value.ProcessedSize * 100.0 / value.TotalSize), (float)value.ProcessingSizeSpeed);

					break;
				// In progress, displaying processed size
				case (not 0, _):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedSize * 100.0 / value.TotalSize), 0, 100);

					SpeedText = $"{value.ProcessingSizeSpeed.ToSizeString()}/s";

					point = new((float)(value.ProcessedSize * 100.0 / value.TotalSize), (float)value.ProcessingSizeSpeed);

					break;
				// In progress, displaying items count
				case (_, not 0):
					ProgressPercentage = Math.Clamp((int)(value.ProcessedItemsCount * 100.0 / value.ItemsCount), 0, 100);

					SpeedText = $"{value.ProcessingItemsCountSpeed:0} items/s";

					point = new((float)(value.ProcessedItemsCount * 100.0 / value.ItemsCount), (float)value.ProcessingItemsCountSpeed);

					break;
				default:
					point = new(ProgressPercentage, (float)value.ProcessingItemsCountSpeed);

					SpeedText = (value.ProcessedSize, value.ProcessedItemsCount) switch
					{
						(not 0, not 0) => $"{value.ProcessingSizeSpeed.ToSizeString()}/s",
						(not 0, _) => $"{value.ProcessingSizeSpeed.ToSizeString()}/s",
						(_, not 0) => $"{value.ProcessingItemsCountSpeed:0} items/s",
						_ => "N/A",
					};
					break;
			}

			// 'debounce' updates a bit so the graph isn't too noisy
			if (SpeedGraphValues.Count == 0 || (point.X - SpeedGraphValues[^1].X) > 0.5)
				SpeedGraphValues?.Add(point);

			// Add percentage to the header
			if (!IsIndeterminateProgress && value.EnumerationCompleted)
				Header = $"{Header} ({ProgressPercentage}%)";

			// Update UI of the address bar
			_viewModel.NotifyChanges();
		}

		public void ExecuteCancelCommand()
		{
			if (IsCancelable)
			{
				_operationCancellationToken?.Cancel();
				IsIndeterminateProgress = true;
				IsCancelable = false;
				IsExpanded = false;
				IsSpeedAndProgressAvailable = false;
				Header = $"{Strings.Canceling.GetStatusCenterResource()} - {Header}";
			}
		}
	}
}
