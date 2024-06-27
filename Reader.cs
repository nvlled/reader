using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using godot_getnode;

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

    string[] textSections;
    int index;
    Color inactiveTextColor;


    // TODO: progress 1/10
    // TODO: paste history


    public override void _Ready()
    {
        GetNodeAttribute.Ready(this);

        AutoPasteTimer.Timeout += CheckClipboard;

        inactiveTextColor = InactiveTextPreview.LabelSettings.FontColor;
        InactiveTextPreview.Visible = false;

        var sample = @"
Press <ctrl+v> to paste text to read.
Press ? to see controls.".Trim();

        SetText(sample);
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
        if (Input.IsActionJustPressed("ui_left") || Input.IsActionJustPressed("ui_text_backspace"))
        {
            ResetInactiveColor();
            Back();
        }
        else if (Input.IsActionJustPressed("ui_right") || Input.IsActionJustPressed("ui_accept"))
        {
            ResetInactiveColor();
            Forward();
        }
        else if (Input.IsActionJustPressed("ui_home"))
        {
            ResetInactiveColor();
            index = 0;
            UpdateDisplay();
        }
        else if (Input.IsActionJustPressed("ui_end"))
        {
            ResetInactiveColor();
            index = textSections.Length - 1;
            UpdateDisplay();
        }
        else if (Input.IsActionJustPressed("show_text") || Input.IsActionJustPressed("ui_up"))
        {
            ResetInactiveColor();
            UpdateDisplay();
        }
        else if (Input.IsActionJustPressed("show_text") || Input.IsActionJustPressed("ui_down"))
        {
            inactiveTextColor = inactiveTextColor.Lightened(0.4f);
            UpdateDisplay();
        }
        else if (Input.IsActionJustPressed("ui_paste"))
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

        lastClipboard = text;
        SetText(text);
    }

    private void ResetInactiveColor()
    {
        inactiveTextColor = InactiveTextPreview.LabelSettings.FontColor;
    }

    public void SetText(string text)
    {
        textSections = SplitText(text, batchSize);
        GD.PrintT("sections", textSections);
        index = 0;
        UpdateDisplay();
    }

    public void Forward()
    {
        if (index < textSections.Length - 1)
        {
            index++;
            UpdateDisplay();
        }
    }

    public void Back()
    {
        if (index > 0)
        {
            index--;
            UpdateDisplay();
        }
    }

    void UpdateDisplay()
    {
        TextDisplay.Text = "";
        TextDisplay.Clear();

        TextDisplay.PushParagraph(HorizontalAlignment.Center);

        var prevWord = "";
        if (index > 0)
        {
            TextDisplay.PushColor(inactiveTextColor);
            var text = textSections[index - 1];

            var (prefix, suffix) = TrimLastWord(text);
            if (suffix.Trim().Length <= 2)
            {
                text = prefix;
                prevWord = suffix;
            }

            TextDisplay.AppendText(SubstrEnd(text, batchSize / 3));
            TextDisplay.Pop();
        }

        if (index >= 0 && index < textSections.Length)
        {
            //TextDisplay.PushColor(Colors.White);
            var text = textSections[index];
            text = MyRegex1().Replace(text, m =>
            {
                if (m.Value.Trim() == "")
                {
                    return m.Value;
                }
                return $"[color=red]{m.Value}[/color]";
            });
            TextDisplay.AppendText(prevWord + text);
        }

        if (index < textSections.Length - 1)
        {
            TextDisplay.PushColor(inactiveTextColor);
            var text = textSections[index + 1];
            TextDisplay.AppendText(SubstrStart(text, batchSize / 3));
            TextDisplay.Pop();
        }
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

    public string[] SplitText(string text, int size)
    {
        var result = new List<string>();
        var blocks = SplitTextByBlocks(text);
        foreach (var para in blocks)
        {
            // 100 characters here is based on the viewport size of roughly 800x600
            // Ideally, this would be computed or derived automatically,
            // but I have no idea how to easily compute text sizes in godot (for now).
            if (para.Length < 100)
            {
                result.Add(symbolizeNewlines ? para.Replace('\n', '↲') : para);
                continue;
            }

            for (var i = 0; i < para.Length;)
            {
                var j = Math.Min(i + size, para.Length - 1);
                var endIndex = j;
                for (; j > i; j--)
                {
                    var ch = para[j];
                    if (char.IsControl(ch) || char.IsWhiteSpace(ch))
                    {
                        break;
                    }
                }

                string sub;
                if (j <= i || j - i < size / 2)
                {
                    sub = para.Substr(i, endIndex + 1);
                }
                else
                {
                    sub = para.Substr(i, j - i + 1);
                }

                if (symbolizeNewlines) sub = sub.Replace('\n', '↲');

                result.Add(sub);
                i = j + 1;
            }
        }

        return result.ToArray();
    }

    string SubstrEnd(string text, int size)
    {
        if (text.Length <= size)
        {
            return text;
        }

        var endSpace = char.IsWhiteSpace(text[^1]) ? text[^1].ToString() : "";
        text = text.TrimEnd();

        var len = text.Length;
        for (var i = 0; i < len - size; i++)
        {
            var j = Math.Max(len - size - i, 0);
            var ch = text[j];
            if (IsWhiteSpace(ch))
            {
                return text.Substr(j, len) + endSpace;
            }

            j = Math.Min(len - size + i, len);
            ch = text[j];
            if (IsWhiteSpace(ch))
            {
                return text.Substr(j, len) + endSpace;
            }
        }

        return text.Substr(len - size, len) + endSpace;
    }

    string SubstrStart(string text, int size)
    {
        if (text.Length <= size)
        {
            return text;
        }

        var startSpace = char.IsWhiteSpace(text[0]) ? text[0].ToString() : "";
        text = text.TrimEnd();

        var len = text.Length;
        for (var i = 0; i < len - size; i++)
        {
            var j = Math.Max(size - i, 0);
            var ch = text[j];
            if (IsWhiteSpace(ch))
            {
                return startSpace + text.Substr(0, j);
            }

            j = Math.Min(size + i, len);
            ch = text[j];
            if (IsWhiteSpace(ch))
            {
                return startSpace + text.Substr(0, j);
            }
        }

        return startSpace + text.Substr(len - size, len);
    }

    bool IsWhiteSpace(char ch) { return char.IsWhiteSpace(ch) || char.IsControl(ch); }

    [GeneratedRegex("\n\n+")]
    private static partial Regex MyRegex();
    [GeneratedRegex("\\W+")]
    private static partial Regex MyRegex1();
}
