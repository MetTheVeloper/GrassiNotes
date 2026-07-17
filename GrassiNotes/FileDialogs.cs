using System.IO;
using Microsoft.Win32;

namespace GrassiNotes;

public static class FileDialogs
{
	public static string? Open()
	{
		OpenFileDialog openFileDialog = new OpenFileDialog
		{
			Filter = "Supported documents|*.grass;*.rtf;*.md;*.markdown;*.txt;*.log;*.json;*.xml;*.csv|GrassiNotes documents|*.grass|Markdown|*.md;*.markdown|Rich Text|*.rtf|Plain text|*.txt;*.log;*.json;*.xml;*.csv|All files|*.*",
			CheckFileExists = true,
			Multiselect = false
		};
		if (openFileDialog.ShowDialog() != true)
		{
			return null;
		}
		return openFileDialog.FileName;
	}

	public static string? Save(string suggested)
	{
		SaveFileDialog saveFileDialog = new SaveFileDialog
		{
			FileName = suggested,
			AddExtension = true,
			DefaultExt = ".grass",
			Filter = "GrassiNotes document (*.grass)|*.grass|Markdown (*.md)|*.md|Rich Text (*.rtf)|*.rtf|Plain text (*.txt)|*.txt"
		};
		if (saveFileDialog.ShowDialog() != true)
		{
			return null;
		}
		return saveFileDialog.FileName;
	}

	public static DocumentKind KindFromPath(string path)
	{
		return Path.GetExtension(path).ToLowerInvariant() switch
		{
			".grass" => DocumentKind.Native, 
			".rtf" => DocumentKind.RichText, 
			".md" => DocumentKind.Markdown, 
			".markdown" => DocumentKind.Markdown, 
			_ => DocumentKind.PlainText, 
		};
	}
}
