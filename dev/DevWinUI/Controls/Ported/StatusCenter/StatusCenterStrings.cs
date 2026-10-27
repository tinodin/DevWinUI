// Copyright (c) Files Community
// Licensed under the MIT License.

namespace DevWinUI
{
	internal static class Strings
	{
		public const string Canceling = nameof(Canceling);
		public const string DiscoveringItems = nameof(DiscoveringItems);
		public const string ProcessingItems = nameof(ProcessingItems);
		public const string StatusCenter_GitOperationProgress = nameof(StatusCenter_GitOperationProgress);
		public const string StatusCenter_ProcessedItems_Header = nameof(StatusCenter_ProcessedItems_Header);
		public const string StatusCenter_ProcessedSize_Header = nameof(StatusCenter_ProcessedSize_Header);
	}

	internal static class StatusCenterStrings
	{
		private static readonly Dictionary<string, string> Resources = new(StringComparer.Ordinal)
		{
			["ByteSymbol"] = "B",
			["Cancel"] = "Cancel",
			["Canceling"] = "Canceling",
			["Clear"] = "Clear",
			["ClearCompleted"] = "Clear completed",
			["DiscoveringItems"] = "Discovering items...",
			["GigaByteSymbol"] = "GB",
			["ItemSizeBytes"] = "bytes",
			["KiloByteSymbol"] = "KB",
			["MegaByteSymbol"] = "MB",
			["MoreOptions"] = "More options...",
			["NameWithColon"] = "Name:",
			["NoFileOperations"] = "No ongoing file operations",
			["PetaByteSymbol"] = "PB",
			["ProcessingItems"] = "Processing items...",
			["SpeedWithColon"] = "Speed:",
			["StatusCenter"] = "Status center",
			["StatusCenter_CompressCanceled_Header"] = "Canceled compressing {0} items to \"{1}\"",
			["StatusCenter_CompressCanceled_SubHeader"] = "Canceled compressing {0} items from \"{1}\" to \"{2}\"",
			["StatusCenter_CompressComplete_Header"] = "Compressed {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CompressComplete_SubHeader"] = "Compressed {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CompressFailed_Header"] = "Error compressing {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CompressFailed_SubHeader"] = "Failed to compress {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CompressInProgress_Header"] = "Compressing {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CompressInProgress_SubHeader"] = "Compressing {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CopyCanceled_Header"] = "Canceled copying {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CopyCanceled_SubHeader"] = "Canceled copying {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CopyComplete_Header"] = "Copied {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CopyComplete_SubHeader"] = "Copied {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CopyDiscovery_Header"] = "Discovered {0, plural, one {# item} other {# items}}",
			["StatusCenter_CopyFailed_Header"] = "Error copying {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CopyFailed_SubHeader"] = "Failed to copy {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_CopyInProgress_Header"] = "Copying {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_CopyInProgress_SubHeader"] = "Copying {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_DecompressCanceled_Header"] = "Canceled extracting \"{0}\" to \"{1}\"",
			["StatusCenter_DecompressCanceled_SubHeader"] = "Canceled extracting \"{0}\" from \"{1}\" to \"{2}\"",
			["StatusCenter_DecompressComplete_Header"] = "Extracted \"{0}\" to \"{1}\"",
			["StatusCenter_DecompressComplete_SubHeader"] = "Extracted \"{0}\" from \"{1}\" to \"{2}\"",
			["StatusCenter_DecompressFailed_Header"] = "Error extracting \"{0}\" to \"{1}\"",
			["StatusCenter_DecompressFailed_SubHeader"] = "Failed to extract \"{0}\" from \"{1}\" to \"{2}\"",
			["StatusCenter_DecompressInProgress_Header"] = "Extracting \"{0}\" to \"{1}\"",
			["StatusCenter_DecompressInProgress_SubHeader"] = "Extracting \"{0}\" from \"{1}\" to \"{2}\"",
			["StatusCenter_DeleteCanceled_Header"] = "Canceled deleting {0, plural, one {# item} other {# items}} from \"{1}\"",
			["StatusCenter_DeleteComplete_Header"] = "Deleted {0, plural, one {# item} other {# items}} from \"{1}\"",
			["StatusCenter_DeleteFailed_Header"] = "Error deleting {0, plural, one {# item} other {# items}} from \"{1}\"",
			["StatusCenter_DeleteFailed_SubHeader"] = "Failed to delete {0, plural, one {# item} other {# items}} from \"{1}\"",
			["StatusCenter_DeleteInProgress_Header"] = "Deleting {0, plural, one {# item} other {# items}} from \"{1}\"",
			["StatusCenter_EmptyRecycleBinCancel_Header"] = "Canceled emptying Recycle Bin",
			["StatusCenter_EmptyRecycleBinComplete_Header"] = "Emptied Recycle Bin",
			["StatusCenter_EmptyRecycleBinFailed_Header"] = "Error emptying Recycle Bin",
			["StatusCenter_EmptyRecycleBinFailed_SubHeader"] = "Failed to empty Recycle Bin",
			["StatusCenter_EmptyRecycleBinInProgress_Header"] = "Emptying Recycle Bin",
			["StatusCenter_GitCheckoutCanceled_Header"] = "Canceled switching to \"{0}\" in \"{1}\"",
			["StatusCenter_GitCheckoutComplete_Header"] = "Switched to \"{0}\" in \"{1}\"",
			["StatusCenter_GitCheckoutFailed_Header"] = "Failed to switch to \"{0}\" in \"{1}\"",
			["StatusCenter_GitCheckoutInProgress_Header"] = "Switching to \"{0}\" in \"{1}\"",
			["StatusCenter_GitCloneCanceled_Header"] = "Canceled cloning {0} to \"{1}\"",
			["StatusCenter_GitCloneCanceled_SubHeader"] = "Canceled cloning {0} from \"{1}\" to \"{2}\"",
			["StatusCenter_GitCloneComplete_Header"] = "Cloned \"{0}\" to \"{1}\"",
			["StatusCenter_GitCloneComplete_SubHeader"] = "Cloned {0} from \"{1}\" to \"{2}\"",
			["StatusCenter_GitCloneFailed_Header"] = "Error cloning \"{0}\" to \"{1}\"",
			["StatusCenter_GitCloneFailed_SubHeader"] = "Failed to clone {0} from \"{1}\" to \"{2}\"",
			["StatusCenter_GitCloneInProgress_Header"] = "Cloning \"{0}\" to \"{1}\"",
			["StatusCenter_GitCloneInProgress_SubHeader"] = "Cloning {0} from \"{1}\" to \"{2}\"",
			["StatusCenter_GitFetchCanceled_Header"] = "Canceled fetching remote changes for \"{0}\"",
			["StatusCenter_GitFetchComplete_Header"] = "Fetched remote changes for \"{0}\"",
			["StatusCenter_GitFetchFailed_Header"] = "Failed to fetch remote changes for \"{0}\"",
			["StatusCenter_GitFetchInProgress_Header"] = "Fetching remote changes for \"{0}\"",
			["StatusCenter_GitOperationProgress"] = "{0}% complete",
			["StatusCenter_GitPullCanceled_Header"] = "Canceled pulling remote changes into \"{0}\"",
			["StatusCenter_GitPullComplete_Header"] = "Pulled remote changes into \"{0}\"",
			["StatusCenter_GitPullFailed_Header"] = "Failed to pull remote changes into \"{0}\"",
			["StatusCenter_GitPullInProgress_Header"] = "Pulling remote changes into \"{0}\"",
			["StatusCenter_GitPushCanceled_Header"] = "Canceled pushing local commits from \"{0}\"",
			["StatusCenter_GitPushComplete_Header"] = "Pushed local commits from \"{0}\"",
			["StatusCenter_GitPushFailed_Header"] = "Failed to push local commits from \"{0}\"",
			["StatusCenter_GitPushInProgress_Header"] = "Pushing local commits from \"{0}\"",
			["StatusCenter_GitSyncCanceled_Header"] = "Canceled syncing local and remote commits for \"{0}\"",
			["StatusCenter_GitSyncComplete_Header"] = "Synced local and remote commits for \"{0}\"",
			["StatusCenter_GitSyncFailed_Header"] = "Failed to sync local and remote commits for \"{0}\"",
			["StatusCenter_GitSyncInProgress_Header"] = "Syncing local and remote commits for \"{0}\"",
			["StatusCenter_InstallFontCanceled_Header"] = "Canceled installing {0} fonts",
			["StatusCenter_InstallFontCanceled_SubHeader"] = "Canceled installing {0, plural, one {# font} other {# fonts}} from \"{1}\"",
			["StatusCenter_InstallFontComplete_Header"] = "Installed {0, plural, one {# font} other {# fonts}}",
			["StatusCenter_InstallFontComplete_SubHeader"] = "Installed {0, plural, one {# font} other {# fonts}} from \"{1}\"",
			["StatusCenter_InstallFontFailed_Header"] = "Error installing {0, plural, one {# font} other {# fonts}}",
			["StatusCenter_InstallFontFailed_SubHeader"] = "Failed to install {0, plural, one {# font} other {# fonts}} from \"{1}\"",
			["StatusCenter_InstallFontInProgress_Header"] = "Installing {0, plural, one {# font} other {# fonts}}",
			["StatusCenter_InstallFontInProgress_SubHeader"] = "Installing {0, plural, one {# font} other {# fonts}} from \"{1}\"",
			["StatusCenter_MoveCanceled_Header"] = "Canceled moving {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_MoveCanceled_SubHeader"] = "Canceled moving {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_MoveComplete_Header"] = "Moved {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_MoveComplete_SubHeader"] = "Moved {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_MoveFailed_Header"] = "Error moving {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_MoveFailed_SubHeader"] = "Failed to move {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_MoveInProgress_Header"] = "Moving {0, plural, one {# item} other {# items}} to \"{1}\"",
			["StatusCenter_MoveInProgress_SubHeader"] = "Moving {0, plural, one {# item} other {# items}} from \"{1}\" to \"{2}\"",
			["StatusCenter_Prepare_Header"] = "Preparing the operation...",
			["StatusCenter_ProcessedItems_Header"] = "{0}/{1, plural, one {# item} other {# items}} processed",
			["StatusCenter_ProcessedSize_Header"] = "{0} of {1} processed",
			["StatusCenterProgressRing"] = "Status center progress ring",
			["TeraByteSymbol"] = "TB",
		};

		public static string Get(string resourceKey) => Resources.TryGetValue(resourceKey, out var value) ? value : string.Empty;

		public static string Format(string resourceKey, object[] values) => StatusCenterMessageFormatter.Format(resourceKey, values);
	}
}
