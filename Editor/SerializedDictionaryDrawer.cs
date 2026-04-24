using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CC.SerializedCollections.Editor
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        private const string KeyName = "Key";
        private const string ValueName = "Value";
        private const string DataPropertyName = "data";

        private readonly Dictionary<string, ReorderableList> _lists = new();

        // Returns the true height of a property, accounting for nested ReorderableLists
        private float GetTruePropertyHeight(SerializedProperty prop)
        {
            if (_lists.TryGetValue(prop.propertyPath, out var nestedList))
            {
                return prop.isExpanded
                    ? EditorGUIUtility.singleLineHeight + 2f + nestedList.GetHeight()
                    : EditorGUIUtility.singleLineHeight + 2f;
            }

            return EditorGUI.GetPropertyHeight(prop, GUIContent.none, true);
        }

        private ReorderableList GetOrCreateList(SerializedProperty property)
        {
            var key = property.propertyPath;

            if (_lists.TryGetValue(key, out var existing))
            {
                if (existing.serializedProperty.serializedObject == property.serializedObject)
                {
                    return existing;
                }

                _lists.Remove(key);
            }

            var dataProp = property.FindPropertyRelative(DataPropertyName);
            var list = new ReorderableList(property.serializedObject, dataProp, true, true, true, true);

            list.drawHeaderCallback = rect =>
            {
                GetGenericTypeNames(fieldInfo, out var kName, out var vName);
                EditorGUI.LabelField(rect, $"{property.displayName} <{kName}, {vName}>");
            };

            list.elementHeightCallback = index =>
            {
                var el = dataProp.GetArrayElementAtIndex(index);
                var keyProp = el.FindPropertyRelative(KeyName);
                var valProp = el.FindPropertyRelative(ValueName);

                var hKey = EditorGUI.GetPropertyHeight(keyProp, GUIContent.none, true);
                var hVal = GetTruePropertyHeight(valProp);

                var single = Mathf.Approximately(hKey, EditorGUIUtility.singleLineHeight) &&
                             Mathf.Approximately(hVal, EditorGUIUtility.singleLineHeight);

                // single line: key + val side by side
                // multi line: key label row + key field + val label row + val field
                return single
                    ? hKey + 4f
                    : EditorGUIUtility.singleLineHeight + 2f  // "Key" label
                    + hKey + 4f                               // key field
                    + EditorGUIUtility.singleLineHeight + 2f  // "Value" label
                    + hVal + 4f;                              // val field
            };

            list.drawElementCallback = (rect, index, _, _) =>
            {
                var el = dataProp.GetArrayElementAtIndex(index);
                var keyProp = el.FindPropertyRelative(KeyName);
                var valProp = el.FindPropertyRelative(ValueName);

                var hKey = EditorGUI.GetPropertyHeight(keyProp, GUIContent.none, true);
                var hVal = GetTruePropertyHeight(valProp);

                var single = Mathf.Approximately(hKey, EditorGUIUtility.singleLineHeight) &&
                             Mathf.Approximately(hVal, EditorGUIUtility.singleLineHeight);

                // Highlight duplicates
                var duplicates = ComputeDuplicateIndicesFromTarget(property, fieldInfo);
                if (duplicates.Contains(index))
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y + 1, rect.width, rect.height - 2),
                        new Color(1f, 0.75f, 0.75f, 0.5f));

                var r = new Rect(rect.x, rect.y + 2f, rect.width, EditorGUIUtility.singleLineHeight);

                if (single)
                {
                    const float labelWidth = 38f;
                    const float gap = 6f;
                    var half = (r.width - labelWidth * 2f - gap) / 2f;

                    EditorGUI.LabelField(new Rect(r.x, r.y, labelWidth, r.height), KeyName);
                    EditorGUI.PropertyField(new Rect(r.x + labelWidth, r.y, half, r.height), keyProp, GUIContent.none, true);

                    EditorGUI.LabelField(new Rect(r.x + labelWidth + half + gap, r.y, labelWidth, r.height), ValueName);
                    EditorGUI.PropertyField(new Rect(r.x + labelWidth * 2f + half + gap, r.y, half, r.height), valProp, GUIContent.none, true);
                }
                else
                {
                    // Key label + field
                    EditorGUI.LabelField(r, KeyName);
                    var keyFieldRect = new Rect(r.x, r.y + r.height + 2f, r.width, hKey);
                    EditorGUI.PropertyField(keyFieldRect, keyProp, GUIContent.none, true);

                    // Value label + field
                    var valLabelY = keyFieldRect.y + hKey + 4f;
                    EditorGUI.LabelField(new Rect(r.x, valLabelY, r.width, r.height), ValueName);
                    var valFieldRect = new Rect(r.x, valLabelY + r.height + 2f, r.width, hVal);
                    EditorGUI.PropertyField(valFieldRect, valProp, GUIContent.none, true);
                }

                if (GUI.changed)
                {
                    property.serializedObject.ApplyModifiedProperties();
                }
            };

            list.onAddCallback = l =>
            {
                var arr = l.serializedProperty;
                arr.InsertArrayElementAtIndex(arr.arraySize);
                l.index = arr.arraySize - 1;
                var newEl = arr.GetArrayElementAtIndex(l.index);
                ResetProperty(newEl.FindPropertyRelative(KeyName));
                ResetProperty(newEl.FindPropertyRelative(ValueName));
                arr.serializedObject.ApplyModifiedProperties();
            };

            list.onRemoveCallback = l =>
            {
                if (l.index >= 0 && l.index < l.count)
                    ReorderableList.defaultBehaviours.DoRemoveButton(l);
            };

            _lists[key] = list;
            return list;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded == false)
            {
                return EditorGUIUtility.singleLineHeight + 4f;
            }

            var list = GetOrCreateList(property);
            return EditorGUIUtility.singleLineHeight + 2f + list.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();

            var foldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded, label, true);
            if (EditorGUI.EndChangeCheck())
            {
                InternalEditorUtility.RepaintAllViews();
            }

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var list = GetOrCreateList(property);
                var listRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f, position.width, list.GetHeight());

                EditorGUI.BeginChangeCheck();
                list.DoList(listRect);
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    InternalEditorUtility.RepaintAllViews();
                }

                // Duplicate warning
                var duplicates = ComputeDuplicateIndicesFromTarget(property, fieldInfo);
                if (duplicates.Count > 0)
                {
                    var warnRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2f + list.GetHeight() + 4f, position.width, 40f);
                    EditorGUI.HelpBox(warnRect, "Duplicate keys detected", MessageType.Warning);
                }

                EditorGUI.indentLevel--;
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private static void ResetProperty(SerializedProperty prop)
        {
            if (prop == null) return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.ManagedReference:
                    var child = prop.Copy();
                    var end   = prop.GetEndProperty();
                    if (!child.NextVisible(true)) break;
                    while (!SerializedProperty.EqualContents(child, end))
                    {
                        ResetProperty(child.Copy());
                        if (!child.NextVisible(false)) break;
                    }
                    break;
                case SerializedPropertyType.ArraySize:
                    prop.intValue = 0;
                    break;
                case SerializedPropertyType.FixedBufferSize:
                    break;
                default:
                    try { prop.boxedValue = default; }
                    catch { /* unsettable, skip */ }
                    break;
            }
        }

        private static HashSet<int> ComputeDuplicateIndicesFromTarget(SerializedProperty property, FieldInfo field)
        {
            var result = new HashSet<int>();
            if (property == null || field == null) return result;

            var target = property.serializedObject.targetObject;
            if (target == false || field.DeclaringType.IsAssignableFrom(target.GetType()) == false)
            {
                return result;
            }

            var container = field.GetValue(target);
            if (container == null)
            {
                return result;
            }

            var containerType = container.GetType();
            if (containerType.IsGenericType == false || containerType.GetGenericTypeDefinition() != typeof(SerializedDictionary<,>))
            {
                return result;
            }

            var dataField = containerType.GetField(DataPropertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (dataField == null || dataField.GetValue(container) is not IList list)
            {
                return result;
            }

            var seen = new Dictionary<object, int>();
            for (var i = 0; i < list.Count; i++)
            {
                var kv = list[i];
                if (kv == null)
                {
                    continue;
                }

                var keyField = kv.GetType().GetField(KeyName, BindingFlags.Instance | BindingFlags.Public);
                if (keyField == null)
                {
                    continue;
                }

                var keyObj = keyField.GetValue(kv);
                if (keyObj == null)
                {
                    continue;
                }

                if (seen.TryGetValue(keyObj, out var first))
                {
                    result.Add(first);
                    result.Add(i);
                }
                else
                {
                    seen[keyObj] = i;
                }
            }

            return result;
        }

        private static void GetGenericTypeNames(FieldInfo field, out string keyName, out string valName)
        {
            keyName = "TKey";
            valName = "TValue";
            
            if (field == null) return;

            var t = field.FieldType;
            if (t.IsGenericType == false)
            {
                return;
            }

            var args = t.GetGenericArguments();
            if (args.Length != 2)
            {
                return;
            }

            keyName = args[0].Name;
            valName = args[1].Name;
        }
    }
}