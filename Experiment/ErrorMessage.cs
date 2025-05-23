using Godot;
using godot_getnode;
using System;

public partial class ErrorMessage : PanelContainer
{
	[GetNode(Unique: true)] private Label Message;
	[GetNode(Unique: true)] private Button CloseButton;

	public string Text
	{
		get { return Message.Text; }
		set { Message.Text = value; }
	}

	public override void _Ready()
	{
		this.GetAnnotatedNodes();
		Message.CustomMinimumSize = Message.CustomMinimumSize with { X = Size.X };

		CloseButton.Pressed += () => QueueFree();
	}
}
