using PhotoLibrary.DB;
using PhotoLibrary.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PhotoLibrary
{
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
        
        public MainWindow()
        {
            InitializeComponent();
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
    }
}
