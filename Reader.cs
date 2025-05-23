using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Godot;

public struct IndexHistory()
{
	int?[] indices = new int?[1024];
	int current = 0;

	public void Clear()
	{
		for (var i = 0; i < indices.Length; i++) indices[i] = null;
		current = 0;
	}

	public void Save(int value)
	{
		indices[current] = value;
		current = (current + 1) % indices.Length;
	}
	public int? Undo()
	{
		current--;
		if (current < 0) current = indices.Length - 1;
		return indices[current];
	}
	public int? Restore()
	{
		current++;
		if (current >= indices.Length) current = 0;
		return indices[current];
	}
}

public partial class Reader : Control
{
	const string CLIPBOARD_FILENAME = "user://clipboard.txt";

	[Export]
	public bool AutoPaste = false;

	BaseButton pasteButton;
	TokenizedRichTextLabel editor;
	TokenizedRichTextLabel smallEditor;
	Panel helpPanel;
	Button closeHelpButton;
	Button showHelpButton;
	CheckButton autoPasteButton;
	Button joinLinesButton;

	List<Token> tokens = new();

	string pastedText = "";


	IndexRange tokenRange;
	int numCharPerBatch = 15;

	IndexHistory indexHistory = new IndexHistory();


	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest && pastedText.Length > 0)
		{
			using var file = FileAccess.Open(CLIPBOARD_FILENAME, FileAccess.ModeFlags.Write);
			file.StoreString(pastedText);
			GetTree().Quit();
		}
	}

	public override void _Ready()
	{
		this.FetchNode(out pasteButton, "%PasteButton");
		this.FetchNode(out editor, "%Editor");
		this.FetchNode(out smallEditor, "%SmallEditor");
		this.FetchNode(out helpPanel, "%HelpPanel");
		this.FetchNode(out showHelpButton, "%ShowHelpButton");
		this.FetchNode(out closeHelpButton, "%CloseHelpButton");
		this.FetchNode(out autoPasteButton, "%AutoPasteToggle");
		this.FetchNode(out joinLinesButton, "%JoinLinesButton");

		GetWindow().FocusEntered += onWindowFocus;

		pasteButton.Pressed += onPastePressed;
		editor.TokenClicked += onTokenClicked;
		closeHelpButton.Pressed += () => helpPanel.Visible = false;
		showHelpButton.Pressed += () => helpPanel.Visible = !helpPanel.Visible;
		autoPasteButton.Toggled += value => AutoPaste = value;
		joinLinesButton.Pressed += onJoinLines;

		using var file = FileAccess.Open(CLIPBOARD_FILENAME, FileAccess.ModeFlags.Read);
		if (file is not null)
		{
			var text = file.GetAsText();
			SetText(text);
		}
		else if (DisplayServer.ClipboardHas() && AutoPaste)
		{
			SetText(DisplayServer.ClipboardGet());
		}
		else
		{
			SetText(sampleText);
		}
	}

	private void onJoinLines()
	{
		if (!DisplayServer.ClipboardHas()) return;
		var text = DisplayServer.ClipboardGet();
		var buf = new StringBuilder();
		var skip = true;
		foreach (var (ch, i) in text.WithIndex())
		{
			if (i >= text.Length - 1 || ch != '\n' || skip)
			{
				skip = false;
				buf.Append(ch);
				continue;
			}

			if (text[i + 1] == '\n')
			{
				buf.Append(ch);
				skip = true;
			}
			else
			{
				buf.Append(' ');
			}
		}
		text = buf.ToString();
		SetText(text);
	}

	private void onWindowFocus()
	{
		if (AutoPaste) onPastePressed();
	}

	private void onTokenClicked(int tokenIndex)
	{
		indexHistory.Clear();
		selectIndex(tokenIndex);
	}

	private void onPastePressed()
	{
		if (DisplayServer.ClipboardHas())
		{
			var text = DisplayServer.ClipboardGet();
			if (text == pastedText) return;
			pastedText = text;
			smallEditor.Clear();
			SetText(text);
			scrollToCurrentLine();
			pasteButton.ReleaseFocus();
		}
	}


	private void scrollToCurrentLine()
	{
		var i = tokenRange.Item1;
		editor.ScrollToLine(editor.GetCharacterLine(tokens[i].Pos) - 1);
	}

	public override void _Input(InputEvent @event)
	{
		var (start, end) = tokenRange;
		if (@event.IsActionPressed("ui_accept") || @event.IsActionPressed("ui_right"))
		{
			indexHistory.Save(start);
			selectIndex(end + 1);
			scrollToCurrentLine();
		}
		else if (@event.IsActionPressed("ui_text_backspace") || @event.IsActionPressed("ui_left"))
		{
			var index = indexHistory.Undo();
			if (index.HasValue)
			{
				selectIndex(index.Value);
				scrollToCurrentLine();
			}
		}
		else if (@event.IsActionPressed("ui_left"))
		{
			selectIndex(start - 1, end);
			scrollToCurrentLine();
		}
		else if (@event.IsActionPressed("ui_right"))
		{
			selectIndex(start, end + 1);
			scrollToCurrentLine();
		}

		@event.Dispose();
	}

	private void selectIndex(int start, int end)
	{
		start = Math.Max(start, 0);
		end = Math.Min(end, tokens.Count - 1);

		editor.Highlight(start, end);
		tokenRange = (start, end);
		editor.ScrollToLine(editor.GetCharacterLine(tokens[start].Pos) - 1);
		smallEditor.SetContents(tokens.Slice(start, end - start + 1).ToList().AsReadOnly());
	}

	private void selectIndex(int start)
	{
		while (tokens.GetTypeAt(start) == CharType.Space)
		{
			start++;
		}

		if (start < 0 || start >= tokens.Count) return;


		var charCount = 0;
		var index = start;
		while (true)
		{
			var token = tokens[index];
			charCount += token.CharCount();
			var (_, paraBreaks) = token.CountSpaces();


			if (charCount > numCharPerBatch && token.Type != CharType.LetterOrNumber)
			{
				if (tokens.GetTypeAt(index) == CharType.Punctuation &&
				   tokens.GetTypeAt(index + 1) == CharType.LetterOrNumber)
				{
					index++;
				}
				break;
			}
			if (paraBreaks > 0 || index >= tokens.Count - 1)
			{
				break;
			}
			index++;
		}

		editor.Highlight(start, index);
		tokenRange = (start, index);
		smallEditor.SetContents(tokens.Slice(start, index - start + 1).ToList().AsReadOnly());
	}


	private void SetText(string text)
	{
		text += new string('\n', 10);
		tokens = Token.Scan(text).ToList();
		editor.SetContents(tokens.AsReadOnly());
		selectIndex(0);
	}


	// Excerpt from the book "Frankenstein"
	string sampleText =
"""
Some text here.
""";
}


