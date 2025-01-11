using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace PhotoLibrary
{

    /// <summary>
    ///  Helper class to access the exported resources
    /// </summary>
    public class AddInManager
    {
        private CompositionContainer _compositionContainer;

        public static AddInManager Instance { get; } = new();

        internal void SetCompositionContainer(CompositionContainer compositionContainer)
        {  _compositionContainer = compositionContainer; }

        public T GetInstance<T>()
        {
            return _compositionContainer.GetExportedValue<T>();
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private CompositionContainer _container;

        [Import]
        private MainWindow _mainWindow { get; set; }
        [Import]
        private SplashScreen _splashScreen { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
          
            try
            {
                // An aggregate catalog that combines multiple catalogs.
                var catalog = new AggregateCatalog();
                // Adds all the parts found in the same assembly as the Program class.
                foreach (var type in typeof(App).Assembly.GetTypes())
                {
                    Console.WriteLine(type.Name);
                }
                catalog.Catalogs.Add(new AssemblyCatalog(typeof(App).Assembly));

                // Create the CompositionContainer with the parts in the catalog.
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);

                AddInManager.Instance.SetCompositionContainer(_container);
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }

            _splashScreen.Show();
            _mainWindow.Init();
            _mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _container.Dispose();

            base.OnExit(e);
        }
        void AppStartup(object sender, StartupEventArgs e)
        {
             
        }

    }
}
