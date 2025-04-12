using BatchRename.Models;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace BatchRename.ViewModels
{
    public  class MainViewModel 
    {
        public MainViewModel()
        { }

        public static FileRenameModel Model =>FileRenameModel.Instance;
    }
}
