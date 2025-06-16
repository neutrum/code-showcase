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
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(ICTWMessage).IsAssignableFrom(t) &&
                t.IsSerializable &&
                !t.IsAbstract &&
                !t.IsInterface
            )
            .OrderBy(t => t.Name)
            .ToArray();

        typeNames = messageTypes.Select(t => t.Name).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Message Subscriptions", EditorStyles.boldLabel);

        for (int i = 0; i < subscriptionsProp.arraySize; i++)
        {
            var element = subscriptionsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(element, new GUIContent($"[{i}]"), true);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Add New Subscription", EditorStyles.boldLabel);

        selectedTypeIndex = EditorGUILayout.Popup("Message Type", selectedTypeIndex, typeNames);

        if (GUILayout.Button("Add Subscription"))
        {
            var selectedMessageType = messageTypes[selectedTypeIndex];
            var subscriptionType = typeof(CTWMessageSubscription<>).MakeGenericType(selectedMessageType);
            var instance = Activator.CreateInstance(subscriptionType);

            subscriptionsProp.arraySize++;
            serializedObject.ApplyModifiedProperties(); // Apply so the new index exists

            var newElement = subscriptionsProp.GetArrayElementAtIndex(subscriptionsProp.arraySize - 1);
            newElement.managedReferenceValue = instance;

            serializedObject.ApplyModifiedProperties();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
