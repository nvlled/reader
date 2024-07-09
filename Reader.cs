using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using godot_getnode;


public record Phrase(string Text, int WordIndex);

public partial class Reader : Control
{

    [Export]
    public int batchSize = 30;

    [Export]
    public bool symbolizeNewlines = true;

    [Export]
    public bool autoPasteClipboard = true;
    public string lastClipboard = "";

    [GetNode("%TextDisplay")] RichTextLabel TextDisplay;
    [GetNode] Label InactiveTextPreview;
    [GetNode] Label TextSizeCompute;
    [GetNode] Timer AutoPasteTimer;
    [GetNode] ColorRect Background;
    [GetNode(Unique: true)] Button ButtonStart;
    [GetNode(Unique: true)] Button ButtonPrev;
    [GetNode(Unique: true)] Label ProgressText;
    [GetNode(Unique: true)] Button ButtonNext;
    [GetNode(Unique: true)] Button ButtonEnd;
    [GetNode(Unique: true)] Label WholeText;
    [GetNode(Unique: true)] ScrollContainer ScrollContainer;
    [GetNode] StorageWindow StorageWindow;
    [GetNode(Unique: true)] TextureButton ShowHistoryButton;

    Phrase[] textSections;
    int index;
    Color inactiveTextColor;

    readonly int pageSize = 10;

    Storage Storage;

    public override void _Ready()
    {
        GetNodeAttribute.Ready(this);

        ShowHistoryButton.Pressed += () => StorageWindow.Visible = true;

        StorageWindow.CloseRequested += () =>
        {
            StorageWindow.Visible = false;
        };

        Storage = StorageWindow.Storage;
        Storage.Opened += OnOpenText;
        lastClipboard = Storage.LastClipboard;

        if (lastClipboard == "")
        {

            lastClipboard = @"
Press <ctrl+v> to paste text to read.
Press ? to see controls.".Trim();
        }

        SetText(lastClipboard);

        AutoPasteTimer.Timeout += CheckClipboard;

        inactiveTextColor = InactiveTextPreview.LabelSettings.FontColor;
        inactiveTextColor = inactiveTextColor.Lerp(Background.Color, 0.6f);

        InactiveTextPreview.Visible = false;

        ButtonStart.Pressed += GoToStart;
        ButtonPrev.Pressed += () => Back();
        ButtonNext.Pressed += () => Forward();
        ButtonEnd.Pressed += GoToEnd;

        /*
        var storage = GetNode<Storage>("Storage");
        var window = GetNode<Window>("Window");
        RemoveChild(storage);
        //window.Size = window.Size with { X = 500, Y = 200 };
        window.Visible = true;
        window.AddChild(storage);
        window.Popup();
        storage.Visible = true;
        window.ChildControlsChanged();
        */
    }

    private void OnOpenText(string contents)
    {
        SetText(contents);
    }

    /*
    async Task TestSize()
    {
        GD.Print(TextSizeCompute.GetRect());
        TextSizeCompute.Text = @"ajsodfiajsdifoasj ajsodfiajsdf asodifjaosdf aodifjasodfajpdofjapodsifj aposidfj aosidjfpaisdjf aopsdjfajd foasjd faosdj fpa";
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GD.Print(TextSizeCompute.GetRect());
        //GD.Print(TextSizeCompute.GetThemeDefaultFont());
        GD.PrintT(
                "huh",
                TextSizeCompute.Text,
            TextSizeCompute.GetThemeDefaultFont().GetStringSize(TextSizeCompute.Text, width: TextSizeCompute.Size.X, fontSize: 50)
        );
    }
    */

    public override void _Process(double delta)
    {
        var isActionForward = Input.IsActionJustPressed(InputNames.Right) || Input.IsActionJustPressed(InputNames.Accept);
        var isActionBack = Input.IsActionJustPressed(InputNames.Left) || Input.IsActionJustPressed(InputNames.Backspace);
        if (Input.IsActionJustPressed(InputNames.PageUp) || (Input.IsKeyLabelPressed(Key.Shift) && isActionBack))
        {
            Back(pageSize);
        }
        else if (Input.IsActionJustPressed(InputNames.PageDown) || (Input.IsKeyLabelPressed(Key.Shift) && isActionForward))
        {
            Forward(pageSize);
        }
        else if (isActionBack)
        {
            Back();
        }
        else if (isActionForward)
        {
            Forward();
        }
        else if (Input.IsActionJustPressed(InputNames.Home))
        {
            GoToStart();
        }
        else if (Input.IsActionJustPressed(InputNames.End))
        {
            GoToEnd();
        }
        else if (Input.IsActionJustPressed(InputNames.Up))
        {
            if (inactiveTextColor.Luminance < 0.6)
            {
                inactiveTextColor = inactiveTextColor.Lerp(Colors.SkyBlue, 0.2f);
                UpdateDisplay();
            }
        }
        else if (Input.IsActionJustPressed(InputNames.Down))
        {
            inactiveTextColor = inactiveTextColor.Lerp(Background.Color, 0.2f);
            UpdateDisplay();
        }
        else if (Input.IsActionJustPressed(InputNames.Paste))
        {
            var text = DisplayServer.ClipboardGet();
            SetText(text);
        }
    }

