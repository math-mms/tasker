using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Tasker;

public static class TodoStorage
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Tasker",
        "todos.json");

    public static ObservableCollection<TodoItem> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new ObservableCollection<TodoItem>();
            }

            var json = File.ReadAllText(FilePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new ObservableCollection<TodoItem>();
            }

            var items = JsonSerializer.Deserialize<List<TodoItem>>(json);
            return items is null ? new ObservableCollection<TodoItem>() : new ObservableCollection<TodoItem>(items);
        }
        catch
        {
            return new ObservableCollection<TodoItem>();
        }
    }

    public static void Save(IEnumerable<TodoItem> items)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }

    public static List<TodoItem> GetDefaultTasks()
    {
        return new List<TodoItem>
        {
            new TodoItem
            {
                Title = "Plan sprint tasks",
                Description = "Meet with the team, review priorities, and confirm which features are ready for the next sprint review."
            },
            new TodoItem
            {
                Title = "Review bug reports",
                Description = "Check the backlog for high-severity issues, classify them by impact, and assign first responders for triage."
            },
            new TodoItem
            {
                Title = "Prepare release notes",
                Description = "Collect fixes, improvements, and new features from the current cycle and summarize them for customers and stakeholders."
            },
            new TodoItem
            {
                Title = "Client follow-up",
                Description = "Send a short update to the client confirming the current status, next milestone, and any concerns requiring review."
            }
        };
    }
}
