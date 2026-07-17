using System.IO;
using System.Text;
using System.Text.Json;

namespace GrassiNotes;

public static class SessionService
{
	private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
	{
		WriteIndented = false
	};

	public static SessionState? Load()
	{
		try
		{
			return File.Exists(AppPaths.Session) ? JsonSerializer.Deserialize<SessionState>(File.ReadAllText(AppPaths.Session), Options) : null;
		}
		catch
		{
			return null;
		}
	}

	public static void Save(SessionState state)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		try
		{
			Directory.CreateDirectory(AppPaths.Root);
			File.WriteAllText(AppPaths.Session, JsonSerializer.Serialize(state, Options), (Encoding)new UTF8Encoding(false));
		}
		catch
		{
		}
	}
}
