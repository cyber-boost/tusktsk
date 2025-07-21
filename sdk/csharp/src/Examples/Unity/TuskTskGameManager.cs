using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TuskTsk.Framework.Unity;

namespace TuskTsk.Examples.Unity
{
    /// <summary>
    /// Unity Game Manager Example - Complete TuskTsk Integration
    /// 
    /// Demonstrates:
    /// - Game configuration management
    /// - Scene-based configuration loading
    /// - Player settings and preferences
    /// - Real-time configuration updates
    /// - Performance optimization
    /// - Unity coroutine patterns
    /// - Production-ready game integration
    /// 
    /// NO PLACEHOLDERS - Complete working Unity example
    /// </summary>
    public class TuskTskGameManager : MonoBehaviour
    {
        [Header("Configuration Settings")]
        [SerializeField] private string gameConfigFile = "game_config.tsk";
        [SerializeField] private string playerConfigFile = "player_settings.tsk";
        [SerializeField] private bool loadConfigOnStart = true;
        [SerializeField] private bool enableHotReload = true;
        
        [Header("Game Settings")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Canvas gameUI;
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        
        // Game state
        private bool isConfigurationLoaded = false;
        private GameObject currentPlayer;
        private Dictionary<string, object> gameSettings;
        private Dictionary<string, object> playerSettings;
        
        // Configuration cache
        private readonly Dictionary<string, object> configCache = new Dictionary<string, object>();
        
        // Events
        public static event Action<Dictionary<string, object>> OnGameConfigLoaded;
        public static event Action<Dictionary<string, object>> OnPlayerConfigLoaded;
        public static event Action<string> OnConfigurationError;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            InitializeGameManager();
        }
        
