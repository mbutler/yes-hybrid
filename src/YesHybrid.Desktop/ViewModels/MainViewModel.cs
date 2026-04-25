using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace YesHybrid.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string _statusText = "";
    private string _errorText = "";
    private bool _errorVisible;
    private string _resultText = "Ongoing";

    public string StatusText
    {
        get => _statusText;
        set { if (_statusText == value) return; _statusText = value; OnPropertyChanged(); }
    }

    public string ErrorText
    {
        get => _errorText;
        set
        {
            if (_errorText == value) return;
            _errorText = value;
            ErrorVisible = !string.IsNullOrEmpty(_errorText);
            OnPropertyChanged();
        }
    }

    public bool ErrorVisible
    {
        get => _errorVisible;
        private set { if (_errorVisible == value) return; _errorVisible = value; OnPropertyChanged(); }
    }

    public string ResultText
    {
        get => _resultText;
        set { if (_resultText == value) return; _resultText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> HistoryLines { get; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RebuildHistory(IReadOnlyList<string> lines)
    {
        HistoryLines.Clear();
        foreach (var l in lines) HistoryLines.Add(l);
    }

    void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
