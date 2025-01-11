using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PhotoLibrary.ViewModels;

public class PictureViewModel : ObservableObject
{
    private string m_path;

    private Image m_image;
    public Image Image
    {
        get => m_image;
        set => Set(ref m_image, value);
    }
    public string Path
    {
        get => m_path;
        set => Set(ref m_path, value);
    }
}
