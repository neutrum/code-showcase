using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Utils.MessageBus;

[CustomEditor(typeof(CTWGenericMessageListener))]
public class CTWGenericMessageListenerEditor : Editor
{
    private Type[] messageTypes;
    private string[] typeNames;
    private int selectedTypeIndex;
    private SerializedProperty subscriptionsProp;

    private void OnEnable()
    {
        subscriptionsProp = serializedObject.FindProperty("subs");

        messageTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => {
                try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
            })
            .Where(t =>
                typeof(ICTWMessage).IsAssignableFrom(t) &&
                t.IsSerializable &&
                !t.IsAbstract &&
                !t.IsInterface
            )
            .OrderBy(t => t.Name)
            .ToArray();

        typeNames = messageTypes.Select(t => t.Name).ToArray();
        if (selectedTypeIndex >= typeNames.Length) selectedTypeIndex = 0;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Message Subscriptions", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        int removeIndex = -1;

        // Draw each subscription with a small remove button
        for (int i = 0; i < subscriptionsProp.arraySize; i++)
        {
            var element = subscriptionsProp.GetArrayElementAtIndex(i);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.MaxWidth(36));
                    GUILayout.FlexibleSpace();

                    // Small remove button
                    if (GUILayout.Button("✖", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        removeIndex = i;
                    }
                }

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(element, GUIContent.none, includeChildren: true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);
        }

        // Handle removal AFTER loop to avoid index issues
        if (removeIndex >= 0)
        {
            // For managed references, one call usually removes it.
            // For object references Unity may null first; call twice if needed.
            int sizeBefore = subscriptionsProp.arraySize;
            subscriptionsProp.DeleteArrayElementAtIndex(removeIndex);
            if (subscriptionsProp.arraySize == sizeBefore)
            {
                // try once more in case it just nulled reference
                subscriptionsProp.DeleteArrayElementAtIndex(removeIndex);
            }
            serializedObject.ApplyModifiedProperties();
            // Early return so we don’t also draw the add UI in the same frame after deletion
            return;
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Add New Subscription", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(typeNames == null || typeNames.Length == 0))
            {
                selectedTypeIndex = EditorGUILayout.Popup(new GUIContent("Message Type"), selectedTypeIndex, typeNames ?? Array.Empty<string>());
                if (GUILayout.Button("Add", GUILayout.Width(80)))
                {
                    if (messageTypes != null && messageTypes.Length > 0)
                    {
                        var selectedMessageType = messageTypes[Mathf.Clamp(selectedTypeIndex, 0, messageTypes.Length - 1)];
                        var subscriptionType = typeof(CTWMessageSubscription<>).MakeGenericType(selectedMessageType);
                        var instance = Activator.CreateInstance(subscriptionType);

                        subscriptionsProp.arraySize++;
                        serializedObject.ApplyModifiedProperties(); // ensure the new slot exists

                        var newElement = subscriptionsProp.GetArrayElementAtIndex(subscriptionsProp.arraySize - 1);
                        newElement.managedReferenceValue = instance;

                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
