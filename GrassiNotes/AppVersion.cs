using System.Reflection;

namespace GrassiNotes;

internal static class AppVersion
{
	public static string Current { get; } =
		Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion
			.Split('+')[0] ?? "0.0.0";

	public static string WindowTitle(string? documentTitle)
	{
		string product = $"GrassiNotes v. {Current}";
		return string.IsNullOrWhiteSpace(documentTitle)
			? product
			: $"{documentTitle} \u2014 {product}";
	}
}
