using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.Settings;

[Export]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class StaticSettings
{
    static readonly private string _applicationFolderName = "PhotoLibrary";
    static readonly private string _configurationFolderName = "conf";

    public string ApplicationFolderName { get { return _applicationFolderName; } }
    public string ApplicationFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _applicationFolderName);
    public string ConfigFolder { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _applicationFolderName, _configurationFolderName);

    public string HomeFolder { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    public string PicutresFolder { get; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    public StaticSettings()
    {
        if (!Directory.Exists(ConfigFolder))
        {
            Directory.CreateDirectory(ConfigFolder);
        }
    }
}
