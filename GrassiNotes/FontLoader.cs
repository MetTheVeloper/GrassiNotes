using System;
using System.IO;
using System.Windows.Media;

namespace GrassiNotes;

public static class FontLoader
{
	private static readonly (byte[] Bytes, string File)[] Fonts = new(byte[], string)[3]
	{
		(EmbeddedAssets.VazirRegular, "Vazirmatn-Regular.ttf"),
		(EmbeddedAssets.VazirSemiBold, "Vazirmatn-SemiBold.ttf"),
		(EmbeddedAssets.VazirBold, "Vazirmatn-Bold.ttf")
	};

	public static FontFamily Load()
	{
		try
		{
			Directory.CreateDirectory(AppPaths.Fonts);
			(byte[], string)[] fonts = Fonts;
			for (int i = 0; i < fonts.Length; i++)
			{
				(byte[], string) tuple = fonts[i];
				byte[] item = tuple.Item1;
				string item2 = tuple.Item2;
				string text = Path.Combine(AppPaths.Fonts, item2);
				if (!File.Exists(text) || new FileInfo(text).Length != item.Length)
				{
					File.WriteAllBytes(text, item);
				}
			}
			return new FontFamily(new Uri(AppPaths.Fonts.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, UriKind.Absolute), "./#Vazirmatn");
		}
		catch
		{
			return new FontFamily("Vazirmatn, Segoe UI Variable, Segoe UI");
		}
	}
}
