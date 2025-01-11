using PhotoLibrary.DB;
using PhotoLibrary.Settings;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using PhotoLibrary.ViewModels;
using System.Windows;

namespace PhotoLibrary;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
[Export(typeof(MainWindow))]
[PartCreationPolicy(CreationPolicy.Shared)]
public partial class MainWindow : Window
{
    [ImportMany]
    private IEnumerable<ISettings> AllSettings { get; set; }

    [Import]
    private PhotoDb Database { get; set; }

    [ImportingConstructor]
    public MainWindow(MainWindowViewModel mainWindowViewModel)
    {
        InitializeComponent();
        DataContext = mainWindowViewModel;
    }

    public void Init()
    {
        LoadSettings();
        Database.Init();
    }
    private void LoadSettings()
    {
        foreach (var setting in AllSettings)
        {
            setting.Load();
        }
    }

    public void OnLoaded(object sender, RoutedEventArgs e)
    {
        // m_splashScreen.Close();
    }
}
