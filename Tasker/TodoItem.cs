using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Tasker;

public class TodoItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _subtitle = string.Empty;
    private string _description = string.Empty;
    private bool _isCompleted;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Subtitle { get => _subtitle; set => SetField(ref _subtitle, value); }
    public string Description { get => _description; set => SetField(ref _description, value); }
    public bool IsCompleted { get => _isCompleted; set => SetField(ref _isCompleted, value); }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
