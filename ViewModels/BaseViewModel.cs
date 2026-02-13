using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace DocumentalManager.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private string title = string.Empty;

        public ICommand GoBackCommand => new RelayCommand(async () =>
        {
            await Shell.Current.GoToAsync("..");
        });
    }
}