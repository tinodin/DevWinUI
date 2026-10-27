using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Storage;

namespace DevWinUIGallery.Views;

public sealed partial class StatusCenterPage : Page
{
    private const string SourceRoot = @"C:\Users\Public\Documents\StatusCenterDemo";
    private const string DestinationRoot = @"C:\Users\Public\Downloads";
    private const string RepositoryUrl = "https://github.com/files-community/Files";
    private const long Megabyte = 1024 * 1024;
    private const double TimerIntervalSeconds = 0.1;

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

    private void CmbFlyoutPlacement_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbFlyoutPlacement.SelectedItem is ComboBoxItem { Tag: string placement } &&
            Enum.TryParse(placement, out FlyoutPlacementMode flyoutPlacement))
            StatusCenterButtonDemo.FlyoutPlacement = flyoutPlacement;
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

    private void CopyInProgress_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(3);
        var destination = Destinations(3);
        var totalSize = TotalSize(source);

        Start(StatusCenterHelper.AddCard_Copy(ReturnResult.InProgress, source, destination, source.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Copy(result, source, destination, source.Length, totalSize));
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(4);
        var destination = Destinations(4);
        var totalSize = TotalSize(source);

        Start(StatusCenterHelper.AddCard_Move(ReturnResult.InProgress, source, destination, source.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Move(result, source, destination, source.Length, totalSize));
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(2);
        var totalSize = TotalSize(source);

        Start(StatusCenterHelper.AddCard_Delete(ReturnResult.InProgress, source, source.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Delete(result, source, source.Length, totalSize));
    }

    private void Recycle_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(5);
        var totalSize = TotalSize(source);

        Start(StatusCenterHelper.AddCard_Recycle(ReturnResult.InProgress, source, source.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Recycle(result, source, source.Length, totalSize));
    }

    private void CompressInProgress_Click(object sender, RoutedEventArgs e)
    {
        var source = Items(3);
        var paths = source.Select(item => item.Path).ToArray();
        var totalSize = TotalSize(source);
        string[] destination = [$@"{DestinationRoot}\Archive.zip"];

        Start(StatusCenterHelper.AddCard_Compress(paths, destination, ReturnResult.InProgress, source.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Compress(paths, destination, result, source.Length, totalSize));
    }

    private void Extract_Click(object sender, RoutedEventArgs e)
    {
        const long totalSize = 512 * Megabyte;
        string[] source = [$@"{SourceRoot}\Archive.zip"];
        string[] destination = [$@"{DestinationRoot}\Archive"];

        Start(StatusCenterHelper.AddCard_Decompress(source, destination, ReturnResult.InProgress, 1, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_Decompress(source, destination, result, 1, totalSize));
    }

    private void InstallFont_Click(object sender, RoutedEventArgs e)
    {
        const long totalSize = 24 * Megabyte;
        string[] fonts = [$@"{SourceRoot}\Font1.ttf", $@"{SourceRoot}\Font2.ttf"];

        Start(StatusCenterHelper.AddCard_InstallFont(fonts, ReturnResult.InProgress, fonts.Length, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_InstallFont(fonts, result, fonts.Length, totalSize));
    }

    private void EmptyRecycleBin_Click(object sender, RoutedEventArgs e)
    {
        const int itemsCount = 128;
        const long totalSize = 512 * Megabyte;

        Start(StatusCenterHelper.AddCard_EmptyRecycleBin(ReturnResult.InProgress, itemsCount, totalSize), totalSize,
            result => StatusCenterHelper.AddCard_EmptyRecycleBin(result, itemsCount, totalSize));
    }

    private void GitClone_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Clone, RepositoryPath, ReturnResult.InProgress, true, RepositoryUrl), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Clone, RepositoryPath, result, false, RepositoryUrl));
    }

    private void GitFetch_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Fetch, RepositoryPath, ReturnResult.InProgress, true), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Fetch, RepositoryPath, result));
    }

    private void GitPull_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Pull, RepositoryPath, ReturnResult.InProgress, true), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Pull, RepositoryPath, result));
    }

    private void GitPush_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Push, RepositoryPath, ReturnResult.InProgress, true), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Push, RepositoryPath, result));
    }

    private void GitSync_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Sync, RepositoryPath, ReturnResult.InProgress, true), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Sync, RepositoryPath, result));
    }

    private void GitCheckout_Click(object sender, RoutedEventArgs e)
    {
        Start(StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Checkout, RepositoryPath, ReturnResult.InProgress, true, "main"), 0,
            result => StatusCenterHelper.AddCard_GitOperation(GitStatusCenterOperationKind.Checkout, RepositoryPath, result, false, "main"));
    }

    private void Start(StatusCenterItem item, long totalSize, Action<ReturnResult>? onResult)
    {
        item.Progress.TotalSize = totalSize;
        item.Progress.ItemsCount = item.TotalItemsCount;
        item.Progress.EnumerationCompleted = true;

        string[] fileNames = item.Operation is FileOperationType.Git
            ? []
            : item.Source?.Select(path => Path.GetFileName(path)).OfType<string>().ToArray() ?? [];

        if (fileNames.Length > 0)
            item.Progress.FileName = fileNames[0];

        _operations.Add(new DemoOperation(item, totalSize, fileNames, onResult));

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

            var percentage = operation.TotalSize > 0
                ? Advance(operation)
                : operation.Percentage + 4;

            operation.Percentage = Math.Min(percentage, 100);
            operation.Item.Progress.Report(operation.Percentage);

            if (operation.Percentage < 100)
                continue;

            _operations.RemoveAt(index);
            StatusCenterViewModel.Instance.RemoveItem(operation.Item);
            operation.OnResult?.Invoke(ReturnResult.Success);
        }

        if (_operations.Count is 0)
            _timer.Stop();
    }

    private double Advance(DemoOperation operation)
    {
        // Simulate a varying transfer speed (8-100 MB/s), like the SpeedGraph sample.
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

    private static string RepositoryPath => $@"{SourceRoot}\Files";

    private static DemoItem[] Items(int count) =>
        Enumerable.Range(0, count).Select(index => new DemoItem($"File {index + 1}.txt", $@"{SourceRoot}\File {index + 1}.txt", DemoSizes[index] * Megabyte)).ToArray();

    private static string[] Destinations(int count) =>
        Enumerable.Range(1, count).Select(index => $@"{DestinationRoot}\File {index}.txt").ToArray();

    private static long TotalSize(IEnumerable<DemoItem> items) => items.Sum(item => item.Size);

    private static readonly long[] DemoSizes = [192, 160, 128, 96, 64];

    private sealed class DemoOperation(StatusCenterItem item, long totalSize, string[] fileNames, Action<ReturnResult>? onResult)
    {
        public StatusCenterItem Item { get; } = item;

        public long TotalSize { get; } = totalSize;

        public string[] FileNames { get; } = fileNames;

        public Action<ReturnResult>? OnResult { get; } = onResult;

        public long ProcessedSize { get; set; }

        public int ProcessedItems { get; set; }

        public double Percentage { get; set; }
    }

    private sealed record DemoItem(string Name, string Path, long Size) : IStorageItemWithPath
    {
        public IStorageItem? Item => null;

        public FilesystemItemType ItemType => FilesystemItemType.File;
    }
}
