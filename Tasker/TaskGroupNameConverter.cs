using System.Globalization;
using System.Windows.Data;

namespace Tasker;

public sealed class TaskGroupNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is bool isCompleted && isCompleted ? "Completed" : "To do";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        System.Windows.Data.Binding.DoNothing;
}
