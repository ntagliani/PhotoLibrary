using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.Models;

public interface IPicturesModel
{
    IEnumerable<IImageModel> GetPictures();
}
