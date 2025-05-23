using Godot;
using godot_getnode;
using System;

public partial class StorageWindow : Window
{
	[GetNode]
	public Storage Storage;

	public override void _Ready()
	{
		this.GetAnnotatedNodes();
	}
}
