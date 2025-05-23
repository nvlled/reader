using Godot;
using godot_getnode;
using System;

public partial class StorageItem : MarginContainer
{
	[Signal] public delegate void PressedEventHandler(ButtonType button);

	[GetNode(Unique: true)] Label TextContents;
	[GetNode(Unique: true)] Button RemoveButton;
	[GetNode(Unique: true)] Button SaveButton;
	[GetNode(Unique: true)] Button OpenButton;

	public string Text
	{
		get { return TextContents.Text; }
		set { TextContents.Text = value; }
	}


	public override void _Ready()
	{
		this.GetAnnotatedNodes();

		RemoveButton.Pressed += () => EmitSignal(SignalName.Pressed, (int)ButtonType.Remove);
		SaveButton.Pressed += () => EmitSignal(SignalName.Pressed, (int)ButtonType.Save);
		OpenButton.Pressed += () => EmitSignal(SignalName.Pressed, (int)ButtonType.Open);
	}

	public void ShowButton(ButtonType button, bool value = true)
	{
		switch (button)
		{
			case ButtonType.Remove: RemoveButton.Visible = value; break;
			case ButtonType.Save: SaveButton.Visible = value; break;
			case ButtonType.Open: OpenButton.Visible = value; break;
		}
	}

	public void SetMinimumWidth(float width)
	{
		TextContents.CustomMinimumSize = TextContents.CustomMinimumSize with { X = width };
	}

	public enum ButtonType
	{
		Remove,
		Save,
		Open
	}
}
