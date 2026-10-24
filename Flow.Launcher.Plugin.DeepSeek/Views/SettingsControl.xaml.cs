using System.Windows.Controls;
using Flow.Launcher.Plugin.DeepSeek.ViewModels;

namespace Flow.Launcher.Plugin.DeepSeek.Views;

public partial class SettingsControl : UserControl
{
    public SettingsControl(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
