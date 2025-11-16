using System.Windows.Input;

namespace Realivation_Companion.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase _currentViewModel = new();
        private Comms _comms = new();

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        public ICommand ShowPairingViewCommand { get; }
        public ICommand ShowTransferViewCommand { get; }

        public MainWindowViewModel()
        {
            CurrentViewModel = new PairingViewModel();
            ShowPairingViewCommand = new RelayCommand(execute => CurrentViewModel = new PairingViewModel());
            ShowTransferViewCommand = new RelayCommand(execute => CurrentViewModel = new TransferViewModel());
        }
    }
}
