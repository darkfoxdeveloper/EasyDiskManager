using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasyDiskManager.Services;

public static class DialogService
{
    public static string? Prompt(string title, string message, string initialValue = "")
    {
        var textBox = new TextBox
        {
            Text = initialValue,
            Margin = new Thickness(0, 10, 0, 18),
            Padding = new Thickness(10, 8, 10, 8),
            FontSize = 14
        };
        var window = CreateWindow(title);
        var panel = CreatePanel(message);
        panel.Children.Add(textBox);
        AddButtons(window, panel);
        window.Content = panel;
        window.Loaded += (_, _) => { textBox.Focus(); textBox.SelectAll(); };
        return window.ShowDialog() == true ? textBox.Text.Trim() : null;
    }

    public static string? Choose(string title, string message, IEnumerable<string> choices, string? selected = null)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = choices.ToArray(),
            SelectedItem = selected,
            Margin = new Thickness(0, 10, 0, 18),
            Padding = new Thickness(8, 6, 8, 6),
            FontSize = 14
        };
        if (comboBox.SelectedIndex < 0) comboBox.SelectedIndex = 0;
        var window = CreateWindow(title);
        var panel = CreatePanel(message);
        panel.Children.Add(comboBox);
        AddButtons(window, panel);
        window.Content = panel;
        return window.ShowDialog() == true ? comboBox.SelectedItem?.ToString() : null;
    }

    private static Window CreateWindow(string title) => new()
    {
        Title = title,
        Width = 430,
        SizeToContent = SizeToContent.Height,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        Owner = Application.Current.MainWindow,
        Background = Brushes.White,
        ShowInTaskbar = false
    };

    private static StackPanel CreatePanel(string message)
    {
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 32, 51))
        });
        return panel;
    }

    private static void AddButtons(Window window, Panel panel)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancelar", MinWidth = 90 };
        var accept = new Button { Content = "Aceptar", MinWidth = 90, IsDefault = true };
        cancel.Click += (_, _) => window.DialogResult = false;
        accept.Click += (_, _) => window.DialogResult = true;
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        panel.Children.Add(buttons);
    }
}
