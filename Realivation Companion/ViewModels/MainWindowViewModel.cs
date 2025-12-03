using Realivation_Companion.Views;
using System.Windows.Input;

namespace Realivation_Companion.ViewModels;

class MainWindowViewModel : ViewModelBase
{
    const int REALIVATION_PORT = 33134;
    private ViewModelBase _currentViewModel = new();
    private readonly Comms _comms = new(REALIVATION_PORT);
    private bool _isLogVisible = false;
    public bool IsLogVisible
    {
        get => _isLogVisible;
        set {
            _isLogVisible = value;
            OnPropertyChanged(nameof(IsLogVisible));
        }
    }
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
    public ICommand ShowAboutCmd => new RelayCommand(OnShowAbout);

    public MainWindowViewModel()
    {
        ShowPairingViewCommand = new RelayCommand(execute => CurrentViewModel = new PairingViewModel());
        ShowTransferViewCommand = new RelayCommand(execute => CurrentViewModel = new TransferViewModel(_comms));
        _comms.QuestConnectedEvent += OnQuestConnected;
        _comms.QuestDisconnectedEvent += OnQuestDisconnected;
    }

    private void OnQuestConnected(object? sender, EventArgs e)
    {
        CurrentViewModel = new TransferViewModel(_comms);
    }

    private void OnQuestDisconnected(object? sender, EventArgs e)
    {
        CurrentViewModel = new PairingViewModel();
    }

    // cuz logging doesnt work until the window shows up...
    public void RunStartupStuff()
    {
        CurrentViewModel = new PairingViewModel();
        _ = _comms.StartListening();
    }

    private void OnShowAbout(object? commandParameter)
    {
        new AboutView().Show();
    }
}
