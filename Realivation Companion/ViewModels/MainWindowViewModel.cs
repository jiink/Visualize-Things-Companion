using System.Windows.Input;

namespace Realivation_Companion.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        const int REALIVATION_PORT = 33134;
        private ViewModelBase _currentViewModel = new();
        private Comms _comms = new(REALIVATION_PORT);
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
            ShowPairingViewCommand = new RelayCommand(execute => CurrentViewModel = new PairingViewModel());
            ShowTransferViewCommand = new RelayCommand(execute => CurrentViewModel = new TransferViewModel());
        }

        // cuz logging doesnt work until the window shows up...
        public void RunStartupStuff()
        {
            CurrentViewModel = new PairingViewModel();
            _ = _comms.StartListening();
        }
    }
}
