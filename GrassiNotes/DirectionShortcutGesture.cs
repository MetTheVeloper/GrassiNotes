using System.Collections.Generic;
using System.Windows.Input;

namespace GrassiNotes;

internal readonly record struct DirectionShortcutKeyUpResult(
	bool Handled,
	ParagraphDirection? Direction);

internal sealed class DirectionShortcutGesture
{
	private readonly HashSet<Key> _pressedControlKeys = new();
	private readonly HashSet<Key> _pressedShiftKeys = new();
	private Key? _directionShift;
	private bool _armed;
	private bool _cancelled;

	public bool OnKeyDown(Key key, ModifierKeys modifiers, bool isRepeat = false)
	{
		if (key is Key.LeftCtrl or Key.RightCtrl)
		{
			_pressedControlKeys.Add(key);
			TryArm(modifiers);
			return _armed;
		}

		if (key is Key.LeftShift or Key.RightShift)
		{
			_pressedShiftKeys.Add(key);
			TryArm(modifiers);
			return _armed;
		}

		if (_armed && !isRepeat)
			_cancelled = true;

		return false;
	}

	public DirectionShortcutKeyUpResult OnKeyUp(
		Key key,
		ModifierKeys modifiers)
	{
		bool chordKey = key is Key.LeftCtrl or Key.RightCtrl or
			Key.LeftShift or Key.RightShift;
		if (!chordKey)
			return default;

		if (key is Key.LeftCtrl or Key.RightCtrl)
			_pressedControlKeys.Remove(key);
		else
			_pressedShiftKeys.Remove(key);

		if (!_armed)
			return default;

		if (_pressedControlKeys.Count != 0 || _pressedShiftKeys.Count != 0)
			return new DirectionShortcutKeyUpResult(true, null);

		ParagraphDirection? direction = !_cancelled &&
			_directionShift is Key shift
				? shift == Key.RightShift
					? ParagraphDirection.RightToLeft
					: ParagraphDirection.LeftToRight
				: null;

		Reset();
		return new DirectionShortcutKeyUpResult(true, direction);
	}

	public void Reset()
	{
		_pressedControlKeys.Clear();
		_pressedShiftKeys.Clear();
		_directionShift = null;
		_armed = false;
		_cancelled = false;
	}

	private void TryArm(ModifierKeys modifiers)
	{
		if (_armed ||
			_pressedControlKeys.Count == 0 ||
			_pressedShiftKeys.Count == 0 ||
			(modifiers & (ModifierKeys.Alt | ModifierKeys.Windows)) != 0)
		{
			return;
		}

		_directionShift = _pressedShiftKeys.Contains(Key.RightShift)
			? Key.RightShift
			: Key.LeftShift;
		_armed = true;
		_cancelled = false;
	}
}
