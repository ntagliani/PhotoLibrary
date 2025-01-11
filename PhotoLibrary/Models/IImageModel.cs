using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PhotoLibrary.Models;

public interface IImageModel
{
    Image Image { get; }
    String Name { get; }
    UInt64 Size { get; }
    String Path { get; }
}
