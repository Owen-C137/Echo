using System.ComponentModel;
using System.Runtime.CompilerServices;
using Echo.Services;

namespace Echo.Models
{
    public class AssetViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public bool Exists { get; set; }
        public string StatusText => Exists ? "✓ Found" : "❌ Missing";
        public string StatusIcon => Exists ? "✓" : "❌";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        // Store original asset reference for package creation
        public ScannedAsset? ScannedAssetRef { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
