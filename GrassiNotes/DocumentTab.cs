using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Documents;

namespace GrassiNotes;

public sealed class DocumentTab : INotifyPropertyChanged
{
    private string _title = "Untitled";
    private string? _filePath;
    private bool _isDirty;
    private bool _isActive;

    public Guid Id { get; set; } = Guid.NewGuid();
    public FlowDocument Document { get; set; } = new();
    public DocumentKind Kind { get; set; }
    public double Zoom { get; set; } = 1.0;

    public string Title
    {
        get => _title;
        set { if (_title != value) { _title = value; Notify(); Notify(nameof(DisplayTitle)); } }
    }

    public string? FilePath
    {
        get => _filePath;
        set { if (_filePath != value) { _filePath = value; Notify(); } }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set { if (_isDirty != value) { _isDirty = value; Notify(); Notify(nameof(DisplayTitle)); } }
    }

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; Notify(); } }
    }

    public string DisplayTitle => IsDirty ? Title + " •" : Title;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
