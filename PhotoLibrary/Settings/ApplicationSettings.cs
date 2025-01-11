using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.Settings;

public interface IApplicationSettings : ISettings
{
    public string DatabasePath { get; set; }

}

[Export(typeof(ISettings))]
[Export(typeof(IApplicationSettings))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class ApplicationSettings : IApplicationSettings
{
    private readonly static string _databaseFilename = "photos.db";
    public int Version { get; set; } = 1;
    public string DatabasePath { get; set; }

    [ImportingConstructor]
    ApplicationSettings(StaticSettings settings)
    {
        DatabasePath = Path.Combine(settings.PicutresFolder, settings.ApplicationFolderName, _databaseFilename);
    }
}
