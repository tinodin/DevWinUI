using System.ComponentModel;

namespace DevWinUI;

public partial class PreviewItemsViewModel : INotifyPropertyChanged
{
    private ObservableCollection<object> _items = [];
    private object? _currentItem;
    private int _selectedIndex = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<object> Items
    {
        get => _items;
        set
        {
            if (!ReferenceEquals(_items, value))
            {
                _items = value;
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalIndex));
            }
        }
    }

    public object? CurrentItem
    {
        get => _currentItem;
        set
        {
            if (!ReferenceEquals(_currentItem, value))
            {
                _currentItem = value;
                OnPropertyChanged(nameof(CurrentItem));
            }
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex == value)
            {
                return;
            }

            int oldIndex = _selectedIndex;
            _selectedIndex = value;

            if (_items != null && value >= 0 && value < _items.Count)
            {
                CurrentItem = _items[value];
            }

            OnPropertyChanged(nameof(SelectedIndex));
            OnPropertyChanged(nameof(Caption));
            OnPropertyChanged(nameof(CurrentIndex));
            OnPropertyChanged(nameof(TotalIndex));

            if (_items != null && oldIndex >= 0 && oldIndex < _items.Count)
            {
                if (_items[oldIndex] is VideoPlayerSource oldVideo)
                {
                    try
                    {
                        if (oldVideo.MediaPlayer?.PlaybackSession?.CanPause == true)
                        {
                            oldVideo.MediaPlayer.Pause();
                        }
                    }
                    catch { }
                }
            }
        }
    }

    public string Caption
    {
        get
        {
            if (CurrentItem is ScreenshotTileItem screenshot)
            {
                return screenshot.Title ?? string.Empty;
            }
            if (CurrentItem is VideoPlayerSource video)
            {
                return video.Title ?? string.Empty;
            }
            return string.Empty;
        }
    }

    public string CurrentIndex => SelectedIndex >= 0 ? (SelectedIndex + 1).ToString() : "1";

    public string TotalIndex => Items != null ? Items.Count.ToString() : "0";

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
