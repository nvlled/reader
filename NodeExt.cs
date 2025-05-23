using System.Diagnostics;
using Godot;

public static class NodeGet
{
	public static void FetchNode<T>(this Node self, out T destNode, NodePath path = null)
	where T : class
	{
		if (path is null) path = typeof(T).Name;
		destNode = self.GetNode<T>(path);
		Debug.Assert(destNode is not null, "Node not found: " + path);
	}
}