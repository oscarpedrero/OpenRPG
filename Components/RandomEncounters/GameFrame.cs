using Il2CppInterop.Runtime.Injection;
using System;
using BepInEx.Logging;
using UnityEngine;
using LogSystem = OpenRPG.Plugin.LogSystem;

namespace OpenRPG.Components.RandomEncounters;

public class GameFrame : MonoBehaviour
{
    private static GameFrame _instance;

    public delegate void GameFrameUpdateEventHandler();

    /// <summary>
    /// Event emitted every frame update
    /// </summary>
    public static event GameFrameUpdateEventHandler OnUpdate;

    /// <summary>
    /// Event emitted every frame late update
    /// </summary>
    public static event GameFrameUpdateEventHandler OnLateUpdate;

    void Update()
    {
        try
        {
            OnUpdate?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogSystem.Core, LogLevel.Error, $"Error dispatching OnUpdate event:\n{ex}");
        }
    }

    void LateUpdate()
    {
        try
        {
            OnLateUpdate?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log(LogSystem.Core, LogLevel.Error, $"Error dispatching OnLateUpdate event:\n{ex}");
        }
    }

    public static void Initialize()
    {
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<GameFrame>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<GameFrame>();
        }

        _instance = Plugin.Instance.AddComponent<GameFrame>();
    }

    public static void Uninitialize()
    {
        OnUpdate = null;
        OnLateUpdate = null;
        Destroy(_instance);
        _instance = null;
    }

}