using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RTSEngine;

//property drawers for IntRange & FloatRange:

public class RangeDrawer : PropertyDrawer
{
    public static void DrawRange(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Calculate rects
        var minLabelRect = new Rect(position.x, position.y, 35, position.height / 2);
        var minRect = new Rect(position.x + 45, position.y, position.width - 45, position.height / 2);
        var maxLabelRect = new Rect(position.x, position.y + position.height / 2, 35, position.height / 2);
        var maxRect = new Rect(position.x + 45, position.y + position.height / 2, position.width - 45, position.height / 2);

        EditorGUI.LabelField(minLabelRect, "Min");
        EditorGUI.PropertyField(minRect, property.FindPropertyRelative("min"), GUIContent.none);
        EditorGUI.LabelField(maxLabelRect, "Max");
        EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("max"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    //override this function to add space below the new property drawer
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * 2;
    }
}

[CustomPropertyDrawer(typeof(IntRange))]
public class IntRangeDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RangeDrawer.DrawRange(position, property, label);
    }

    //override this function to add space below the new property drawer
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * 2;
    }
}

[CustomPropertyDrawer(typeof(FloatRange))]
public class FloatRangeDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        RangeDrawer.DrawRange(position, property, label);
    }

    //override this function to add space below the new property drawer
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return base.GetPropertyHeight(property, label) * 2;
    }

}