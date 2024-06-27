using Godot;
using System;
using System.Reflection;

namespace godot_getnode;


public static class NodeExtension
{
    public static void GetAnnotatedNodes<T>(this T node) where T : Node
    {
        GetNodeAttribute.Ready(node);
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class GetNodeAttribute(string? Path = null, bool AllowNull = false, bool Unique = false) : Attribute
{
    private readonly string? path = Path;
    private readonly bool allowNull = AllowNull;
    private readonly bool unique = Unique;

    public static void Ready<T>(T node) where T : Node
    {
        var fields = node.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        foreach (var field in fields)
        {
            var attr = (GetNodeAttribute?)field.GetCustomAttribute(typeof(GetNodeAttribute));
            if (attr is null) continue;

            var path = attr.path ?? field.Name;
            if (attr.unique && path[0] != '%')
                path = "%" + path;

            var child = node.GetNode(path);

            if (child is null)
            {
                if (attr.allowNull) return;
                throw new ArgumentException($"Failed to find annotated node, GetNode(\"{path}\") returns null");
            }

            var childType = child.GetType();
            if (field.FieldType != childType && childType.IsSubclassOf(field.FieldType))
            {
                throw new ArgumentException($"Expected GetNode(\"{path}\") to have type {field.FieldType} but got {child.GetType()}");
            }

            field.SetValue(node, child);
        }
    }
}
