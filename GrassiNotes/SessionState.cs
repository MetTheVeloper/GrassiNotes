using System.Collections.Generic;

namespace GrassiNotes;

public sealed class SessionState
{
	public string Theme { get; set; } = "Dark";


	public bool AlwaysOnTop { get; set; }

	public int ActiveIndex { get; set; }

	public List<SessionDocument> Documents { get; set; } = new List<SessionDocument>();

}
