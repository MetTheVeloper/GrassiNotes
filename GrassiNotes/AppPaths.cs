using System;
using System.IO;

namespace GrassiNotes;

public static class AppPaths
{
	public static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GrassiNotes");

	public static readonly string Session = Path.Combine(Root, "session-v2.json");

	public static readonly string Fonts = Path.Combine(Root, "fonts-v2");
}
