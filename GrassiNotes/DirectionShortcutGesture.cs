using System.Windows.Input;

namespace GrassiNotes;

internal readonly record struct DirectionShortcutKeyUpResult(
	bool Handled,
	ParagraphDirection? Direction);

internal sealed class DirectionShortcutGesture
{
	private Key? _armedShift;
	private bool _cancelled;

	public bool OnKeyDown(Key key, ModifierKeys modifiers, bool isRepeat = false)
	{
		if (_armedShift is Key armed)
		{
			if (key == armed)
				return true;

			if (key is Key.LeftCtrl or Key.RightCtrl)
				return false;

			_cancelled = true;
			return false;
		}

		if (!isRepeat &&
			key is Key.LeftShift or Key.RightShift &&
			(modifiers & ModifierKeys.Control) != 0 &&
			(modifiers & ModifierKeys.Alt) == 0)
		{
			_armedShift = key;
			_cancelled = false;
			return true;
		}

		return false;
	}

	public DirectionShortcutKeyUpResult OnKeyUp(
		Key key,
		ModifierKeys modifiers)
	{
		if (_armedShift is not Key armed)
			return default;

		if (key is Key.LeftCtrl or Key.RightCtrl)
		{
			_cancelled = true;
			return default;
		}

		if (key != armed)
			return default;

		ParagraphDirection? direction = !_cancelled &&
			(modifiers & ModifierKeys.Control) != 0
				? armed == Key.RightShift
					? ParagraphDirection.RightToLeft
					: ParagraphDirection.LeftToRight
				: null;

		Reset();
		return new DirectionShortcutKeyUpResult(true, direction);
	}

	public void Reset()
	{
		_armedShift = null;
		_cancelled = false;
	}
}
