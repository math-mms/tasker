using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WpfPoint = System.Windows.Point;

namespace Tasker;

public partial class ActiveTaskWidget : Window
{
    private bool _isDragging = false;
    private bool _mouseDown = false;

    private WpfPoint _dragStartMouse;
    private WpfPoint _windowStartPosition;

    private AppSettings _settings;

    // -----------------------------
    // Windows mouse hook
    // -----------------------------

    private const int WH_MOUSE_LL = 14;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_MOUSEMOVE = 0x0200;

    private static LowLevelMouseProc? _mouseProc;
    private static IntPtr _mouseHook = IntPtr.Zero;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelMouseProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    // -----------------------------
    // Click-through
    // -----------------------------

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex);

    // -----------------------------

    public ActiveTaskWidget(AppSettings? settings = null)
    {
        InitializeComponent();

        _settings = settings ?? AppSettings.Load();

        ApplySettings(_settings);

        Loaded += ActiveTaskWidget_Loaded;
        Closed += ActiveTaskWidget_Closed;
    }

    private void ActiveTaskWidget_Loaded(object sender, RoutedEventArgs e)
    {
        StartMouseHook();

        // Normal state = click-through
        MakeClickThrough();
    }

    private void ActiveTaskWidget_Closed(object? sender, EventArgs e)
    {
        StopMouseHook();
    }

    // -----------------------------
    // Mouse hook
    // -----------------------------

    private void StartMouseHook()
    {
        if (_mouseHook != IntPtr.Zero)
            return;

        _mouseProc = MouseHookCallback;

        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? module = currentProcess.MainModule;

        _mouseHook = SetWindowsHookEx(
            WH_MOUSE_LL,
            _mouseProc,
            GetModuleHandle(module?.ModuleName),
            0);
    }

    private void StopMouseHook()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHook);
            _mouseHook = IntPtr.Zero;
        }

        _mouseProc = null;
    }

    private IntPtr MouseHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var mouse = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            switch (wParam.ToInt32())
            {
                case WM_LBUTTONDOWN:
                    HandleGlobalMouseDown(mouse.pt);
                    break;

                case WM_MOUSEMOVE:
                    HandleGlobalMouseMove(mouse.pt);
                    break;

                case WM_LBUTTONUP:
                    HandleGlobalMouseUp();
                    break;
            }
        }

        return CallNextHookEx(
            _mouseHook,
            nCode,
            wParam,
            lParam);
    }

    // -----------------------------
    // Drag logic
    // -----------------------------

    private void HandleGlobalMouseDown(POINT mousePosition)
    {
        Dispatcher.Invoke(() =>
        {
            if (!IsMouseOverWidget(mousePosition))
                return;

            _mouseDown = true;
            _isDragging = false;

            _dragStartMouse = new WpfPoint(
                mousePosition.X,
                mousePosition.Y);

            _windowStartPosition = new WpfPoint(
                Left,
                Top);
        });
    }

    private void HandleGlobalMouseMove(POINT mousePosition)
    {
        if (!_mouseDown)
            return;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_mouseDown)
                return;

            var offset = ScreenDeltaToDip(
                mousePosition.X - _dragStartMouse.X,
                mousePosition.Y - _dragStartMouse.Y);
            double offsetX = offset.X;
            double offsetY = offset.Y;

            // Don't start dragging from a tiny accidental movement
            const double dragThreshold = 5;

            if (!_isDragging)
            {
                if (Math.Abs(offsetX) < dragThreshold &&
                    Math.Abs(offsetY) < dragThreshold)
                {
                    return;
                }

                _isDragging = true;

                // Make the window receive mouse input
                MakeClickable();
            }

            Left = _windowStartPosition.X + offsetX;
            Top = _windowStartPosition.Y + offsetY;
        }));
    }

    private void HandleGlobalMouseUp()
    {
        if (!_mouseDown)
            return;

        Dispatcher.Invoke(() =>
        {
            _mouseDown = false;

            if (_isDragging)
            {
                _settings.WidgetPositionX = (int)Left;
                _settings.WidgetPositionY = (int)Top;
                _settings.Save();
            }

            _isDragging = false;

            // Back to click-through
            MakeClickThrough();
        });
    }

    // -----------------------------
    // Check whether cursor is over widget
    // -----------------------------

    private bool IsMouseOverWidget(POINT point)
    {
        var cursor = ScreenDeltaToDip(point.X, point.Y);
        return cursor.X >= Left &&
               cursor.X <= Left + ActualWidth &&
               cursor.Y >= Top &&
               cursor.Y <= Top + ActualHeight;
    }

    private WpfPoint ScreenDeltaToDip(double x, double y)
    {
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice;
        return transform?.Transform(new WpfPoint(x, y)) ?? new WpfPoint(x, y);
    }

    // -----------------------------
    // Click-through
    // -----------------------------

    private void MakeClickThrough()
    {
        IntPtr hwnd =
            new WindowInteropHelper(this).Handle;

        int extendedStyle =
            GetWindowLong(hwnd, GWL_EXSTYLE);

        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            extendedStyle | WS_EX_TRANSPARENT);
    }

    private void MakeClickable()
    {
        IntPtr hwnd =
            new WindowInteropHelper(this).Handle;

        int extendedStyle =
            GetWindowLong(hwnd, GWL_EXSTYLE);

        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            extendedStyle & ~WS_EX_TRANSPARENT);
    }
    public void ShowTask(TodoItem task)
    {
        bool hasSubtitle = string.IsNullOrWhiteSpace(task.Subtitle);
        bool showDescription = !hasSubtitle && !string.IsNullOrWhiteSpace(task.Description);
        if (task == null)
        return;

        WidgetTitleText.Text = string.IsNullOrWhiteSpace(task.Title)
            ? "No active task"
            : task.Title;

        WidgetSubtitleText.Text = task.Subtitle;
        WidgetSubtitleText.Visibility =
            string.IsNullOrWhiteSpace(task.Subtitle)
                ? Visibility.Collapsed
                : Visibility.Visible;

        WidgetDescriptionText.Text = task.Description;
        WidgetDescriptionText.Visibility =
            showDescription
                ? Visibility.Collapsed
                : Visibility.Visible;

        Show();
        Activate();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        try { FontFamily = new System.Windows.Media.FontFamily(settings.WidgetFontFamily); }
        catch { FontFamily = new System.Windows.Media.FontFamily("Segoe UI"); }
        Left = settings.WidgetPositionX;
        Top = settings.WidgetPositionY;
        Opacity = Math.Clamp(settings.WidgetOpacity, 0, 1);
        WidgetBorder.MaxWidth = Math.Max(100, settings.WidgetMaxWidth);
        WidgetBorder.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WidgetBackgroundColor));
        WidgetBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WidgetBorderColor));
        WidgetTitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WidgetTitleColor));
        WidgetTitleText.FontSize = settings.WidgetTitleFontSize;
        WidgetSubtitleText.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WidgetSubtitleColor));
        WidgetSubtitleText.FontSize = settings.WidgetSubtitleFontSize;
        WidgetDescriptionText.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.WidgetDescriptionColor));
        WidgetDescriptionText.FontSize = settings.WidgetDescriptionFontSize;
    }
    private void Border_PreviewMouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
    {
        // O drag real é tratado pelo Windows mouse hook.
        // Este evento existe para satisfazer o handler declarado no XAML.
    }

    private void Border_PreviewMouseMove(
        object sender,
    System.Windows.Input.MouseEventArgs e)
    {
        // O drag real é tratado pelo Windows mouse hook.
    }

    private void Border_PreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        // O drag real é tratado pelo Windows mouse hook.
    }
}
