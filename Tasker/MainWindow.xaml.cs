using System.Collections.ObjectModel;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Data;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MessageBox = System.Windows.MessageBox;
using CheckBox = System.Windows.Controls.CheckBox;
using WpfPoint = System.Windows.Point;

namespace Tasker;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TodoItem> _todos;
    private readonly ICollectionView _todoView;
    private bool _usesLiveGrouping;
    private ActiveTaskWidget? _activeTaskWidget;
    private AppSettings _settings;
    private Forms.NotifyIcon? _trayIcon;
    private System.Drawing.Icon? _applicationIcon;
    private HwndSource? _windowSource;
    private bool _exitRequested;

    private const int WmSettingChange = 0x001A;
    private const int WmThemeChanged = 0x031A;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    public MainWindow()
    {
        InitializeComponent();
        InitializeTrayIcon();
        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;
        Closed += (_, _) =>
        {
            _windowSource?.RemoveHook(WindowMessageHook);
            _trayIcon?.Dispose();
            _applicationIcon?.Dispose();
        };
        
        // Load settings
        _settings = AppSettings.Load();
        
        // Apply appearance settings
        ApplyAppearanceSettings();
        
        // Create the active task widget with settings
        _activeTaskWidget = new ActiveTaskWidget(_settings);

        _todos = new ObservableCollection<TodoItem>(TodoStorage.Load());
        _todoView = CollectionViewSource.GetDefaultView(_todos);
        var completionGrouping = new PropertyGroupDescription(nameof(TodoItem.IsCompleted));
        completionGrouping.GroupNames.Add(false);
        completionGrouping.GroupNames.Add(true);
        _todoView.GroupDescriptions!.Add(completionGrouping);
        if (_todoView is ICollectionViewLiveShaping liveShaping && liveShaping.CanChangeLiveGrouping)
        {
            liveShaping.LiveGroupingProperties.Add(nameof(TodoItem.IsCompleted));
            liveShaping.IsLiveGrouping = true;
            _usesLiveGrouping = true;
        }
        TodoListBox.ItemsSource = _todoView;

        if (_todos.Count == 0)
        {
            var defaultTasks = TodoStorage.GetDefaultTasks();
            foreach (var task in defaultTasks)
            {
                _todos.Add(task);
            }
        }

        TodoStorage.Save(_todos);
        ShowEmptyState();
    }

    private void OpenAddTask_Click(object sender, RoutedEventArgs e)
    {
        AddTaskSection.Visibility = Visibility.Visible;
        FloatingAddButton.Visibility = Visibility.Collapsed;
        TodoTitleInput.Focus();
    }

    private void CancelAddTask_Click(object sender, RoutedEventArgs e)
    {
        AddTaskSection.Visibility = Visibility.Collapsed;
        FloatingAddButton.Visibility = Visibility.Visible;
        TodoTitleInput.Clear();
        TodoSubtitleInput.Clear();
        TodoDescriptionInput.Clear();
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        var title = TodoTitleInput.Text.Trim();
        var subtitle = TodoSubtitleInput.Text.Trim();
        var description = TodoDescriptionInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Please enter a task title before adding a new task.", "Task required", MessageBoxButton.OK, MessageBoxImage.Information);
            TodoTitleInput.Focus();
            return;
        }

        var todo = new TodoItem
        {
            Title = title,
            Subtitle = subtitle,
            Description = string.IsNullOrWhiteSpace(description) ? "No additional details added yet." : description
        };

        _todos.Add(todo);
        TodoStorage.Save(_todos);
        TodoListBox.SelectedItem = todo;
        TodoTitleInput.Clear();
        TodoSubtitleInput.Clear();
        TodoDescriptionInput.Clear();
        
        AddTaskSection.Visibility = Visibility.Collapsed;
        FloatingAddButton.Visibility = Visibility.Visible;
        TodoTitleInput.Focus();
    }

    private void TodoListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            ShowEmptyState();
            return;
        }

        ShowDetail(selectedItem);
    }

    private void TodoListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (FindAncestor<CheckBox>(source) is not null)
            return;

        var listItem = FindAncestor<ListBoxItem>(source);

        if (listItem is null || TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            return;
        }

        if (ReferenceEquals(listItem.DataContext, selectedItem))
        {
            TodoListBox.SelectedItem = null;
            e.Handled = true;
        }
    }

    private void ToggleCompleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            return;
        }

        selectedItem.IsCompleted = !selectedItem.IsCompleted;
        RefreshTodoGroups();
        if (!ReferenceEquals(TodoListBox.SelectedItem, selectedItem))
            TodoListBox.SelectedItem = selectedItem;
        TodoStorage.Save(_todos);
        ShowDetail(selectedItem);
        if (selectedItem.IsCompleted)
            AnimateTaskCompletion(selectedItem);
    }

    private void TaskCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { DataContext: TodoItem item })
            return;

        bool wasSelected = ReferenceEquals(TodoListBox.SelectedItem, item);
        RefreshTodoGroups();
        TodoStorage.Save(_todos);
        if (wasSelected)
        {
            if (!ReferenceEquals(TodoListBox.SelectedItem, item))
                TodoListBox.SelectedItem = item;
            ShowDetail(item);
        }
        if (item.IsCompleted)
            AnimateTaskCompletion(item);
    }

    private void RefreshTodoGroups()
    {
        if (!_usesLiveGrouping)
            _todoView.Refresh();
    }

    private void AnimateTaskCompletion(TodoItem item)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var container = FindVisualChildren<ListBoxItem>(TodoListBox)
                .FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, item));
            var card = container is null ? null : FindVisualChildren<Border>(container).FirstOrDefault();
            if (card is null)
                return;

            card.RenderTransformOrigin = new WpfPoint(0.5, 0.5);
            var pulse = new System.Windows.Media.ScaleTransform(1, 1);
            card.RenderTransform = pulse;
            var animation = new DoubleAnimation(1, 1.045, TimeSpan.FromMilliseconds(150))
            {
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            pulse.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
            pulse.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
            card.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1, 0.72, TimeSpan.FromMilliseconds(150))
            {
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
        }), DispatcherPriority.Loaded);
    }

    private void EditTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            return;
        }

        EditTaskGrid.Visibility = EditTaskGrid.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (EditTaskGrid.Visibility == Visibility.Visible)
        {
            DetailTitleInput.Text = selectedItem.Title;
            DetailSubtitleInput.Text = selectedItem.Subtitle;
            DetailDescriptionInput.Text = selectedItem.Description;
            DetailTitleInput.Focus();
        }
    }

    private void SaveSelectedTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            return;
        }

        var title = DetailTitleInput.Text.Trim();
        var subtitle = DetailSubtitleInput.Text.Trim();
        var description = DetailDescriptionInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            MessageBox.Show("Task title cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            DetailTitleInput.Focus();
            return;
        }

        selectedItem.Title = title;
        selectedItem.Subtitle = subtitle;
        selectedItem.Description = description;
        TodoStorage.Save(_todos);
        TodoListBox.Items.Refresh();
        EditTaskGrid.Visibility = Visibility.Collapsed;
        ShowDetail(selectedItem);
    }

    private void DeleteSelectedTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (TodoListBox.SelectedItem is not TodoItem selectedItem)
        {
            return;
        }

        var result = MessageBox.Show($"Delete task '{selectedItem.Title}'?", "Delete task", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _todos.Remove(selectedItem);
        TodoStorage.Save(_todos);
        TodoListBox.SelectedItem = null;
        ShowEmptyState();
    }

    private void ShowEmptyState()
    {
        DetailPanel.Visibility = Visibility.Collapsed;
        EmptyDetailPanel.Visibility = Visibility.Visible;
        SelectedTitleText.Text = "Select a task";
        SelectedSubtitleText.Text = "";
        SelectedSubtitleText.Visibility = Visibility.Collapsed;
        SelectedDescriptionText.Text = "Choose an item from the list to view its full description here.";
        DetailTitleInput.Text = string.Empty;
        DetailSubtitleInput.Text = string.Empty;
        DetailDescriptionInput.Text = string.Empty;
        EditTaskGrid.Visibility = Visibility.Collapsed;
        ToggleCompleteButton.Content = "Mark as complete";
        CompletionMarker.Visibility = Visibility.Collapsed;
        _activeTaskWidget?.Hide();
    }

    private void ShowDetail(TodoItem selectedItem)
    {
        DetailPanel.Visibility = Visibility.Visible;
        EmptyDetailPanel.Visibility = Visibility.Collapsed;
        SelectedTitleText.Text = selectedItem.Title;
        SelectedSubtitleText.Text = selectedItem.Subtitle;
        SelectedSubtitleText.Visibility = string.IsNullOrWhiteSpace(selectedItem.Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        SelectedDescriptionText.Text = selectedItem.Description;
        DetailTitleInput.Text = selectedItem.Title;
        DetailSubtitleInput.Text = selectedItem.Subtitle;
        DetailDescriptionInput.Text = selectedItem.Description;
        EditTaskGrid.Visibility = Visibility.Collapsed;
        
        // Show the active task widget
        if (_activeTaskWidget != null)
        {
            _activeTaskWidget.ShowTask(selectedItem);
        }

        if (selectedItem.IsCompleted)
        {
            CompletionMarker.Visibility = Visibility.Visible;
            ToggleCompleteButton.Content = "Mark as incomplete";
        }
        else
        {
            CompletionMarker.Visibility = Visibility.Collapsed;
            ToggleCompleteButton.Content = "Mark as complete";
        }
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T ancestor)
            {
                return ancestor;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this
        };

        settingsWindow.SettingsChanged += ApplySettings;

        if (settingsWindow.ShowDialog() == true)
        {
            ApplySettings();
        }
    }

    private void ApplyAppearanceSettings()
    {
        try
        {
            Resources["AppBackgroundBrush"] = CreateBrush(_settings.AppBackgroundColor);
            Resources["AppPanelBrush"] = CreateBrush(_settings.AppPanelColor);
            Resources["AppTextBrush"] = CreateBrush(_settings.AppTextColor);
            Resources["AppSecondaryTextBrush"] = CreateBrush(_settings.AppSecondaryTextColor);
            Resources["AppBorderBrush"] = CreateBrush(_settings.AppBorderColor);
            Resources["AppPrimaryButtonBrush"] = CreateBrush(_settings.AppPrimaryButtonColor);
            Resources["AppSecondaryButtonBrush"] = CreateBrush(_settings.AppSecondaryButtonColor);
            Resources["AppTaskCardBrush"] = CreateBrush(string.IsNullOrWhiteSpace(_settings.AppTaskCardBackgroundColor) ? _settings.AppPanelColor : _settings.AppTaskCardBackgroundColor);
            Resources["AppTaskCardTextBrush"] = CreateBrush(string.IsNullOrWhiteSpace(_settings.AppTaskCardTextColor) ? _settings.AppTextColor : _settings.AppTaskCardTextColor);
            Resources["AppTaskCardSubtitleBrush"] = CreateBrush(string.IsNullOrWhiteSpace(_settings.AppTaskCardSubtitleColor) ? _settings.AppSecondaryTextColor : _settings.AppTaskCardSubtitleColor);
            Resources["AppCompletedTaskTextBrush"] = CreateBrush(_settings.AppCompletedTaskTextColor);
            Resources["AppTaskCardBorderBrush"] = CreateBrush(string.IsNullOrWhiteSpace(_settings.AppTaskCardBorderColor) ? _settings.AppBorderColor : _settings.AppTaskCardBorderColor);
            Resources["AppCheckboxAccentBrush"] = CreateBrush(string.IsNullOrWhiteSpace(_settings.AppCheckboxAccentColor) ? _settings.AppPrimaryButtonColor : _settings.AppCheckboxAccentColor);
            Resources["AppFontFamily"] = CreateFontFamily(_settings.AppFontFamily);
            Resources["AppTitleSize"] = (double)_settings.AppTitleFontSize;
            Resources["AppHeadingSize"] = (double)_settings.AppHeadingFontSize;
            Resources["AppTextSize"] = (double)_settings.AppTextFontSize;
            Resources["AppLabelSize"] = (double)_settings.AppLabelFontSize;

            Background = (Brush)Resources["AppBackgroundBrush"];
            TaskListPanel.Background = (Brush)Resources["AppPanelBrush"];
            DetailPanel.Background = (Brush)Resources["AppPanelBrush"];
            EmptyDetailPanel.Background = (Brush)Resources["AppPanelBrush"];
            AddTaskSection.Background = (Brush)Resources["AppPanelBrush"];
            TaskListPanel.BorderBrush = (Brush)Resources["AppBorderBrush"];
            DetailPanel.BorderBrush = (Brush)Resources["AppBorderBrush"];
            EmptyDetailPanel.BorderBrush = (Brush)Resources["AppBorderBrush"];
            FloatingAddButtonBorder.Background = (Brush)Resources["AppPrimaryButtonBrush"];

            SettingsButton.Background = (Brush)Resources["AppSecondaryButtonBrush"];
            AddTaskActionButton.Background = (Brush)Resources["AppPrimaryButtonBrush"];
            CancelAddTaskButton.Background = (Brush)Resources["AppSecondaryButtonBrush"];
            ToggleCompleteButton.Background = (Brush)Resources["AppSecondaryButtonBrush"];
            EditTaskButton.Background = (Brush)Resources["AppSecondaryButtonBrush"];
            SaveTaskButton.Background = (Brush)Resources["AppPrimaryButtonBrush"];
            DeleteTaskButton.Background = (Brush)Resources["AppSecondaryButtonBrush"];
        }
        catch
        {
            // Silently fail if color application fails
        }
    }

    private static SolidColorBrush CreateBrush(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));

    private static System.Windows.Media.FontFamily CreateFontFamily(string familyName)
    {
        try { return new System.Windows.Media.FontFamily(familyName); }
        catch { return new System.Windows.Media.FontFamily("Segoe UI"); }
    }

    private void InitializeTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Tasker", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(() =>
        {
            _exitRequested = true;
            Close();
        }));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tasker.ico");
        _applicationIcon = File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();

        _trayIcon = new Forms.NotifyIcon
        {
            Text = "Tasker",
            Icon = _applicationIcon,
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        _windowSource = HwndSource.FromHwnd(handle);
        _windowSource?.AddHook(WindowMessageHook);
        ApplyWindowsTitleBarTheme(handle);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmSettingChange || message == WmThemeChanged)
            Dispatcher.BeginInvoke(() => ApplyWindowsTitleBarTheme(hwnd));

        return IntPtr.Zero;
    }

    private static void ApplyWindowsTitleBarTheme(IntPtr hwnd)
    {
        try
        {
            using var personalization = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var themeValue = personalization?.GetValue("AppsUseLightTheme")
                ?? personalization?.GetValue("SystemUsesLightTheme");
            var useDarkTitleBar = themeValue is int lightTheme && lightTheme == 0 ? 1 : 0;

            var result = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode,
                ref useDarkTitleBar, sizeof(int));
            if (result != 0)
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeLegacy,
                    ref useDarkTitleBar, sizeof(int));
        }
        catch
        {
            // Keep the standard Windows title bar if theme detection is unavailable.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute,
        ref int attributeValue, int attributeSize);

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_exitRequested)
            return;

        e.Cancel = true;
        Hide();
    }

    private void RestoreFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    private void ApplySettings()
    {
        ApplyAppearanceSettings();
        _activeTaskWidget?.ApplySettings(_settings);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) yield break;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
