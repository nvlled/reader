global using IndexRange = System.ValueTuple<int, int>;

using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

public enum CharType
{
	Unknown,
	LetterOrNumber,
	Punctuation,
	Space,
}

public struct Token
{
	public CharType Type;
	public int Index;
	public int Pos;
	public int Line;
	public int Column;
	public string Value;

	public int EndPos
	{
		get => Pos + Value.Length;
	}

	public override string ToString()
	{
		return string.Format("i={0}, line={1}, col={2}, val={3}", Pos, Line, Column, Value);
	}

	public int CharCount()
	{
		switch (Type)
		{
			case CharType.LetterOrNumber:
			case CharType.Punctuation:
				return Value.Length;
			default:
				return 0;
		}
	}

	public (int, int) CountSpaces()
	{

		switch (Type)
		{
			case CharType.LetterOrNumber:
			case CharType.Punctuation:
				return (0, 0);
			default:
				var lineBreaks = Value.Count("\n");
				var paraBreaks = Value.Count("\n\n");
				return (lineBreaks, paraBreaks);
		}
	}


	public static CharType GetType(char ch)
	{
		if (char.IsLetter(ch)) return CharType.LetterOrNumber;
		if (char.IsNumber(ch)) return CharType.LetterOrNumber;
		if (char.IsPunctuation(ch)) return CharType.Punctuation;
		if (char.IsWhiteSpace(ch)) return CharType.Space;
		return CharType.Unknown;
	}

	public static IEnumerable<Token> Scan(string str)
	{
		if (str.Length == 0) yield break;

		var lastType = GetType(str[0]);
		var buf = new StringBuilder();
		var tokenIndex = 0;
		var startPos = 0;
		var pos = 0;
		var line = 0;
		var column = 0;
		foreach (var ch in str)
		{
			if (ch == '\n')
			{
				line++;
				column = 0;
			}

			var type = GetType(ch);
			if (type != lastType)
			{
				yield return new Token
				{
					Type = lastType,
					Index = tokenIndex,
					Pos = startPos,
					Line = line,
					Column = column,
					Value = buf.ToString(),
				};
				startPos = pos;
				tokenIndex++;
				buf.Clear();
			}
			buf.Append(ch);

			lastType = type;
			pos++;
		}

		if (buf.Length > 0)
		{
			yield return new Token
			{
				Type = lastType,
				Index = tokenIndex,
				Pos = pos,
				Line = line,
				Column = column,
				Value = buf.ToString(),
			};
		}

	}

}


public partial class TokenizedRichTextLabel : RichTextLabel
{
	[Signal]
	public delegate void TokenClickedEventHandler(int tokenIndex);

	[Export]
	public bool ShowWhiteSpaces = true;

	[Export]
	public bool AlternateColors = true;


	public Color HighlightColor = Colors.Yellow;

	(int, int) HighlightedPosRange;

	Highlighter effect;

	ReadOnlyCollection<Token> tokens;

	public override void _Ready()
	{
		effect = new Highlighter(this);
		MetaClicked += metaClicked;

	}

	private void metaClicked(Variant meta)
	{
		var index = (int)meta.AsInt64();
		EmitSignal(SignalName.TokenClicked, index);
	}

	public void Highlight(int start, int end)
	{
		// var (start, end) = range;
		end = Math.Min(end, tokens.Count - 1);
		HighlightedPosRange = (tokens[start].Pos, tokens[end].EndPos);
	}

	public void SetContents(ReadOnlyCollection<Token> tokens)
	{
		this.tokens = tokens;

		Clear();

		var colorIndex = 0;
		var colors = new Color[]{
			Colors.CadetBlue,
			Colors.OrangeRed,
		};

		PushCustomfx(effect, null);

		foreach (var token in tokens)
		{
			var tokenIndex = token.Index;
			var color = Colors.Transparent;
			switch (token.Type)
			{
				case CharType.Space:
					if (AlternateColors) colorIndex = (colorIndex + 1) % colors.Length;
					if (!ShowWhiteSpaces) continue;
					break;

				case CharType.LetterOrNumber:
					color = colors[colorIndex];
					break;

				case CharType.Punctuation:
					color = Colors.Red;
					break;
			}


			PushColor(color);
			PushMeta(Variant.CreateFrom(token.Index));
			AddText(token.Value);
			Pop(); // meta
			Pop(); // color

		}

	}

	public override void _Process(double delta)
	{
	}

	partial class Highlighter : RichTextEffect
	{
		public string bbcode = "blah";

		TokenizedRichTextLabel label;
		public Highlighter(TokenizedRichTextLabel label)
		{
			this.label = label;
		}

		public override bool _ProcessCustomFX(CharFXTransform charFX)
		{
			var (start, end) = label.HighlightedPosRange;
			if (start <= charFX.Range.X && charFX.Range.X < end)
			{
				charFX.Color = label.HighlightColor;
				return true;
			}

			return false;
		}

	}

}
