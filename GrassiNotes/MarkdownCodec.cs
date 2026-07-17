using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace GrassiNotes;

public static class MarkdownCodec
{
	private static readonly Regex Ordered = new Regex("^\\s*\\d+[.)]\\s+(.*)$", RegexOptions.Compiled);

	private static readonly Regex Bullet = new Regex("^\\s*[-+*]\\s+(.*)$", RegexOptions.Compiled);

	private static readonly Regex Heading = new Regex("^(#{1,6})\\s+(.*)$", RegexOptions.Compiled);

	public static FlowDocument Parse(string markdown, FontFamily font)
	{
		FlowDocument flowDocument = new FlowDocument
		{
			PagePadding = new Thickness(0.0),
			FontFamily = font
		};
		string[] array = markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		int i = 0;
		while (i < array.Length)
		{
			string text = array[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				i++;
				continue;
			}
			if (text.TrimStart().StartsWith("```", StringComparison.Ordinal))
			{
				string text2 = ((text.Trim().Length > 3) ? text.Trim()[3..].Trim() : "");
				i++;
				StringBuilder stringBuilder = new StringBuilder();
				while (i < array.Length && !array[i].TrimStart().StartsWith("```", StringComparison.Ordinal))
				{
					if (stringBuilder.Length > 0)
					{
						stringBuilder.AppendLine();
					}
					stringBuilder.Append(array[i++]);
				}
				if (i < array.Length)
				{
					i++;
				}
				Paragraph item = new Paragraph(new Run(stringBuilder.ToString()))
				{
					Tag = (string.IsNullOrEmpty(text2) ? "code" : ("code:" + text2)),
					FontFamily = new FontFamily("Cascadia Mono, Consolas"),
					FontSize = 13.0,
					Background = new SolidColorBrush(Color.FromArgb(24, 127, 127, 127)),
					Padding = new Thickness(12.0),
					Margin = new Thickness(0.0, 6.0, 0.0, 10.0)
				};
				flowDocument.Blocks.Add(item);
				continue;
			}
			Match match = Heading.Match(text);
			if (match.Success)
			{
				int length = match.Groups[1].Value.Length;
				Paragraph paragraph = new Paragraph
				{
					Tag = "h" + length,
					Margin = HeadingMargin(length)
				};
				ApplyHeadingStyle(paragraph, length);
				AddInlines(paragraph.Inlines, match.Groups[2].Value);
				flowDocument.Blocks.Add(paragraph);
				i++;
			}
			else if (IsRule(text))
			{
				flowDocument.Blocks.Add(new BlockUIContainer(new Border
				{
					Height = 1.0,
					Background = new SolidColorBrush(Color.FromArgb(70, 127, 127, 127)),
					Margin = new Thickness(0.0, 10.0, 0.0, 10.0)
				})
				{
					Tag = "hr"
				});
				i++;
			}
			else if (Bullet.IsMatch(text))
			{
				List list = new List
				{
					MarkerStyle = TextMarkerStyle.Disc,
					Margin = new Thickness(18.0, 3.0, 0.0, 8.0),
					Tag = "ul"
				};
				for (; i < array.Length; i++)
				{
					Match match2 = Bullet.Match(array[i]);
					if (!match2.Success)
					{
						break;
					}
					Paragraph paragraph2 = new Paragraph
					{
						Margin = new Thickness(0.0, 0.0, 0.0, 3.0)
					};
					AddInlines(paragraph2.Inlines, match2.Groups[1].Value);
					list.ListItems.Add(new ListItem(paragraph2));
				}
				flowDocument.Blocks.Add(list);
			}
			else if (Ordered.IsMatch(text))
			{
				List list2 = new List
				{
					MarkerStyle = TextMarkerStyle.Decimal,
					Margin = new Thickness(22.0, 3.0, 0.0, 8.0),
					StartIndex = 1,
					Tag = "ol"
				};
				for (; i < array.Length; i++)
				{
					Match match3 = Ordered.Match(array[i]);
					if (!match3.Success)
					{
						break;
					}
					Paragraph paragraph3 = new Paragraph
					{
						Margin = new Thickness(0.0, 0.0, 0.0, 3.0)
					};
					AddInlines(paragraph3.Inlines, match3.Groups[1].Value);
					list2.ListItems.Add(new ListItem(paragraph3));
				}
				flowDocument.Blocks.Add(list2);
			}
			else if (text.TrimStart().StartsWith(">", StringComparison.Ordinal))
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				for (; i < array.Length && array[i].TrimStart().StartsWith(">", StringComparison.Ordinal); i++)
				{
					if (stringBuilder2.Length > 0)
					{
						stringBuilder2.AppendLine();
					}
					stringBuilder2.Append(array[i].TrimStart()[1..].TrimStart());
				}
				Paragraph paragraph4 = new Paragraph
				{
					Tag = "quote",
					Margin = new Thickness(14.0, 5.0, 0.0, 10.0),
					Padding = new Thickness(10.0, 2.0, 0.0, 2.0),
					BorderThickness = new Thickness(3.0, 0.0, 0.0, 0.0),
					BorderBrush = new SolidColorBrush(Color.FromRgb(139, 92, 246))
				};
				AddInlines(paragraph4.Inlines, stringBuilder2.ToString());
				flowDocument.Blocks.Add(paragraph4);
			}
			else
			{
				StringBuilder stringBuilder3 = new StringBuilder(text.TrimEnd());
				for (i++; i < array.Length && !string.IsNullOrWhiteSpace(array[i]) && !IsBlockStart(array[i]); i++)
				{
					stringBuilder3.Append(' ').Append(array[i].Trim());
				}
				Paragraph paragraph5 = new Paragraph
				{
					Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
				};
				AddInlines(paragraph5.Inlines, stringBuilder3.ToString());
				flowDocument.Blocks.Add(paragraph5);
			}
		}
		if (flowDocument.Blocks.Count == 0)
		{
			flowDocument.Blocks.Add(new Paragraph());
		}
		return flowDocument;
	}

	private static bool IsBlockStart(string s)
	{
		if (!Heading.IsMatch(s) && !Bullet.IsMatch(s) && !Ordered.IsMatch(s) && !s.TrimStart().StartsWith("```", StringComparison.Ordinal) && !s.TrimStart().StartsWith(">", StringComparison.Ordinal))
		{
			return IsRule(s);
		}
		return true;
	}

	private static bool IsRule(string s)
	{
		string text = s.Trim();
		if (text.Length >= 3)
		{
			if (!text.All((char c) => c == '-') && !text.All((char c) => c == '*'))
			{
				return text.All((char c) => c == '_');
			}
			return true;
		}
		return false;
	}

	private static void AddInlines(InlineCollection target, string text)
	{
		int num = 0;
		while (num < text.Length)
		{
			if (TryDelimited(text, num, "***", out var end))
			{
				Bold bold = new Bold();
				Italic italic = new Italic();
				AddInlines(italic.Inlines, text[(num + 3)..end]);
				bold.Inlines.Add(italic);
				target.Add(bold);
				num = end + 3;
				continue;
			}
			if (TryDelimited(text, num, "**", out var end2))
			{
				Bold bold2 = new Bold();
				AddInlines(bold2.Inlines, text[(num + 2)..end2]);
				target.Add(bold2);
				num = end2 + 2;
				continue;
			}
			if (text[num] == '*' && TryDelimited(text, num, "*", out var end3))
			{
				Italic italic2 = new Italic();
				AddInlines(italic2.Inlines, text[(num + 1)..end3]);
				target.Add(italic2);
				num = end3 + 1;
				continue;
			}
			if (text[num] == '`')
			{
				int num2 = text.IndexOf('`', num + 1);
				if (num2 > num + 1)
				{
					int num3 = num + 1;
					int length = num2 - num3;
					target.Add(new Run(text.Substring(num3, length))
					{
						FontFamily = new FontFamily("Cascadia Mono, Consolas"),
						Background = new SolidColorBrush(Color.FromArgb(28, 127, 127, 127)),
						Tag = "code"
					});
					num = num2 + 1;
					continue;
				}
			}
			if (text[num] == '[')
			{
				int num4 = text.IndexOf(']', num + 1);
				if (num4 > num && num4 + 1 < text.Length && text[num4 + 1] == '(')
				{
					int num5 = text.IndexOf(')', num4 + 2);
					if (num5 > num4 + 2 && Uri.TryCreate(text[(num4 + 2)..num5], UriKind.RelativeOrAbsolute, out Uri result))
					{
						int num3 = num + 1;
						int length = num4 - num3;
						Hyperlink item = new Hyperlink(new Run(text.Substring(num3, length)))
						{
							NavigateUri = result,
							Tag = "link"
						};
						target.Add(item);
						num = num5 + 1;
						continue;
					}
				}
			}
			int num6 = FindNextSpecial(text, num + 1);
			target.Add(new Run(text[num..num6]));
			num = num6;
		}
	}

	private static bool TryDelimited(string s, int start, string token, out int end)
	{
		end = -1;
		if (!MemoryExtensions.StartsWith(MemoryExtensions.AsSpan(s, start), (ReadOnlySpan<char>)token, StringComparison.Ordinal))
		{
			return false;
		}
		end = s.IndexOf(token, start + token.Length, StringComparison.Ordinal);
		return end > start + token.Length;
	}

	private static int FindNextSpecial(string s, int start)
	{
		for (int i = start; i < s.Length; i++)
		{
			if (s[i] == '*' || s[i] == '`' || s[i] == '[')
			{
				return i;
			}
		}
		return s.Length;
	}

	public static string Export(FlowDocument doc)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Block block in doc.Blocks)
		{
			ExportBlock(block, stringBuilder, 0);
		}
		return stringBuilder.ToString().TrimEnd() + Environment.NewLine;
	}

	private static void ExportBlock(Block block, StringBuilder sb, int depth)
	{
		if (!(block is Paragraph { Tag: var tag } paragraph))
		{
			if (!(block is List { StartIndex: var num } list))
			{
				if (!(block is BlockUIContainer blockUIContainer))
				{
					if (!(block is Section section))
					{
						return;
					}
					{
						foreach (Block block2 in section.Blocks)
						{
							ExportBlock(block2, sb, depth);
						}
						return;
					}
				}
				if (object.Equals(blockUIContainer.Tag, "hr"))
				{
					sb.AppendLine("---").AppendLine();
				}
				return;
			}
			foreach (ListItem listItem in list.ListItems)
			{
				object obj;
				if (list.MarkerStyle != TextMarkerStyle.Decimal)
				{
					obj = "- ";
				}
				else
				{
					int num2 = num;
					num = num2 + 1;
					int num3 = num2;
					obj = num3.ToString(CultureInfo.InvariantCulture) + ". ";
				}
				string text = (string)obj;
				bool flag = true;
				foreach (Block block3 in listItem.Blocks)
				{
					if (block3 is Paragraph paragraph2)
					{
						sb.Append(new string(' ', depth * 2)).Append(flag ? text : "  ").Append(ExportInlines(paragraph2.Inlines))
							.AppendLine();
						flag = false;
					}
					else
					{
						ExportBlock(block3, sb, depth + 1);
					}
				}
			}
			sb.AppendLine();
		}
		else
		{
			string text2 = tag?.ToString() ?? "";
			if (text2.StartsWith("h") && int.TryParse(text2[1..], out var result))
			{
				sb.Append(new string('#', Math.Clamp(result, 1, 6))).Append(' ');
			}
			else if (text2 == "quote")
			{
				sb.Append("> ");
			}
			else if (text2.StartsWith("code"))
			{
				string value = (text2.Contains(':') ? text2[(text2.IndexOf(':') + 1)..] : "");
				sb.Append("```").AppendLine(value).Append(new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text.TrimEnd('\r', '\n'))
					.AppendLine()
					.AppendLine("```");
				return;
			}
			sb.Append(ExportInlines(paragraph.Inlines)).AppendLine().AppendLine();
		}
	}

	private static string ExportInlines(InlineCollection inlines)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Inline inline in inlines)
		{
			ExportInline(inline, stringBuilder);
		}
		return stringBuilder.ToString();
	}

	private static void ExportInline(Inline inline, StringBuilder sb)
	{
		if (!(inline is LineBreak))
		{
			if (!(inline is Hyperlink hyperlink))
			{
				if (!(inline is Bold bold))
				{
					if (!(inline is Italic italic))
					{
						if (!(inline is Span span))
						{
							if (!(inline is Run run))
							{
								return;
							}
							string value = Escape(run.Text ?? "");
							if (!object.Equals(run.Tag, "code"))
							{
								FontFamily fontFamily = run.FontFamily;
								if (fontFamily == null || !fontFamily.Source.Contains("Consolas", StringComparison.OrdinalIgnoreCase))
								{
									FontFamily fontFamily2 = run.FontFamily;
									if (fontFamily2 == null || !fontFamily2.Source.Contains("Cascadia", StringComparison.OrdinalIgnoreCase))
									{
										bool flag = run.FontWeight >= FontWeights.SemiBold;
										bool flag2 = run.FontStyle == FontStyles.Italic;
										if (flag && flag2)
										{
											sb.Append("***").Append(value).Append("***");
										}
										else if (flag)
										{
											sb.Append("**").Append(value).Append("**");
										}
										else if (flag2)
										{
											sb.Append('*').Append(value).Append('*');
										}
										else
										{
											sb.Append(value);
										}
										return;
									}
								}
							}
							sb.Append('`').Append((run.Text ?? "").Replace("`", "\\`")).Append('`');
							return;
						}
						{
							foreach (Inline inline2 in span.Inlines)
							{
								ExportInline(inline2, sb);
							}
							return;
						}
					}
					sb.Append('*');
					foreach (Inline inline3 in italic.Inlines)
					{
						ExportInline(inline3, sb);
					}
					sb.Append('*');
					return;
				}
				sb.Append("**");
				foreach (Inline inline4 in bold.Inlines)
				{
					ExportInline(inline4, sb);
				}
				sb.Append("**");
				return;
			}
			sb.Append('[');
			foreach (Inline inline5 in hyperlink.Inlines)
			{
				ExportInline(inline5, sb);
			}
			sb.Append("](").Append(hyperlink.NavigateUri?.ToString() ?? "").Append(')');
		}
		else
		{
			sb.Append("  \n");
		}
	}

	private static string Escape(string s)
	{
		return s.Replace("\\", "\\\\").Replace("*", "\\*").Replace("_", "\\_")
			.Replace("[", "\\[");
	}

	public static void ApplyHeadingStyle(Paragraph p, int level)
	{
		p.FontWeight = ((level <= 2) ? FontWeights.Bold : FontWeights.SemiBold);
		p.FontSize = level switch
		{
			1 => 28.0, 
			2 => 23.0, 
			3 => 19.0, 
			4 => 17.0, 
			5 => 15.5, 
			_ => 14.5, 
		};
	}

	private static Thickness HeadingMargin(int level)
	{
		return level switch
		{
			1 => new Thickness(0.0, 16.0, 0.0, 8.0), 
			2 => new Thickness(0.0, 14.0, 0.0, 7.0), 
			_ => new Thickness(0.0, 11.0, 0.0, 6.0), 
		};
	}
}