    private void CheckClipboard()
    {
        if (!autoPasteClipboard) return;
        if (!DisplayServer.ClipboardHas()) return;

        var text = DisplayServer.ClipboardGet();

        if (text.Length == lastClipboard.Length && text.Length > 0 &&
            text[0] == lastClipboard[0] && text[^1] == lastClipboard[^1])
        {
            if (text == lastClipboard)
            {
                return;
            }
        }

        Storage.AddClipboardEntry(text);
        lastClipboard = text;
        SetText(text);
    }

    private void ResetInactiveColor()
    {
        inactiveTextColor = InactiveTextPreview.LabelSettings.FontColor;
    }

    public void SetText(string text)
    {
        textSections = SplitTextByWords(text, 7);

        WholeText.Text = text;

        index = 0;
        UpdateDisplay();
    }

    public void Forward(int step = 1)
    {
        if (index < textSections.Length - 1)
        {
            index = Math.Min(index + step, textSections.Length - 1);
            UpdateDisplay();
        }
    }

    public void Back(int step = 1)
    {
        if (index > 0)
        {
            index = Math.Max(index - step, 0);
            UpdateDisplay();
        }
    }

    public void GoToStart()
    {
        index = 0;
        UpdateDisplay();
    }
    public void GoToEnd()
    {
        index = textSections.Length - 1;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        TextDisplay.Text = "";
        TextDisplay.Clear();

        //TextDisplay.PushParagraph(HorizontalAlignment.Center);
        //TextDisplay.PushMono();

        if (index > 0)
        {
            var phrase = textSections[index - 1];
            var words = Spaces().Split(phrase.Text);
            TextDisplay.PushColor(inactiveTextColor);
            TextDisplay.AppendText(string.Join(' ', words[^Math.Min(words.Length, 3)..^0]));
            TextDisplay.AppendText("\n");
            TextDisplay.Pop();
        }

        if (index >= 0 && index < textSections.Length)
        {
            var text = textSections[index].Text;
            text = MyRegex1().Replace(text, m =>
            {
                if (m.Value.Trim() == "")
                {
                    return m.Value;
                }
                return $"[color=red]{m.Value}[/color]";
            });
            TextDisplay.AppendText(text);
        }

        if (index < textSections.Length - 1)
        {
            var phrase = textSections[index + 1];
            var words = Spaces().Split(phrase.Text);
            TextDisplay.PushColor(inactiveTextColor);
            TextDisplay.AppendText(" ");
            TextDisplay.AppendText(string.Join(' ', words[0..Math.Min(words.Length, 3)]));
            TextDisplay.Pop();
        }

        ProgressText.Text = $"{index + 1}/{textSections.Length}";

        Callable.From(() =>
        {
            ScrollContainer.ScrollVertical = (int)((float)index / textSections.Length * WholeText.Size.Y) - (int)ScrollContainer.Size.Y / 2;
        }).CallDeferred();
    }

    public (string, string) TrimLastWord(string text)
    {
        var i = text.Length - 1;
        for (; i >= 0; i--)
        {
            var ch = text[i];
            if (!(char.IsControl(ch) || char.IsWhiteSpace(ch))) break;
        }

        for (; i > 0; i--)
        {
            var ch = text[i];
            if (char.IsControl(ch) || char.IsWhiteSpace(ch)) break;
        }

        return (text.Substr(0, i), text.Substr(i, text.Length));
    }

    public string[] SplitTextByBlocks(string text)
    {
        var result = new List<string>();

        foreach (var e in MyRegex().Split(text))
        {
            var s = e.Trim();
            if (!string.IsNullOrEmpty(s))
                result.Add(s + "\n");
        }

        return result.ToArray();
    }

    public Phrase[] SplitTextByWords(string text, int wordsPerBatch)
    {
        text = text.Replace("\n", "↲ ");
        var words = Spaces().Split(text);
        var result = new List<Phrase>();
        for (var i = 0; i < words.Length;)
        {
            int j;
            var sumLen = 0;
            for (j = i; j < words.Length; j++)
            {
                var n = j - i;
                var word = words[j];
                if (word.Length == 0) continue;

                sumLen += word.Length;

                if (n >= wordsPerBatch * 3 / 4 && (char.IsPunctuation(word[^1]) || word[^1] == '↲'))
                    break;
                if (n >= wordsPerBatch)
                    break;
                if (sumLen >= wordsPerBatch * 4.2)
                    break;
            }
            j++;

            var chunk = string.Join(' ', words[i..Math.Min(j, words.Length)]).Trim();
            if (chunk != "")
            {
                result.Add(new Phrase(chunk, j));
            }

            i = j;
        }

        //foreach (var chunk in result)
        //{
        //    GD.PrintT(chunk.Text.Length, "chunk>", chunk.Text);
        //}


        return result.ToArray();
    }

    bool IsWhiteSpace(char ch) { return char.IsWhiteSpace(ch) || char.IsControl(ch); }

    [GeneratedRegex("\n\n+")]
    private static partial Regex MyRegex();
    [GeneratedRegex("\\W+")]
    private static partial Regex MyRegex1();
    [GeneratedRegex("\\s+")]
    private static partial Regex Spaces();
}
