using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Tasker;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<TodoItem> _todos;
    private ActiveTaskWidget? _activeTaskWidget;
    private AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();
        
        // Load settings
        _settings = AppSettings.Load();
        
        // Apply appearance settings
        ApplyAppearanceSettings();
        
        // Create the active task widget with settings
        _activeTaskWidget = new ActiveTaskWidget(_settings);

        _todos = new ObservableCollection<TodoItem>(TodoStorage.Load());
        TodoListBox.ItemsSource = _todos;

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
        TodoStorage.Save(_todos);
        ShowDetail(selectedItem);
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

        if (settingsWindow.ShowDialog() == true)
        {
            // Apply appearance settings to main window
            ApplyAppearanceSettings();
            
            // Update the active task widget with new settings
            if (_activeTaskWidget != null)
            {
                _activeTaskWidget.ApplySettings(_settings);
                
                // If there's a selected task, refresh the widget display
                if (TodoListBox.SelectedItem is TodoItem selectedItem)
                {
                    _activeTaskWidget.ShowTask(selectedItem);
                }
            }
        }
    }

    private void ApplyAppearanceSettings()
    {
        try
        {
            // Apply window and panel colors
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppBackgroundColor));
            
            // Find all borders and apply panel color
            foreach (var border in FindVisualChildren<Border>(this))
            {
                if (border.Name == "AddTaskSection" || border.Background != null)
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppPanelColor));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppBorderColor));
            }

            // Apply text colors to TextBlocks
            foreach (var textBlock in FindVisualChildren<TextBlock>(this))
            {
                if (!textBlock.Foreground.ToString().Contains("#dfeafc") && !textBlock.Foreground.ToString().Contains("#5e6a81"))
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppTextColor));
            }

            // Apply button colors
            foreach (var button in FindVisualChildren<Button>(this))
            {
                if (button.Background.ToString().Contains("3f7af8"))
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppPrimaryButtonColor));
                else if (button.Background.ToString().Contains("e0e0e0"))
                    button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_settings.AppSecondaryButtonColor));
            }
        }
        catch
        {
            // Silently fail if color application fails
        }
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