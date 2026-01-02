using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Roots & UI")]
    [Tooltip("Put all gameplay objects (player, enemies, spawners, projectiles, etc.) under this GameObject")]
    public GameObject gameplayRoot;

    [Tooltip("Root GameObject of your shop UI (Canvas). Keep disabled by default in inspector)")]
    public GameObject shopUI;

    [Header("Freeze options")]
    [Tooltip("Components here will NOT be disabled when opening the shop. Useful for EventSystem, UI managers, persistent objects.")]
    public MonoBehaviour[] excludeFromFreeze;

    [Tooltip("If true, include disabled components when scanning (usually false is fine)")]
    public bool includeInactiveComponents = false;

    [Header("Time pause")]
    public bool useTimeScalePause = true;
    public float pausedTimeScale = 0f;
    public float normalTimeScale = 1f;

    bool isShopOpen = false;

    // Holds previous enabled states for components we touched
    Dictionary<MonoBehaviour, bool> previousStates = new Dictionary<MonoBehaviour, bool>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Ensure initial UI state & timescale
        if (shopUI != null) shopUI.SetActive(false);
        if (useTimeScalePause) Time.timeScale = normalTimeScale;

        // Set cursor correctly at start
        ApplyCursorState();
    }

    public void OpenShop()
    {
        if (isShopOpen) return;
        isShopOpen = true;

        FreezeGameplayRoot();

        if (shopUI != null) shopUI.SetActive(true);

        ApplyCursorState();

        if (useTimeScalePause) Time.timeScale = pausedTimeScale;
    }

    public void CloseShop()
    {
        if (!isShopOpen) return;
        isShopOpen = false;

        UnfreezeGameplayRoot();

        if (shopUI != null) shopUI.SetActive(false);

        ApplyCursorState();

        if (useTimeScalePause) Time.timeScale = normalTimeScale;
    }

    public void ToggleShop()
    {
        if (isShopOpen) CloseShop();
        else OpenShop();
    }

    void FreezeGameplayRoot()
    {
        previousStates.Clear();

        if (gameplayRoot == null)
        {
            Debug.LogWarning("GameModeManager: gameplayRoot not assigned.");
            return;
        }

        MonoBehaviour[] comps = gameplayRoot.GetComponentsInChildren<MonoBehaviour>(includeInactiveComponents);

        foreach (var comp in comps)
        {
            if (comp == null) continue;
            if (comp == this) continue;
            if (IsExcluded(comp)) continue;

            if (!previousStates.ContainsKey(comp))
                previousStates.Add(comp, comp.enabled);

            comp.enabled = false;
        }
    }

    void UnfreezeGameplayRoot()
    {
        foreach (var kvp in previousStates)
        {
            var comp = kvp.Key;
            if (comp == null) continue;

            comp.enabled = kvp.Value;
        }

        previousStates.Clear();
    }

    bool IsExcluded(MonoBehaviour comp)
    {
        if (excludeFromFreeze == null) return false;
        for (int i = 0; i < excludeFromFreeze.Length; i++)
        {
            if (excludeFromFreeze[i] == comp) return true;
        }
        return false;
    }

    /// <summary>Applies cursor state based on shop open/closed status</summary>
    void ApplyCursorState()
    {
        if (isShopOpen)
        {
            // Always force the cursor state when shop is open
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Always force the cursor state when shop is closed
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>Handles Alt+Tab or window focus changes</summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return; // ignore losing focus
        StartCoroutine(RestoreCursorNextFrame());
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) StartCoroutine(RestoreCursorNextFrame());
    }

    IEnumerator RestoreCursorNextFrame()
    {
        yield return null; // Wait one frame
        ApplyCursorState();
    }

    void OnDestroy()
    {
        if (useTimeScalePause) Time.timeScale = normalTimeScale;
        if (isShopOpen) UnfreezeGameplayRoot();
    }
}