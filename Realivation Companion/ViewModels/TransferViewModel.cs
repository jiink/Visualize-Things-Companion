namespace Realivation_Companion.ViewModels;

class TransferViewModel(Comms comms) : ViewModelBase
{
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
