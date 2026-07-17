using System;

namespace GrassiNotes;

public sealed class SessionDocument
{
	public Guid Id { get; set; }

	public string Title { get; set; } = "Untitled";


	public string? FilePath { get; set; }

	public bool IsDirty { get; set; }

	public DocumentKind Kind { get; set; }

	public double Zoom { get; set; } = 1.0;


	public string Xaml { get; set; } = "";

}
