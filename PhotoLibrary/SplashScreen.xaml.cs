using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PhotoLibrary;

/// <summary>
/// Interaction logic for Window1.xaml
/// </summary>
[Export(typeof(PhotoLibrary.SplashScreen))]
[PartCreationPolicy(CreationPolicy.Shared)]

public partial class SplashScreen : Window
{

    private MainWindow _window;

    [ImportingConstructor]
    public SplashScreen(MainWindow mainWindow)
    {
        _window = mainWindow;
        _window.IsVisibleChanged += OnMainWindowVisibilityChanged;
        InitializeComponent();
        Closed += OnSplashScreenClosed;

    }

    private void OnSplashScreenClosed(object sender, EventArgs e)
    {
        _window.IsVisibleChanged -= OnMainWindowVisibilityChanged;
        Closed -= OnSplashScreenClosed;
    }

    private void OnMainWindowVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if ((bool)e.NewValue)
        {
            Close();
        }
    }
}
