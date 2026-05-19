using Avalonia.Controls;

namespace SmartJam.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Close();

    private void OnApplyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Close();
}
