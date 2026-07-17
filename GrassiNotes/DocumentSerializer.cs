using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;

namespace GrassiNotes;

public static class DocumentSerializer
{
	public static string ToXaml(FlowDocument document)
	{
		return XamlWriter.Save(document);
	}

	public static FlowDocument FromXaml(string xaml)
	{
		try
		{
			return (FlowDocument)XamlReader.Parse(xaml);
		}
		catch
		{
			return NewDocument("");
		}
	}

	public static FlowDocument NewDocument(string text)
	{
		return new FlowDocument
		{
			PagePadding = new Thickness(0.0),
			LineHeight = double.NaN,
			Blocks = { (Block)new Paragraph(new Run(text))
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			} }
		};
	}

	public static string PlainText(FlowDocument doc)
	{
		return new TextRange(doc.ContentStart, doc.ContentEnd).Text.TrimEnd('\r', '\n');
	}
}
