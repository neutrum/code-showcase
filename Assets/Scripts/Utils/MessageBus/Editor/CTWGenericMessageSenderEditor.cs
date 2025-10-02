#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using Utils.MessageBus; // for ICTWMessage

[CustomEditor(typeof(CTWGenericMessageSender))]
public class CTWGenericMessageSenderEditor : Editor
{
    private Type[] _messageTypes;
    private string[] _typeNames;
    private int _selectedTypeIndex;
    private SerializedProperty _messageProp;

    void OnEnable()
    {
        _messageProp = serializedObject.FindProperty("message");

        _messageTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .Where(t => typeof(ICTWMessage).IsAssignableFrom(t) &&
                        t.IsSerializable && !t.IsAbstract && !t.IsInterface)
            .OrderBy(t => t.Name)
            .ToArray();

        _typeNames = _messageTypes.Select(t => t.Name).ToArray();

        if (_messageProp.managedReferenceValue != null)
        {
            var currentType = _messageProp.managedReferenceValue.GetType();
            _selectedTypeIndex = Array.FindIndex(_messageTypes, t => t == currentType);
            if (_selectedTypeIndex < 0) _selectedTypeIndex = 0;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("CTW Generic Message Sender", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
            _selectedTypeIndex = EditorGUILayout.Popup("Type", _selectedTypeIndex, _typeNames ?? Array.Empty<string>());

            if (GUILayout.Button(_messageProp.managedReferenceValue == null ? "Create Message" : "Replace Message"))
            {
                if (_messageTypes != null && _messageTypes.Length > 0)
                {
                    var t = _messageTypes[Mathf.Clamp(_selectedTypeIndex, 0, _messageTypes.Length - 1)];
                    _messageProp.managedReferenceValue = Activator.CreateInstance(t);
                }
            }

            if (_messageProp.managedReferenceValue != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_messageProp, GUIContent.none, true);
            }
        }

        EditorGUILayout.Space(6);
        var dispatchProp = serializedObject.FindProperty("dispatch");
        EditorGUILayout.PropertyField(dispatchProp);

        var mode = (CTWGenericMessageSender.DispatchMode)dispatchProp.enumValueIndex;
        switch (mode)
        {
            case CTWGenericMessageSender.DispatchMode.StaticMethod:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("staticPublishMethod"));
                break;

            case CTWGenericMessageSender.DispatchMode.Channel:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("channel"));
                break;

            case CTWGenericMessageSender.DispatchMode.GenericBus:
                EditorGUILayout.PropertyField(serializedObject.FindProperty("genericBusOpenType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("genericSendMethod"));
                EditorGUILayout.HelpBox(
                    "Open generic type MUST include arity. Example:\n  Utils.MessageBus.CTWMessageBus`1\n" +
                    "This will call CTWMessageBus<YourMessageType>.Send(msg).",
                    MessageType.Info);
                break;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sendOnAwake"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sendOnEnable"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sendOnStart"));

        EditorGUILayout.Space(8);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Send Now"))
                ((CTWGenericMessageSender)target).Send();
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("onSent"));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
