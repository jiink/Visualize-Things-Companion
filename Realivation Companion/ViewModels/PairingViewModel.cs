using QRCoder;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Realivation_Companion.ViewModels;

class PairingViewModel : ViewModelBase
{
    private string _qrCodeText = "";
    private ImageSource? _qrCodeImage;
    private bool _qrSuccess = false;
    private string _messageTxt = "";

    public PairingViewModel()
    {
        string? ip = Comms.GetLocalIPAddress().ipAddr?.ToString();
        if (ip == null)
        {
            QrSuccess = false;
            MessageTxt = "Error: Couldn't figure out your network.";
        }
        else
        {
            QrSuccess = true;
            MessageTxt = "";
            QrCodeText = ip;
            GenerateQrCode();
        }
    }

    public string QrCodeText
    {
        get => _qrCodeText;
        set
        {
            _qrCodeText = value;
            OnPropertyChanged();
            GenerateQrCode();
        }
    }

    public ImageSource? QrCodeImage
    {
        get => _qrCodeImage;
        private set
        {
            _qrCodeImage = value;
            OnPropertyChanged();
        }
    }

    public bool QrSuccess
    {
        get => _qrSuccess;
        private set
        {
            _qrSuccess = value;
            OnPropertyChanged();
        }
    }

    public string MessageTxt
    {
        get => _messageTxt;
        private set
        {
            _messageTxt = value;
            OnPropertyChanged();
        }
    }

    private void GenerateQrCode()
    {
        if (string.IsNullOrEmpty(QrCodeText))
        {
            QrCodeImage = null;
            return;
        }
        try
        {
            QRCodeData qrCodeData = QRCodeGenerator.GenerateQrCode(QrCodeText, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsPngBytes = qrCode.GetGraphic(20);
            QrCodeImage = ToBitmapImage(qrCodeAsPngBytes);
        }
        catch
        {
            QrCodeImage = null;
        }
    }

    private static BitmapImage? ToBitmapImage(byte[] data)
    {
        if (data == null || data.Length == 0) return null;

        var image = new BitmapImage();
        using (var mem = new MemoryStream(data))
        {
            mem.Position = 0;
            image.BeginInit();
            image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = null;
            image.StreamSource = mem;
            image.EndInit();
        }
        image.Freeze();
        return image;
    }
}
