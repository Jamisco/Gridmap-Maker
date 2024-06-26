using UnityEditor;
using UnityEngine;

namespace GridMapMaker
{
    [CustomPropertyDrawer(typeof(ShowOnlyFieldAttribute))]
    public class ShowOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            string valueStr;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    valueStr = prop.intValue.ToString();
                    break;
                case SerializedPropertyType.Boolean:
                    valueStr = prop.boolValue.ToString();
                    break;
                case SerializedPropertyType.Float:
                    valueStr = prop.floatValue.ToString("0.00000");
                    break;
                case SerializedPropertyType.String:
                    valueStr = prop.stringValue;
                    break;
                case SerializedPropertyType.Vector2:
                    valueStr = $"({prop.vector2Value.x}, {prop.vector2Value.y})";
                    break;
                case SerializedPropertyType.Vector2Int:
                    valueStr = $"({prop.vector2IntValue.x}, {prop.vector2IntValue.y})";
                    break;
                default:
                    valueStr = "(not supported)";
                    break;
            }

            EditorGUI.LabelField(position, label.text, valueStr);
        }
    }
}
