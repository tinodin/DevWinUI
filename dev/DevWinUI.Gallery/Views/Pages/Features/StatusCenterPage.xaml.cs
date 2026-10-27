using Microsoft.UI.Xaml.Controls.Primitives;

namespace DevWinUIGallery.Views;

public sealed partial class StatusCenterPage : Page
{
    private const string SourceRoot = @"C:\Users\Public\Documents";
    private const string DestinationRoot = @"C:\Users\Public\Downloads";
    private const string RepositoryUrl = "https://github.com/ghost1372/DevWinUI";
    private const string RepositoryPath = $@"{SourceRoot}\DevWinUI";
    private const long GitCloneObjectCount = 1024;
    private const long Megabyte = 1024 * 1024;
    private const double TimerIntervalSeconds = 0.1;
    private const int IndeterminateDurationTicks = 30;
    private const string DiscoveringItems = "Discovering items...";
    private const string ProcessingItems = "Processing items...";

    private static readonly long[] DemoSizes = [192, 160, 128, 96, 64];

    private readonly List<DemoOperation> _operations = [];
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly Random _random = new();

    public StatusCenterPage()
    {
        InitializeComponent();

        CmbFlyoutPlacement.SelectedIndex = 0;

        _timer.Tick += Timer_Tick;
        Unloaded += (_, _) => _timer.Stop();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        _timer.Stop();
        _operations.Clear();

        var viewModel = StatusCenterViewModel.Instance;

        for (var index = viewModel.StatusCenterItems.Count - 1; index >= 0; index--)
            viewModel.RemoveItem(viewModel.StatusCenterItems[index]);
    }

