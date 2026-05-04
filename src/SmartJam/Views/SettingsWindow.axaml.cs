using Avalonia.Controls;
using SmartJam.ViewModels;

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
    {
        // Apply command is already bound; close after applying
        if (DataContext is SettingsViewModel vm)
            vm.ApplyCommand.Execute(null);
        Close();
    }
}
