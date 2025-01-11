using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PhotoLibrary.Models;

public class ImageDemo : IImageModel
{
    public Image Image { get; set; }

    public string Name { get; set; }

    public ulong Size { get; set; }

    public string Path { get; set; }
}

[Export(typeof(IPicturesModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class PicturesDemoModel : IPicturesModel
{
    List<IImageModel> m_pictures = new();
    public IEnumerable<IImageModel> GetPictures()
    {
        return m_pictures;
    }

    public PicturesDemoModel()
    {
        for (int i = 0; i < 200; i++)
        {
            m_pictures.Add(new ImageDemo() {
                Path = $"{i}_path",
                Name = $"{i}_name",
                Size = (ulong)i * 1024,
            });
        }
    }

}