    private void CmbFlyoutPlacement_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFlyoutPlacement.SelectedItem is ComboBoxItem { Tag: string placement } &&
            Enum.TryParse(placement, out FlyoutPlacementMode flyoutPlacement))
            StatusCenterButtonDemo.FlyoutPlacement = flyoutPlacement;
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(3);
        var destination = Destinations(3);
        var totalSize = GetTotalSize(source.Length);

        Start(AddCardCopy(ReturnResult.InProgress, source, destination, source.Length, totalSize), totalSize,
            result => AddCardCopy(result, source, destination, source.Length, totalSize));
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(4);
        var destination = Destinations(4);
        var totalSize = GetTotalSize(source.Length);

        Start(AddCardMove(ReturnResult.InProgress, source, destination, source.Length, totalSize), totalSize,
            result => AddCardMove(result, source, destination, source.Length, totalSize));
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(2);
        var totalSize = GetTotalSize(source.Length);

        Start(AddCardDelete(ReturnResult.InProgress, source, source.Length, totalSize), totalSize,
            result => AddCardDelete(result, source, source.Length, totalSize));
    }

    private void Recycle_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(5);
        var totalSize = GetTotalSize(source.Length);

        Start(AddCardRecycle(ReturnResult.InProgress, source, source.Length, totalSize), totalSize,
            result => AddCardRecycle(result, source, source.Length, totalSize));
    }

    private void Compress_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(3);
        var totalSize = GetTotalSize(source.Length);
        string[] destination = [$@"{DestinationRoot}\Archive.zip"];

        Start(AddCardCompress(source, destination, ReturnResult.InProgress, source.Length, totalSize), totalSize,
            result => AddCardCompress(source, destination, result, source.Length, totalSize));
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        const long totalSize = 512 * Megabyte;
        string[] source = [$@"{SourceRoot}\Archive.zip"];
        string[] destination = [$@"{DestinationRoot}\Archive"];

        Start(AddCardDecompress(source, destination, ReturnResult.InProgress, 1, totalSize), totalSize,
            result => AddCardDecompress(source, destination, result, 1, totalSize));
    }

    private void InstallFont_Click(object sender, RoutedEventArgs e)
    {
        const int fontCount = 2;

        StartWithoutProgress(AddCardInstallFont([SourceRoot], ReturnResult.InProgress, fontCount, 0),
            result => AddCardInstallFont([SourceRoot], result, fontCount, 0));
    }

    private void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
    {
        StartWithoutProgress(AddCardEmptyRecycleBin(ReturnResult.InProgress, 0, 0),
            result => AddCardEmptyRecycleBin(result, 0, 0));
    }

    private void GitClone_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Clone, RepositoryUrl);

    private void GitFetch_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Fetch, null);

    private void GitPull_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Pull, null);

    private void GitPush_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Push, null);

    private void GitSync_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Sync, null);

    private void GitCheckout_Click(object sender, RoutedEventArgs e) => StartGit(GitOperationKind.Checkout, "main");

    private void StartGit(GitOperationKind operationKind, string? operationTarget)
    {
        var itemsCount = operationKind is GitOperationKind.Clone ? GitCloneObjectCount : 0;

        Start(AddCardGit(operationKind, RepositoryPath, ReturnResult.InProgress, true, operationTarget, itemsCount), 0,
            result => AddCardGit(operationKind, RepositoryPath, result, false, operationTarget, itemsCount), isGit: true);
    }

    private static StatusCenterItem AddCard(
        ReturnResult status,
        string header,
        string subHeader,
        string message,
        StatusCenterItemIconKind iconKind,
        string[]? source,
        string[]? destination,
        bool canProvideProgress,
        long itemsCount,
        long totalSize,
        bool cancellable,
        Func<StatusCenterItem, (string Header, string SubHeader, string Message)>? update)
    {
        var cancellationTokenSource = cancellable ? new CancellationTokenSource() : null;

        var item = StatusCenterViewModel.Instance.AddItem(
            header,
            subHeader,
            status,
            source,
            destination,
            canProvideProgress,
            itemsCount,
            totalSize,
            cancellationTokenSource);

        if (status is ReturnResult.InProgress or ReturnResult.Cancelled)
            item.ItemIconKind = iconKind;

        item.Message = message;

        if (update is not null)
        {
            item.Progress.PropertyChanged += (_, _) =>
            {
                var value = update(item);
                item.Header = value.Header;
                item.SubHeader = value.SubHeader;
                item.Message = value.Message;
            };
        }

        return item;
    }

    private static StatusCenterItem AddCardCopy(ReturnResult returnStatus, string[] source, string[] destination, long itemsCount, long totalSize)
    {
        var destinationDirName = GetLastSegment(GetParentDirectory(destination.FirstOrDefault()));
        var sourcePath = GetParentDirectory(source.FirstOrDefault());
        var destinationPath = GetParentDirectory(destination.FirstOrDefault());

        return AddCard(
            returnStatus,
            CopyHeader(returnStatus, itemsCount, destinationDirName),
            CopySubHeader(returnStatus, itemsCount, sourcePath, destinationPath),
            DiscoveringItems,
            StatusCenterItemIconKind.Copy,
            source,
            destination,
            true,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (CopyHeader(returnStatus, item.TotalItemsCount, destinationDirName),
                CopySubHeader(returnStatus, item.TotalItemsCount, sourcePath, destinationPath),
                SizeMessage(item)));
    }

    private static StatusCenterItem AddCardMove(ReturnResult returnStatus, string[] source, string[] destination, long itemsCount, long totalSize)
    {
        var destinationDirName = GetLastSegment(GetParentDirectory(destination.FirstOrDefault()));
        var sourcePath = GetParentDirectory(source.FirstOrDefault());
        var destinationPath = GetParentDirectory(destination.FirstOrDefault());

        return AddCard(
            returnStatus,
            MoveHeader(returnStatus, itemsCount, destinationDirName),
            MoveSubHeader(returnStatus, itemsCount, sourcePath, destinationPath),
            DiscoveringItems,
            StatusCenterItemIconKind.Move,
            source,
            destination,
            true,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (MoveHeader(returnStatus, item.TotalItemsCount, destinationDirName),
                MoveSubHeader(returnStatus, item.TotalItemsCount, sourcePath, destinationPath),
                SizeMessage(item)));
    }

    private static StatusCenterItem AddCardDelete(ReturnResult returnStatus, string[]? source, long itemsCount, long totalSize)
    {
        var sourcePath = GetParentDirectory(source?.FirstOrDefault());
        var sourceDirName = GetLastSegment(sourcePath);

        return AddCard(
            returnStatus,
            DeleteHeader(returnStatus, itemsCount, sourceDirName),
            returnStatus is ReturnResult.Failed ? DeleteFailedSubHeader(itemsCount, sourcePath) : string.Empty,
            DiscoveringItems,
            StatusCenterItemIconKind.Delete,
            source,
            null,
            true,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (DeleteHeader(returnStatus, item.TotalItemsCount, sourceDirName),
                returnStatus is ReturnResult.Failed ? DeleteFailedSubHeader(item.TotalItemsCount, sourcePath) : string.Empty,
                ItemsMessage(item)));
    }

    private static StatusCenterItem AddCardRecycle(ReturnResult returnStatus, string[]? source, long itemsCount, long totalSize)
    {
        var sourcePath = GetParentDirectory(source?.FirstOrDefault());
        var sourceDirName = GetLastSegment(sourcePath);

        return AddCard(
            returnStatus,
            DeleteHeader(returnStatus, itemsCount, sourceDirName),
            string.Empty,
            DiscoveringItems,
            StatusCenterItemIconKind.Recycle,
            source,
            null,
            true,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (DeleteHeader(returnStatus, item.TotalItemsCount, sourceDirName), string.Empty, ItemsMessage(item)));
    }

    private static StatusCenterItem AddCardCompress(string[] source, string[] destination, ReturnResult returnStatus, long itemsCount, long totalSize)
    {
        var destinationDirName = GetLastSegment(GetParentDirectory(destination.FirstOrDefault()));
        var sourcePath = GetParentDirectory(source.FirstOrDefault());
        var destinationPath = GetParentDirectory(destination.FirstOrDefault());

        return AddCard(
            returnStatus,
            CompressHeader(returnStatus, itemsCount, destinationDirName),
            CompressSubHeader(returnStatus, itemsCount, sourcePath, destinationPath),
            DiscoveringItems,
            StatusCenterItemIconKind.Compress,
            source,
            destination,
            returnStatus is ReturnResult.InProgress,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (CompressHeader(returnStatus, item.TotalItemsCount, destinationDirName),
                CompressSubHeader(returnStatus, item.TotalItemsCount, sourcePath, destinationPath),
                ItemsMessage(item)));
    }

    private static StatusCenterItem AddCardDecompress(string[] source, string[] destination, ReturnResult returnStatus, long itemsCount, long totalSize)
    {
        var sourceFileName = GetLastSegment(source.FirstOrDefault());
        var destinationDirName = GetLastSegment(GetParentDirectory(destination.FirstOrDefault()));
        var sourcePath = GetParentDirectory(source.FirstOrDefault());
        var destinationPath = GetParentDirectory(destination.FirstOrDefault());

        return AddCard(
            returnStatus,
            DecompressHeader(returnStatus, sourceFileName, destinationDirName),
            DecompressSubHeader(returnStatus, sourceFileName, sourcePath, destinationPath),
            DiscoveringItems,
            StatusCenterItemIconKind.Extract,
            source,
            destination,
            returnStatus is ReturnResult.InProgress,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (DecompressHeader(returnStatus, sourceFileName, destinationDirName),
                DecompressSubHeader(returnStatus, sourceFileName, sourcePath, destinationPath),
                SizeMessage(item)));
    }

    private static StatusCenterItem AddCardInstallFont(string[] source, ReturnResult returnStatus, long itemsCount, long totalSize)
    {
        var sourcePath = GetParentDirectory(source.FirstOrDefault());

        return AddCard(
            returnStatus,
            InstallFontHeader(returnStatus, itemsCount),
            InstallFontSubHeader(returnStatus, itemsCount, sourcePath),
            DiscoveringItems,
            StatusCenterItemIconKind.InstallFont,
            source,
            [string.Empty],
            false,
            itemsCount,
            totalSize,
            false,
            item => (InstallFontHeader(returnStatus, item.TotalItemsCount),
                InstallFontSubHeader(returnStatus, item.TotalItemsCount, sourcePath),
                SizeMessage(item)));
    }

    private static StatusCenterItem AddCardEmptyRecycleBin(ReturnResult returnStatus, long itemsCount, long totalSize)
    {
        return AddCard(
            returnStatus,
            EmptyRecycleBinHeader(returnStatus),
            returnStatus is ReturnResult.Failed ? "Failed to empty Recycle Bin" : string.Empty,
            DiscoveringItems,
            StatusCenterItemIconKind.Delete,
            null,
            null,
            false,
            itemsCount,
            totalSize,
            returnStatus is ReturnResult.InProgress,
            item => (EmptyRecycleBinHeader(returnStatus),
                returnStatus is ReturnResult.Failed ? "Failed to empty Recycle Bin" : string.Empty,
                ItemsMessage(item)));
    }

    private static StatusCenterItem AddCardGit(GitOperationKind operationKind, string repositoryPath, ReturnResult returnStatus, bool canProvideProgress, string? operationTarget, long itemsCount)
    {
        var isClone = operationKind is GitOperationKind.Clone;
        var displayPath = isClone ? GetParentDirectory(repositoryPath) : repositoryPath;
        var repositoryName = Path.GetFileName(displayPath.TrimEnd('\\', '/'));

        if (string.IsNullOrEmpty(repositoryName))
            repositoryName = displayPath;

        var subHeader = isClone
            ? GitCloneSubHeader(returnStatus, itemsCount, operationTarget ?? string.Empty, displayPath)
            : repositoryPath;

        var item = AddCard(
            returnStatus,
            GitHeader(operationKind, returnStatus, operationTarget, repositoryName),
            subHeader,
            "0% complete",
            StatusCenterItemIconKind.Git,
            operationTarget is null ? null : [operationTarget],
            [repositoryPath],
            returnStatus is ReturnResult.InProgress && canProvideProgress,
            itemsCount,
            0,
            returnStatus is ReturnResult.InProgress && operationKind is GitOperationKind.Clone,
            item => (GitHeader(operationKind, returnStatus, operationTarget, repositoryName),
                isClone
                    ? GitCloneSubHeader(returnStatus, item.TotalItemsCount, operationTarget ?? string.Empty, displayPath)
                    : subHeader,
                GitMessage(item)));

        if (returnStatus is ReturnResult.InProgress)
            item.IsSpeedAndProgressAvailable = false;

        return item;
    }

    private static string CopyHeader(ReturnResult status, long count, string destinationDirName)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled copying {ItemsText(count)} to \"{destinationDirName}\"",
            ReturnResult.Success => $"Copied {ItemsText(count)} to \"{destinationDirName}\"",
            ReturnResult.Failed => $"Error copying {ItemsText(count)} to \"{destinationDirName}\"",
            _ => $"Copying {ItemsText(count)} to \"{destinationDirName}\""
        };
    }

    private static string CopySubHeader(ReturnResult status, long count, string sourcePath, string destinationPath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled copying {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Success => $"Copied {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Failed => $"Failed to copy {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            _ => $"Copying {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\""
        };
    }

    private static string MoveHeader(ReturnResult status, long count, string destinationDirName)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled moving {ItemsText(count)} to \"{destinationDirName}\"",
            ReturnResult.Success => $"Moved {ItemsText(count)} to \"{destinationDirName}\"",
            ReturnResult.Failed => $"Error moving {ItemsText(count)} to \"{destinationDirName}\"",
            _ => $"Moving {ItemsText(count)} to \"{destinationDirName}\""
        };
    }

    private static string MoveSubHeader(ReturnResult status, long count, string sourcePath, string destinationPath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled moving {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Success => $"Moved {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Failed => $"Failed to move {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            _ => $"Moving {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\""
        };
    }

    private static string DeleteHeader(ReturnResult status, long count, string sourceDirName)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled deleting {ItemsText(count)} from \"{sourceDirName}\"",
            ReturnResult.Success => $"Deleted {ItemsText(count)} from \"{sourceDirName}\"",
            ReturnResult.Failed => $"Error deleting {ItemsText(count)} from \"{sourceDirName}\"",
            _ => $"Deleting {ItemsText(count)} from \"{sourceDirName}\""
        };
    }

    private static string DeleteFailedSubHeader(long count, string sourcePath)
        => $"Failed to delete {ItemsText(count)} from \"{sourcePath}\"";

    private static string CompressHeader(ReturnResult status, long count, string destinationDirName)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled compressing {count} items to \"{destinationDirName}\"",
            ReturnResult.Success => $"Compressed {ItemsText(count)} to \"{destinationDirName}\"",
            ReturnResult.Failed => $"Error compressing {ItemsText(count)} to \"{destinationDirName}\"",
            _ => $"Compressing {ItemsText(count)} to \"{destinationDirName}\""
        };
    }

    private static string CompressSubHeader(ReturnResult status, long count, string sourcePath, string destinationPath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled compressing {count} items from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Success => $"Compressed {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Failed => $"Failed to compress {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\"",
            _ => $"Compressing {ItemsText(count)} from \"{sourcePath}\" to \"{destinationPath}\""
        };
    }

    private static string DecompressHeader(ReturnResult status, string sourceFileName, string destinationDirName)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled extracting \"{sourceFileName}\" to \"{destinationDirName}\"",
            ReturnResult.Success => $"Extracted \"{sourceFileName}\" to \"{destinationDirName}\"",
            ReturnResult.Failed => $"Error extracting \"{sourceFileName}\" to \"{destinationDirName}\"",
            _ => $"Extracting \"{sourceFileName}\" to \"{destinationDirName}\""
        };
    }

    private static string DecompressSubHeader(ReturnResult status, string sourceFileName, string sourcePath, string destinationPath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled extracting \"{sourceFileName}\" from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Success => $"Extracted \"{sourceFileName}\" from \"{sourcePath}\" to \"{destinationPath}\"",
            ReturnResult.Failed => $"Failed to extract \"{sourceFileName}\" from \"{sourcePath}\" to \"{destinationPath}\"",
            _ => $"Extracting \"{sourceFileName}\" from \"{sourcePath}\" to \"{destinationPath}\""
        };
    }

    private static string InstallFontHeader(ReturnResult status, long count)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled installing {count} fonts",
            ReturnResult.Success => $"Installed {FontsText(count)}",
            ReturnResult.Failed => $"Error installing {FontsText(count)}",
            _ => $"Installing {FontsText(count)}"
        };
    }

    private static string InstallFontSubHeader(ReturnResult status, long count, string sourcePath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled installing {FontsText(count)} from \"{sourcePath}\"",
            ReturnResult.Success => $"Installed {FontsText(count)} from \"{sourcePath}\"",
            ReturnResult.Failed => $"Failed to install {FontsText(count)} from \"{sourcePath}\"",
            _ => $"Installing {FontsText(count)} from \"{sourcePath}\""
        };
    }

    private static string EmptyRecycleBinHeader(ReturnResult status)
    {
        return status switch
        {
            ReturnResult.Cancelled => "Canceled emptying Recycle Bin",
            ReturnResult.Success => "Emptied Recycle Bin",
            ReturnResult.Failed => "Error emptying Recycle Bin",
            _ => "Emptying Recycle Bin"
        };
    }

    private static string GitHeader(GitOperationKind operationKind, ReturnResult status, string? operationTarget, string repositoryName)
    {
        return operationKind switch
        {
            GitOperationKind.Clone => status switch
            {
                ReturnResult.Cancelled => $"Canceled cloning {operationTarget} to \"{repositoryName}\"",
                ReturnResult.Success => $"Cloned \"{operationTarget}\" to \"{repositoryName}\"",
                ReturnResult.Failed => $"Error cloning \"{operationTarget}\" to \"{repositoryName}\"",
                _ => $"Cloning \"{operationTarget}\" to \"{repositoryName}\""
            },
            GitOperationKind.Fetch => status switch
            {
                ReturnResult.Cancelled => $"Canceled fetching remote changes for \"{repositoryName}\"",
                ReturnResult.Success => $"Fetched remote changes for \"{repositoryName}\"",
                ReturnResult.Failed => $"Failed to fetch remote changes for \"{repositoryName}\"",
                _ => $"Fetching remote changes for \"{repositoryName}\""
            },
            GitOperationKind.Pull => status switch
            {
                ReturnResult.Cancelled => $"Canceled pulling remote changes into \"{repositoryName}\"",
                ReturnResult.Success => $"Pulled remote changes into \"{repositoryName}\"",
                ReturnResult.Failed => $"Failed to pull remote changes into \"{repositoryName}\"",
                _ => $"Pulling remote changes into \"{repositoryName}\""
            },
            GitOperationKind.Push => status switch
            {
                ReturnResult.Cancelled => $"Canceled pushing local commits from \"{repositoryName}\"",
                ReturnResult.Success => $"Pushed local commits from \"{repositoryName}\"",
                ReturnResult.Failed => $"Failed to push local commits from \"{repositoryName}\"",
                _ => $"Pushing local commits from \"{repositoryName}\""
            },
            GitOperationKind.Sync => status switch
            {
                ReturnResult.Cancelled => $"Canceled syncing local and remote commits for \"{repositoryName}\"",
                ReturnResult.Success => $"Synced local and remote commits for \"{repositoryName}\"",
                ReturnResult.Failed => $"Failed to sync local and remote commits for \"{repositoryName}\"",
                _ => $"Syncing local and remote commits for \"{repositoryName}\""
            },
            _ => status switch
            {
                ReturnResult.Cancelled => $"Canceled switching to \"{operationTarget}\" in \"{repositoryName}\"",
                ReturnResult.Success => $"Switched to \"{operationTarget}\" in \"{repositoryName}\"",
                ReturnResult.Failed => $"Failed to switch to \"{operationTarget}\" in \"{repositoryName}\"",
                _ => $"Switching to \"{operationTarget}\" in \"{repositoryName}\""
            }
        };
    }

    private static string GitCloneSubHeader(ReturnResult status, long count, string operationTarget, string displayPath)
    {
        return status switch
        {
            ReturnResult.Cancelled => $"Canceled cloning {count} from \"{operationTarget}\" to \"{displayPath}\"",
            ReturnResult.Success => $"Cloned {count} from \"{operationTarget}\" to \"{displayPath}\"",
            ReturnResult.Failed => $"Failed to clone {count} from \"{operationTarget}\" to \"{displayPath}\"",
            _ => $"Cloning {count} from \"{operationTarget}\" to \"{displayPath}\""
        };
    }

    private static string SizeMessage(StatusCenterItem item)
        => $"{ToSizeString(item.Progress.ProcessedSize)} of {ToSizeString(item.Progress.TotalSize)} processed";

    private static string ItemsMessage(StatusCenterItem item)
        => $"{item.Progress.ProcessedItemsCount}/{ItemsText(item.Progress.ItemsCount)} processed";

    private static string GitMessage(StatusCenterItem item)
        => $"{(int)(item.Progress.Percentage ?? 0)}% complete";

    private void Start(StatusCenterItem item, long totalSize, Action<ReturnResult>? onResult, bool isGit = false)
    {
        item.Progress.TotalSize = totalSize;
        item.Progress.ItemsCount = item.TotalItemsCount;
        item.Progress.EnumerationCompleted = isGit;

        string[] fileNames = totalSize > 0
            ? item.Source?.Select(path => Path.GetFileName(path)).OfType<string>().ToArray() ?? []
            : [];

        if (fileNames.Length > 0)
            item.Progress.FileName = fileNames[0];

        _operations.Add(new DemoOperation(item, totalSize, fileNames, onResult, isGit: isGit));

        if (!_timer.IsEnabled)
            _timer.Start();
    }

    private void StartWithoutProgress(StatusCenterItem item, Action<ReturnResult>? onResult, int durationTicks = IndeterminateDurationTicks)
    {
        _operations.Add(new DemoOperation(item, 0, [], onResult, false, durationTicks));

        if (!_timer.IsEnabled)
            _timer.Start();
    }

    private void Timer_Tick(object? sender, object e)
    {
        for (var index = _operations.Count - 1; index >= 0; index--)
        {
            var operation = _operations[index];

            if (operation.Item.CancellationToken.IsCancellationRequested)
            {
                _operations.RemoveAt(index);
                StatusCenterViewModel.Instance.RemoveItem(operation.Item);
                operation.OnResult?.Invoke(ReturnResult.Cancelled);
                continue;
            }

            if (operation.ReportsProgress)
            {
                var percentage = operation.TotalSize > 0
                    ? Advance(operation)
                    : operation.Percentage + 4;

                operation.Percentage = Math.Min(percentage, 100);

                if (!operation.IsGit && !operation.Item.Progress.EnumerationCompleted)
                    operation.Item.Progress.EnumerationCompleted = true;

                operation.Item.Progress.Report(operation.IsGit ? Math.Min(operation.Percentage, 99) : operation.Percentage);

                if (operation.IsDiscovering)
                {
                    operation.IsDiscovering = false;
                    operation.Item.Message = ProcessingItems;
                }

                if (operation.Percentage < 100)
                    continue;
            }
            else if (++operation.ElapsedTicks < operation.DurationTicks)
            {
                continue;
            }

            _operations.RemoveAt(index);

            StatusCenterViewModel.Instance.RemoveItem(operation.Item);
            operation.OnResult?.Invoke(ReturnResult.Success);
        }

        if (_operations.Count is 0)
            _timer.Stop();
    }

    private double Advance(DemoOperation operation)
    {
        var speed = _random.Next(8, 100) * (double)Megabyte;

        operation.ProcessedSize = Math.Min(operation.TotalSize, operation.ProcessedSize + (long)(speed * TimerIntervalSeconds));

        var percentage = operation.ProcessedSize * 100.0 / operation.TotalSize;

        if (operation.FileNames.Length > 0)
        {
            var processedItems = Math.Min(operation.FileNames.Length, (int)(percentage * operation.FileNames.Length / 100));
            var fileName = operation.FileNames[Math.Min(operation.FileNames.Length - 1, processedItems)];

            if (operation.Item.Progress.FileName != fileName)
                operation.Item.Progress.FileName = fileName;

            if (processedItems > operation.ProcessedItems)
            {
                operation.Item.Progress.AddProcessedItemsCount(processedItems - operation.ProcessedItems);
                operation.ProcessedItems = processedItems;
            }
        }

        return percentage;
    }

    private static string[] Items(int count) =>
        Enumerable.Range(1, count).Select(index => $@"{SourceRoot}\File {index}.txt").ToArray();

    private static string[] Destinations(int count) =>
        Enumerable.Range(1, count).Select(index => $@"{DestinationRoot}\File {index}.txt").ToArray();

    private static long GetTotalSize(int count) => DemoSizes.Take(count).Sum() * Megabyte;

    private static string GetParentDirectory(string? path)
        => string.IsNullOrEmpty(path) ? string.Empty : Path.GetDirectoryName(path) ?? string.Empty;

    private static string GetLastSegment(string path)
        => string.IsNullOrEmpty(path) ? string.Empty : path.Split('\\', '/')[^1];

    private static string ItemsText(long count) => count == 1 ? "1 item" : $"{count} items";

    private static string FontsText(long count) => count == 1 ? "1 font" : $"{count} fonts";

    private static string ToSizeString(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = size;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit is 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    private enum GitOperationKind
    {
        Clone,
        Fetch,
        Pull,
        Push,
        Sync,
        Checkout
    }

    private sealed class DemoOperation(StatusCenterItem item, long totalSize, string[] fileNames, Action<ReturnResult>? onResult, bool reportsProgress = true, int durationTicks = IndeterminateDurationTicks, bool isGit = false)
    {
        public StatusCenterItem Item { get; } = item;

        public long TotalSize { get; } = totalSize;

        public string[] FileNames { get; } = fileNames;

        public Action<ReturnResult>? OnResult { get; } = onResult;

        public bool ReportsProgress { get; } = reportsProgress;

        public bool IsGit { get; } = isGit;

        public bool IsDiscovering { get; set; } = !isGit;

        public int DurationTicks { get; } = durationTicks;

        public int ElapsedTicks { get; set; }

        public long ProcessedSize { get; set; }

        public int ProcessedItems { get; set; }

        public double Percentage { get; set; }
    }
}
