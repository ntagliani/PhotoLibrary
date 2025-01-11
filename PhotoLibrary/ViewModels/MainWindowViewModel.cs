using PhotoLibrary.Common;
using PhotoLibrary.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.ViewModels;

[Export(typeof(MainWindowViewModel))]
public class MainWindowViewModel
{

    private readonly IPicturesModel m_mainWindowModel;

    public ObservableCollection<PictureViewModel> Images { get; set; } = [];
    
    [ImportingConstructor]
    public MainWindowViewModel(IPicturesModel mainWindowModel)
    {
        m_mainWindowModel = mainWindowModel;
        
        foreach (var  picture in m_mainWindowModel.GetPictures())
        {
            Images.Add(new PictureViewModel() {
                Path = picture.Path,
                Image = picture.Image,
            });
        }
    }
}