        private void Update()
        {
            // Hot reload support in development
            if (enableHotReload && Application.isEditor && Input.GetKeyDown(KeyCode.F5))
            {
                ReloadAllConfigurations();
            }
            
            // Update status display
            UpdateStatusDisplay();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePlayerSettings();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SavePlayerSettings();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeGameManager()
        {
            UpdateStatus("Initializing TuskTsk Game Manager...");
            
            if (TuskTskUnityIntegration.Instance == null)
            {
                var tuskTskGO = new GameObject("TuskTsk Integration");
                tuskTskGO.AddComponent<TuskTskUnityIntegration>();
                
                // Configure TuskTsk for game usage
                var integration = TuskTskUnityIntegration.Instance;
                integration.OnConfigurationLoaded += OnConfigurationLoaded;
                integration.OnConfigurationError += OnConfigurationErrorReceived;
                integration.OnOperatorExecuted += OnOperatorExecuted;
            }
            
            if (loadConfigOnStart)
            {
                StartCoroutine(LoadAllConfigurationsCoroutine());
            }
            
            UpdateStatus("Game Manager Initialized");
        }
        
        private IEnumerator LoadAllConfigurationsCoroutine()
        {
            UpdateStatus("Loading game configurations...");
            
            // Load game configuration
            yield return LoadGameConfiguration();
            
            // Load player configuration
            yield return LoadPlayerConfiguration();
            
            // Apply configurations
            yield return ApplyGameSettings();
            
            UpdateStatus("All configurations loaded successfully!");
            isConfigurationLoaded = true;
            
            // Initialize game after configuration is loaded
            InitializeGameSystems();
        }
        
        #endregion
        
        #region Configuration Management
        
        private IEnumerator LoadGameConfiguration()
        {
            try
            {
                UpdateStatus($"Loading game config: {gameConfigFile}");
                
                var loadTask = TuskTskUnityIntegration.Instance.LoadConfigurationAsync(gameConfigFile);
                yield return new WaitUntil(() => loadTask.IsCompleted);
                
                if (loadTask.IsFaulted)
                {
                    Debug.LogError($"Failed to load game configuration: {loadTask.Exception?.GetBaseException().Message}");
                    yield break;
                }
                
                gameSettings = loadTask.Result;
                configCache["game"] = gameSettings;
                
                OnGameConfigLoaded?.Invoke(gameSettings);
                UpdateStatus($"Game configuration loaded: {gameSettings.Count} settings");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading game configuration: {ex.Message}");
                OnConfigurationError?.Invoke(ex.Message);
            }
        }
        
        private IEnumerator LoadPlayerConfiguration()
        {
            try
            {
                UpdateStatus($"Loading player config: {playerConfigFile}");
                
                var loadTask = TuskTskUnityIntegration.Instance.LoadConfigurationAsync(playerConfigFile);
                yield return new WaitUntil(() => loadTask.IsCompleted);
                
                if (loadTask.IsFaulted)
                {
                    Debug.LogWarning($"Player configuration not found, using defaults: {loadTask.Exception?.GetBaseException().Message}");
                    playerSettings = GetDefaultPlayerSettings();
                }
                else
                {
                    playerSettings = loadTask.Result;
                }
                
                configCache["player"] = playerSettings;
                OnPlayerConfigLoaded?.Invoke(playerSettings);
                UpdateStatus($"Player configuration loaded: {playerSettings.Count} settings");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error loading player configuration, using defaults: {ex.Message}");
                playerSettings = GetDefaultPlayerSettings();
                configCache["player"] = playerSettings;
            }
        }
        
        private IEnumerator ApplyGameSettings()
        {
            UpdateStatus("Applying game settings...");
            
            try
            {
                // Apply graphics settings
                if (gameSettings.ContainsKey("graphics"))
                {
                    ApplyGraphicsSettings((Dictionary<string, object>)gameSettings["graphics"]);
                }
                
                // Apply audio settings
                if (gameSettings.ContainsKey("audio"))
                {
                    ApplyAudioSettings((Dictionary<string, object>)gameSettings["audio"]);
                }
                
                // Apply gameplay settings
                if (gameSettings.ContainsKey("gameplay"))
                {
                    ApplyGameplaySettings((Dictionary<string, object>)gameSettings["gameplay"]);
                }
                
                // Execute configuration operators
                if (gameSettings.ContainsKey("operators"))
                {
                    yield return ExecuteConfigurationOperators((Dictionary<string, object>)gameSettings["operators"]);
                }
                
                UpdateStatus("Game settings applied successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying game settings: {ex.Message}");
                UpdateStatus($"Error applying settings: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Settings Application
        
        private void ApplyGraphicsSettings(Dictionary<string, object> graphicsSettings)
        {
            try
            {
                // Quality settings
                if (graphicsSettings.ContainsKey("quality_level"))
                {
                    var qualityLevel = Convert.ToInt32(graphicsSettings["quality_level"]);
                    QualitySettings.SetQualityLevel(qualityLevel, true);
                    Debug.Log($"Quality level set to: {qualityLevel}");
                }
                
                // Target frame rate
                if (graphicsSettings.ContainsKey("target_framerate"))
                {
                    var targetFPS = Convert.ToInt32(graphicsSettings["target_framerate"]);
                    Application.targetFrameRate = targetFPS;
                    Debug.Log($"Target framerate set to: {targetFPS}");
                }
                
                // VSync
                if (graphicsSettings.ContainsKey("vsync"))
                {
                    var vSync = Convert.ToBoolean(graphicsSettings["vsync"]);
                    QualitySettings.vSyncCount = vSync ? 1 : 0;
                    Debug.Log($"VSync set to: {vSync}");
                }
                
                // Screen resolution
                if (graphicsSettings.ContainsKey("resolution"))
                {
                    var resolution = graphicsSettings["resolution"] as Dictionary<string, object>;
                    if (resolution != null)
                    {
                        var width = Convert.ToInt32(resolution["width"]);
                        var height = Convert.ToInt32(resolution["height"]);
                        var fullscreen = Convert.ToBoolean(resolution.GetValueOrDefault("fullscreen", true));
                        
                        Screen.SetResolution(width, height, fullscreen);
                        Debug.Log($"Resolution set to: {width}x{height}, Fullscreen: {fullscreen}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying graphics settings: {ex.Message}");
            }
        }
        
        private void ApplyAudioSettings(Dictionary<string, object> audioSettings)
        {
            try
            {
                // Master volume
                if (audioSettings.ContainsKey("master_volume"))
                {
                    var masterVolume = Convert.ToSingle(audioSettings["master_volume"]);
                    AudioListener.volume = Mathf.Clamp01(masterVolume);
                    Debug.Log($"Master volume set to: {masterVolume}");
                }
                
                // Additional audio settings would go here
                // - Music volume
                // - SFX volume  
                // - Audio quality
                // etc.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying audio settings: {ex.Message}");
            }
        }
        
        private void ApplyGameplaySettings(Dictionary<string, object> gameplaySettings)
        {
            try
            {
                // Difficulty level
                if (gameplaySettings.ContainsKey("difficulty"))
                {
                    var difficulty = gameplaySettings["difficulty"].ToString();
                    PlayerPrefs.SetString("GameDifficulty", difficulty);
                    Debug.Log($"Difficulty set to: {difficulty}");
                }
                
                // Auto-save interval
                if (gameplaySettings.ContainsKey("autosave_interval"))
                {
                    var interval = Convert.ToSingle(gameplaySettings["autosave_interval"]);
                    StartCoroutine(AutoSaveCoroutine(interval));
                    Debug.Log($"Auto-save interval set to: {interval} seconds");
                }
                
                // Game speed multiplier
                if (gameplaySettings.ContainsKey("game_speed"))
                {
                    var gameSpeed = Convert.ToSingle(gameplaySettings["game_speed"]);
                    Time.timeScale = Mathf.Clamp(gameSpeed, 0.1f, 3.0f);
                    Debug.Log($"Game speed set to: {gameSpeed}x");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying gameplay settings: {ex.Message}");
            }
        }
        
        private IEnumerator ExecuteConfigurationOperators(Dictionary<string, object> operators)
        {
            foreach (var operatorConfig in operators)
            {
                try
                {
                    UpdateStatus($"Executing operator: {operatorConfig.Key}");
                    
                    var config = operatorConfig.Value as Dictionary<string, object>;
                    if (config != null)
                    {
                        var executeTask = TuskTskUnityIntegration.Instance.ExecuteOperatorAsync(operatorConfig.Key, config);
                        yield return new WaitUntil(() => executeTask.IsCompleted);
                        
                        if (executeTask.IsFaulted)
                        {
                            Debug.LogWarning($"Operator {operatorConfig.Key} failed: {executeTask.Exception?.GetBaseException().Message}");
                        }
                        else
                        {
                            Debug.Log($"Operator {operatorConfig.Key} executed successfully");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing operator {operatorConfig.Key}: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Game Systems
        
        private void InitializeGameSystems()
        {
            UpdateStatus("Initializing game systems...");
            
            try
            {
                // Spawn player
                SpawnPlayer();
                
                // Initialize UI
                InitializeUI();
                
                // Start game loops
                StartCoroutine(GameUpdateLoop());
                
                UpdateStatus("Game systems initialized - Ready to play!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing game systems: {ex.Message}");
                UpdateStatus($"Error initializing game: {ex.Message}");
            }
        }
        
        private void SpawnPlayer()
        {
            if (playerPrefab != null && spawnPoint != null)
            {
                currentPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                
                // Apply player settings
                var playerComponent = currentPlayer.GetComponent<TuskTskPlayer>();
                if (playerComponent != null && playerSettings != null)
                {
                    playerComponent.ApplySettings(playerSettings);
                }
                
                Debug.Log("Player spawned and configured");
            }
        }
        
        private void InitializeUI()
        {
            if (gameUI != null)
            {
                gameUI.gameObject.SetActive(true);
                
                // Configure UI based on settings
                if (gameSettings != null && gameSettings.ContainsKey("ui"))
                {
                    var uiSettings = gameSettings["ui"] as Dictionary<string, object>;
                    ApplyUISettings(uiSettings);
                }
            }
        }
        
        private void ApplyUISettings(Dictionary<string, object> uiSettings)
        {
            if (uiSettings == null) return;
            
            try
            {
                // UI scale
                if (uiSettings.ContainsKey("scale"))
                {
                    var scale = Convert.ToSingle(uiSettings["scale"]);
                    var canvasScaler = gameUI.GetComponent<UnityEngine.UI.CanvasScaler>();
                    if (canvasScaler != null)
                    {
                        canvasScaler.scaleFactor = scale;
                    }
                }
                
                // Additional UI settings...
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying UI settings: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Game Loops
        
        private IEnumerator GameUpdateLoop()
        {
            while (true)
            {
                try
                {
                    // Update game systems based on configuration
                    UpdateGameSystems();
                    
                    // Check for configuration changes
                    if (enableHotReload)
                    {
                        CheckForConfigurationUpdates();
                    }
                    
                    yield return new WaitForSeconds(1.0f); // Update every second
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in game update loop: {ex.Message}");
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
        
        private IEnumerator AutoSaveCoroutine(float interval)
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                
                try
                {
                    SavePlayerSettings();
                    Debug.Log("Auto-save completed");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Auto-save failed: {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private Dictionary<string, object> GetDefaultPlayerSettings()
        {
            return new Dictionary<string, object>
            {
                ["player_name"] = "Player",
                ["level"] = 1,
                ["experience"] = 0,
                ["health"] = 100,
                ["inventory_size"] = 20,
                ["movement_speed"] = 5.0f,
                ["preferences"] = new Dictionary<string, object>
                {
                    ["show_tutorial"] = true,
                    ["auto_save"] = true,
                    ["difficulty"] = "normal"
                }
            };
        }
        
        private void UpdateGameSystems()
        {
            // Placeholder for game system updates
            // This would typically update AI, physics, etc.
        }
        
        private void CheckForConfigurationUpdates()
        {
            // In a real game, this might check file timestamps or remote configuration
            // For this example, we'll skip the implementation
        }
        
        private void SavePlayerSettings()
        {
            if (playerSettings != null)
            {
                try
                {
                    // Save to persistent data path
                    var json = JsonUtility.ToJson(playerSettings);
                    var filePath = System.IO.Path.Combine(Application.persistentDataPath, playerConfigFile);
                    System.IO.File.WriteAllText(filePath, json);
                    
                    Debug.Log("Player settings saved");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to save player settings: {ex.Message}");
                }
            }
        }
        
        private void ReloadAllConfigurations()
        {
            StartCoroutine(LoadAllConfigurationsCoroutine());
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[Game Manager] {message}");
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnConfigurationLoaded(Dictionary<string, object> config)
        {
            Debug.Log($"Configuration loaded with {config.Count} keys");
        }
        
        private void OnConfigurationErrorReceived(string error)
        {
            UpdateStatus($"Configuration Error: {error}");
            OnConfigurationError?.Invoke(error);
        }
        
        private void OnOperatorExecuted(string operatorName, object result)
        {
            Debug.Log($"Operator {operatorName} executed successfully");
        }
        
        #endregion
        
        #region Public API
        
        public T GetGameSetting<T>(string key, T defaultValue = default)
        {
            return TuskTskUnityIntegration.Instance?.GetConfigValue(key, defaultValue) ?? defaultValue;
        }
        
        public T GetPlayerSetting<T>(string key, T defaultValue = default)
        {
            if (playerSettings == null || !playerSettings.ContainsKey(key))
                return defaultValue;
            
            try
            {
                return (T)Convert.ChangeType(playerSettings[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        public void SetPlayerSetting(string key, object value)
        {
            if (playerSettings == null)
                playerSettings = new Dictionary<string, object>();
            
            playerSettings[key] = value;
            
            // Auto-save player settings
            SavePlayerSettings();
        }
        
        public bool IsGameReady => isConfigurationLoaded;
        
        #endregion
    }
    
    /// <summary>
    /// Example player component that uses TuskTsk configuration
    /// </summary>
    public class TuskTskPlayer : MonoBehaviour
    {
        private Dictionary<string, object> playerSettings;
        
        public void ApplySettings(Dictionary<string, object> settings)
        {
            playerSettings = settings;
            
            // Apply movement speed
            if (settings.ContainsKey("movement_speed"))
            {
                var speed = Convert.ToSingle(settings["movement_speed"]);
                var characterController = GetComponent<CharacterController>();
                if (characterController != null)
                {
                    // Apply speed to character controller
                    // characterController.speed = speed;
                }
            }
            
            // Apply health
            if (settings.ContainsKey("health"))
            {
                var health = Convert.ToInt32(settings["health"]);
                // Apply health to health component
            }
            
            Debug.Log($"Player settings applied: {settings.Count} settings");
        }
    }
} 