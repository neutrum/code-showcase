using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Utils.MessageBus; // for ICTWMessage

/// Add this to any GameObject. Pick a concrete ICTWMessage in the Inspector and call Send().
public class CTWGenericMessageSender : MonoBehaviour
{
    [Header("Message")]
    [SerializeReference] public ICTWMessage message;

    [Header("Dispatch")]
    public DispatchMode dispatch = DispatchMode.GenericBus;

    [Tooltip("Static method that accepts ICTWMessage (e.g., Utils.MessageBus.MessageBus.Publish).")]
    public string staticPublishMethod = "Utils.MessageBus.MessageBus.Publish";

    [Tooltip("If using Channel, assign a CTWMessageChannel asset.")]
    public CTWMessageChannel channel;

    [Header("Generic Bus")]
    [Tooltip("Fully-qualified OPEN generic type for your bus. IMPORTANT: include arity, e.g. YourNs.CTWMessageBus`1")]
    public string genericBusOpenType = "Utils.MessageBus.CTWMessageBus`1";
    [Tooltip("Name of the static method on the closed generic bus type that sends a message (usually 'Send').")]
    public string genericSendMethod = "Send";

    [Header("Auto Send (optional)")]
    public bool sendOnAwake;
    public bool sendOnEnable;
    public bool sendOnStart;

    [Header("Events")]
    public UnityEvent onSent;

    private MethodInfo _cachedStaticPublish; // for DispatchMode.StaticMethod

    void Awake()    { if (sendOnAwake)  Send(); }
    void OnEnable() { if (sendOnEnable) Send(); }
    void Start()    { if (sendOnStart)  Send(); }

    public void Send()
    {
        if (message == null)
        {
            Debug.LogWarning("[CTWGenericMessageSender] No message assigned.");
            return;
        }

        switch (dispatch)
        {
            case DispatchMode.Channel:
                if (!channel)
                {
                    Debug.LogWarning("[CTWGenericMessageSender] Channel mode selected but no channel assigned.");
                    return;
                }
                channel.Raise(message);
                break;

            case DispatchMode.StaticMethod:
                if (_cachedStaticPublish == null)
                    _cachedStaticPublish = ResolveStaticPublish(staticPublishMethod);
                if (_cachedStaticPublish == null)
                {
                    Debug.LogError($"[CTWGenericMessageSender] Could not resolve static method '{staticPublishMethod}'.");
                    return;
                }
                try { _cachedStaticPublish.Invoke(null, new object[] { message }); }
                catch (TargetParameterCountException)
                { Debug.LogError($"[CTWGenericMessageSender] '{staticPublishMethod}' must accept exactly one parameter compatible with ICTWMessage."); }
                break;

            case DispatchMode.GenericBus:
                if (!TrySendViaGenericBus(message, out var err))
                {
                    Debug.LogError($"[CTWGenericMessageSender] GenericBus send failed: {err}");
                    return;
                }
                break;
        }

        onSent?.Invoke();
    }

    // ===== GenericBus: CTWMessageBus<T>.Send(T msg) =====
    private bool TrySendViaGenericBus(ICTWMessage msg, out string error)
    {
        error = "";
        var concreteMsgType = msg.GetType();

        var open = ResolveOpenGenericType(genericBusOpenType);
        if (open == null || !open.IsGenericTypeDefinition)
        {
            error = $"Open generic type not found or invalid: '{genericBusOpenType}'. " +
                    "Include arity with backtick, e.g., 'YourNs.CTWMessageBus`1'.";
            return false;
        }

        Type closed;
        try { closed = open.MakeGenericType(concreteMsgType); }
        catch (Exception ex) { error = $"MakeGenericType failed: {ex.Message}"; return false; }

        // Find static Send(T) (or any one-parameter method named genericSendMethod compatible with the message type)
        var send = closed.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(mi =>
            {
                if (mi.Name != genericSendMethod) return false;
                var p = mi.GetParameters();
                if (p.Length != 1) return false;
                return p[0].ParameterType.IsAssignableFrom(concreteMsgType);
            });

        if (send == null)
        {
            // Fallback: accept exact T
            send = closed.GetMethod(genericSendMethod, BindingFlags.Public | BindingFlags.Static, null,
                                    new[] { concreteMsgType }, null);
        }

        if (send == null)
        {
            error = $"No static method '{genericSendMethod}({concreteMsgType.Name})' found on {closed.FullName}.";
            return false;
        }

        try { send.Invoke(null, new object[] { msg }); }
        catch (Exception ex) { error = $"Invoke failed: {ex.InnerException?.Message ?? ex.Message}"; return false; }

        return true;
    }

    // ===== Static Publish: SomeBus.Publish(ICTWMessage) =====
    private static MethodInfo ResolveStaticPublish(string fullyQualified)
    {
        if (string.IsNullOrWhiteSpace(fullyQualified)) return null;
        int lastDot = fullyQualified.LastIndexOf('.');
        if (lastDot < 0 || lastDot == fullyQualified.Length - 1) return null;

        string typeName = fullyQualified.Substring(0, lastDot);
        string methodName = fullyQualified.Substring(lastDot + 1);

        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .FirstOrDefault(t => t.FullName == typeName);
        if (type == null) return null;

        return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(mi =>
            {
                if (mi.Name != methodName) return false;
                var p = mi.GetParameters();
                return p.Length == 1 && typeof(ICTWMessage).IsAssignableFrom(p[0].ParameterType);
            });
    }

    private static Type ResolveOpenGenericType(string fqnOrName)
    {
        if (string.IsNullOrWhiteSpace(fqnOrName)) return null;

        // Prefer exact FullName match
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .FirstOrDefault(t => t.IsGenericTypeDefinition && t.FullName == fqnOrName);

        if (type != null) return type;

        // Fallback by Name (e.g., "CTWMessageBus`1")
        type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .FirstOrDefault(t => t.IsGenericTypeDefinition && t.Name == fqnOrName);

        return type;
    }

    public enum DispatchMode { StaticMethod, Channel, GenericBus }
}
