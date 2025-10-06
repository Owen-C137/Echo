using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Echo.Models
{
    public class GdtFileItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public int EstimatedAssets { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedFormatted { get; set; } = string.Empty;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
