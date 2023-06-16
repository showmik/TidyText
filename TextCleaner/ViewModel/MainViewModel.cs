using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextCleaner.ViewModel
{
    partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private bool _trim;
        [ObservableProperty] private bool _removeLeadSpace;
        [ObservableProperty] private bool _removeTrailSpace;
        [ObservableProperty] private bool _multipleSpaceToSingle;
        [ObservableProperty] private bool _multipleLinesToSingle;
        [ObservableProperty] private bool _removeLineBreaks;
        [ObservableProperty] private bool _wrapLines;

        [ObservableProperty] private string _mainText;
    }
}
