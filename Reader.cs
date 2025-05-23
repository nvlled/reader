using System;
using System.Collections.Generic;
using System.Linq;
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
	[Export]
	public bool AutoPaste = false;

	BaseButton pasteButton;
	TokenizedRichTextLabel editor;
	TokenizedRichTextLabel smallEditor;
	Panel helpPanel;
	Button closeHelpButton;
	Button showHelpButton;

	List<Token> tokens = new();


	IndexRange tokenRange;
	int numCharPerBatch = 15;

	IndexHistory indexHistory = new IndexHistory();

	public override void _Ready()
	{
		this.FetchNode(out pasteButton, "%PasteButton");
		this.FetchNode(out editor, "%Editor");
		this.FetchNode(out smallEditor, "%SmallEditor");
		this.FetchNode(out helpPanel, "%HelpPanel");
		this.FetchNode(out showHelpButton, "%ShowHelpButton");
		this.FetchNode(out closeHelpButton, "%CloseHelpButton");

		pasteButton.Pressed += onPastePressed;
		editor.TokenClicked += onTokenClicked;
		closeHelpButton.Pressed += () => helpPanel.Visible = false;
		showHelpButton.Pressed += () => helpPanel.Visible = true;

		if (DisplayServer.ClipboardHas() && AutoPaste)
		{
			SetText(DisplayServer.ClipboardGet());
		}
		else
		{
			SetText(sampleText);
		}
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
			smallEditor.Clear();
			SetText(DisplayServer.ClipboardGet());
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
	string sampleText = @"
You will rejoice to hear that no disaster has accompanied the commencement of an enterprise which you have regarded with such evil forebodings. I arrived here yesterday, and my first task is to assure my dear sister of my welfare and increasing confidence in the success of my undertaking.

I am already far north of London, and as I walk in the streets of Petersburgh, I feel a cold northern breeze play upon my cheeks, which braces my nerves and fills me with delight. Do you understand this feeling? This breeze, which has travelled from the regions towards which I am advancing, gives me a foretaste of those icy climes.  Inspirited by this wind of promise, my daydreams become more fervent and vivid. I try in vain to be persuaded that the pole is the seat of frost and desolation; it ever presents itself to my imagination as the region of beauty and delight. There, Margaret, the sun is for ever visible, its broad disk just skirting the horizon and diffusing a perpetual splendour. There—for with your leave, my sister, I will put some trust in preceding navigators—there snow and frost are banished; and, sailing over a calm sea, we may be wafted to a land surpassing in wonders and in beauty every region hitherto discovered on the habitable globe. Its productions and features may be without example, as the phenomena of the heavenly bodies undoubtedly are in those undiscovered solitudes. What may not be expected in a country of eternal light? I may there discover the wondrous power which attracts the needle and may regulate a thousand celestial observations that require only this voyage to render their seeming eccentricities consistent for ever. I shall satiate my ardent curiosity with the sight of a part of the world never before visited, and may tread a land never before imprinted by the foot of man. These are my enticements, and they are sufficient to conquer all fear of danger or death and to induce me to commence this laborious voyage with the joy a child feels when he embarks in a little boat, with his holiday mates, on an expedition of discovery up his native river. But supposing all these conjectures to be false, you cannot contest the inestimable benefit which I shall confer on all mankind, to the last generation, by discovering a passage near the pole to those countries, to reach which at present so many months are requisite; or by ascertaining the secret of the magnet, which, if at all possible, can only be effected by an undertaking such as mine.

	";
}


