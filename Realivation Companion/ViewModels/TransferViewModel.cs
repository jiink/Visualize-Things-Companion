using System.Windows.Input;

namespace Realivation_Companion.ViewModels;

class TransferViewModel : ViewModelBase
{
    private double _transferProgress = 0; // 0.0 to 1.0
    public double TransferProgress
    {
        get => _transferProgress;
        set
        {
            if (_transferProgress != value)
            {
                _transferProgress = value;
                OnPropertyChanged();
            }
        }
    }
    public ICommand DisconnectCmd { get; private set; }

    private Comms comms;

    public TransferViewModel(Comms _comms)
    {
        comms = _comms;
        DisconnectCmd = new RelayCommand(OnDisconnectCmd);
        comms.ProgressUpdateEvent += OnTransferProgresssUpdate;
    }

    private void OnTransferProgresssUpdate(object? sender, EventArgs e)
    {
        if (sender is Comms c)
        {
            TransferProgress = c.TransferProgress;
        }
    }

    private void OnDisconnectCmd(object? obj)
    {
        comms.UnpairQuest();
    }

    internal void OnDragNDrop(string filePath)
    {
        _ = comms.SendFileToQuest(filePath);
    }

    internal void OnFilePrompt()
    {
        Microsoft.Win32.OpenFileDialog openFileDialog = new();
        openFileDialog.Title = "Open File...";
        openFileDialog.Filter = "3D Files (*.obj;*.glb;*.gltf;*.fbx;*.stl;*.ply;*.3mf;*.dae;*.png;*.jpg;*.hdr)|*.obj;*.glb;*.gltf;*.fbx;*.stl;*.ply;*.3mf;*.dae;*.png;*.jpg;*.hdr";
        if (openFileDialog.ShowDialog() == true)
        {
            string filePath = openFileDialog.FileName;
            _ = comms.SendFileToQuest(filePath);
        }
    }
}
