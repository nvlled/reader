using Godot;
using godot_getnode;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Storage : Control
{
	const int MAX_CLIPBOARD_COUNT = 30;

	public static readonly string ClipboardFile = "user://clipboard-history.json";
	public static readonly string SavedFile = "user://saved-items.json";

	PackedScene _storageItemScene = GD.Load<PackedScene>("res://StorageItem.tscn");

	[Signal] public delegate void OpenedEventHandler(string contents);

	[GetNode("%ClipboardHistory")] VBoxContainer ClipboardNode;
	[GetNode("%Saved")] VBoxContainer SavedItems;

	[GetNode(Unique: true)] ErrorMessage ErrorMessage;
	[GetNode(Unique: true)] ScrollContainer ScrollContainer;


	public string LastClipboard
	{
		get
		{
			var child = ClipboardNode.GetChild(-1) as StorageItem;
			if (child is null) return "";
			return child.Text;
		}
	}

	public override void _Ready()
	{
		this.GetAnnotatedNodes();

		foreach (var child in ClipboardNode.GetChildren())
		{
			child.QueueFree();
		}
		foreach (var child in SavedItems.GetChildren())
		{
			child.QueueFree();
		}

		LoadData(ClipboardFile, ClipboardNode);
		LoadData(SavedFile, SavedItems);

		foreach (var child in SavedItems.GetChildren())
		{
			(child as StorageItem).ShowButton(StorageItem.ButtonType.Save, false);
		}
	}

	public void LoadData(string filename, Container container)
	{
		if (!FileAccess.FileExists(filename)) return;

		var file = FileAccess.Open(filename, FileAccess.ModeFlags.Read);
		var path = ProjectSettings.GlobalizePath(filename);

		if (file is null)
		{
			var err = FileAccess.GetOpenError();
			ErrorMessage.Text = $"Error: {err.ToString()}\nFailed to open file: {path}";
			ErrorMessage.Visible = true;
			GD.Print(ErrorMessage.Text);
		}
		else
		{
			try
			{
				var clipboard = JsonSerializer.Deserialize<List<string>>(file.GetAsText());
				foreach (var str in clipboard)
				{
					var item = _storageItemScene.Instantiate() as StorageItem;
					container.AddChild(item);
					item.Text = str;
					item.SetMinimumWidth(400);
					item.Pressed += (button) => OnPress(item, button);
				}
				GD.PrintT("loaded", ProjectSettings.GlobalizePath(filename), clipboard.Count);
			}
			catch (JsonException err)
			{
				ErrorMessage.Text = $"Error: invalid clipboard data";
				ErrorMessage.Visible = true;
				GD.Print(ErrorMessage.Text);
			}
			finally
			{
				file.Close();
			}
		}
	}

	public void SaveData(string filename, Container container)
	{
		var file = FileAccess.Open(filename, FileAccess.ModeFlags.Write);
		var clipboard = new string[container.GetChildCount()];

		for (var i = 0; i < clipboard.Length; i++)
		{
			clipboard[i] = (container.GetChild(i) as StorageItem)?.Text ?? "";
		}

		file.StoreString(JsonSerializer.Serialize(clipboard));
		file.Close();
		GD.PrintT("saved", clipboard.Length, ProjectSettings.GlobalizePath(filename));
	}

	public StorageItem AddEntry(string filename, Container container, string text)
	{
		var item = _storageItemScene.Instantiate() as StorageItem;
		container.AddChild(item);

		item.Text = text;
		item.SetMinimumWidth(400);
		item.Pressed += (button) => OnPress(item, button);

		var count = container.GetChildCount();
		if (count > MAX_CLIPBOARD_COUNT)
		{
			var excess = count - MAX_CLIPBOARD_COUNT;
			for (var i = 0; i < excess; i++)
			{
				var child = container.GetChild(i) as StorageItem;
				child?.QueueFree();
			}
		}

		SaveData(filename, container);
		return item;
	}

	public void AddSavedEntry(string text)
	{
		var item = AddEntry(SavedFile, SavedItems, text);
		item.ShowButton(StorageItem.ButtonType.Save, false);
	}

	public void AddClipboardEntry(string text)
	{
		AddEntry(ClipboardFile, ClipboardNode, text);
		ScrollContainer.ScrollVertical = (int)ClipboardNode.Size.Y;
	}

	private void OnPress(StorageItem item, StorageItem.ButtonType button)
	{
		var index = item.GetIndex();
		if (index < 0) return;

		if (button == StorageItem.ButtonType.Open)
		{
			EmitSignal(SignalName.Opened, item.Text);
			return;
		}

		switch (button)
		{
			case StorageItem.ButtonType.Remove:
				var parent = item.GetParent();
				item.QueueFree();

				if (parent == SavedItems)
					SaveData(SavedFile, SavedItems);
				else
					SaveData(ClipboardFile, ClipboardNode);

				break;

			case StorageItem.ButtonType.Save:
				item.QueueFree();
				AddSavedEntry(item.Text);
				SaveData(ClipboardFile, ClipboardNode);
				SaveData(SavedFile, SavedItems);
				break;
		}
	}
}
