using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using static PlayerMLAgent;

/// <summary>
/// Manages the spawning and coordination of multiple ML training agents
/// Attach this to a GameObject in your training scene
/// </summary>
public class MLTrainingManager : MonoBehaviour
{
    
    [Header("Spawn Layout")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(50, 50);
    [SerializeField] private Vector2 spawnAreaCenter = Vector2.zero;
    [SerializeField] private float minSpawnDistance = 5f;
    [SerializeField] private SpawnPattern spawnPattern = SpawnPattern.Grid;
   
    
    [Header("Target Management")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private bool spawnTargetsForEachAgent = true;
    [SerializeField] private float targetDistanceFromAgent = 10f;
    
    [Header("Episode Management")]
    [SerializeField] private bool synchronizeEpisodes = false;
    [SerializeField] private float episodeDuration = 60f;
    [SerializeField] private bool autoReset = true;
    
    [Header("Performance")]
    [SerializeField] private bool useFixedDecisionInterval = true;
    [SerializeField] public int decisionRequestsPerSecond = 10;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showGizmos = true;
    
    public enum SpawnPattern
    {
        Grid,
        Circle,
        Random,
        Line
    }
    [SerializeField]
    private DebugVisualization debugFlags = DebugVisualization.None;
    private List<PlayerMLAgent> spawnedAgents = new List<PlayerMLAgent>();
    private List<GameObject> spawnedTargets = new List<GameObject>();
    PlayerSynchronizer playerSynchronizer;
    private float globalEpisodeTimer = 0f;
    private float decisionTimer = 0;

    #region Initialization

    private void Awake()
    {
        playerSynchronizer = GetComponent<PlayerSynchronizer>();
    }

    private void Start()
    {
        Academy.Instance.AutomaticSteppingEnabled = false;
    }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.1f; // Time between each agent spawn
    [SerializeField] private int numberOfAgents = 16;

    [SerializeField] public bool isTraining = false;

    [ContextMenu("Spawn All Agents")]
    public void SpawnAllAgents()
    {

        //Scene optimizations
        if (isTraining)
        {
            Camera.main.gameObject.GetComponent<CameraAnimator>().enabled = false;
            Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);
            //Camera.main.cullingMask = 704;
            Camera.main.orthographicSize = 40f;
            playerSynchronizer.localSquare.gameObject.SetActive(false);
            playerSynchronizer.localSquare.transform.position = new Vector3(128, 128, 0);
        }

        StartCoroutine(SpawnAllAgentsCoroutine());
    }

    private IEnumerator SpawnAllAgentsCoroutine()
    {
        for (int i = 0; i < numberOfAgents; i++)
        {
            SpawnAgent(i);
            if (i < numberOfAgents - 1)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    private void SpawnAgent(int index)
    {
        PlayerController agentObj = GetComponent<PlayerFactorySynchronizer>().SpawnAgent();
        agentObj.gameObject.name = $"TrainingAgent_{index}";

        if (weaponSelections.Length > 0)
        {
            int weaponSelectionIndex = index % weaponSelections.Length;
            agentObj.playerBehaviour.nozzleBehaviour.primary = weaponSelections[weaponSelectionIndex].primary.typeID;
            agentObj.playerBehaviour.nozzleBehaviour.secondary = weaponSelections[weaponSelectionIndex].secondary.typeID;
        }

        PlayerMLAgent agent = agentObj.GetComponent<PlayerMLAgent>();
        agent.mLTrainingManager = this;
        agent.isTraining = isTraining;


        spawnedAgents.Add(agent);
    }

    private void SpawnTargetForAgent(PlayerMLAgent agent, int index)
    {
        Vector3 targetPosition = (Vector2)agent.transform.position + 
            UnityEngine.Random.insideUnitCircle.normalized * targetDistanceFromAgent;
        
        GameObject target = Instantiate(targetPrefab, targetPosition, Quaternion.identity, transform);
        target.name = $"Target_{index}";
        spawnedTargets.Add(target);
    }
    
    #endregion
    
    #region Spawn Position Generation
    
    private Vector2[] GenerateSpawnPositions()
    {
        return spawnPattern switch
        {
            SpawnPattern.Grid => GenerateGridPositions(),
            SpawnPattern.Circle => GenerateCirclePositions(),
            SpawnPattern.Random => GenerateRandomPositions(),
            SpawnPattern.Line => GenerateLinePositions(),
            _ => GenerateGridPositions()
        };
    }
    
    private Vector2[] GenerateGridPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(numberOfAgents));
        float spacing = Mathf.Min(spawnAreaSize.x, spawnAreaSize.y) / gridSize;
        
        Vector2 startPos = spawnAreaCenter - spawnAreaSize / 2f;
        
        for (int y = 0; y < gridSize && positions.Count < numberOfAgents; y++)
        {
            for (int x = 0; x < gridSize && positions.Count < numberOfAgents; x++)
            {
                Vector2 pos = startPos + new Vector2(x * spacing + spacing / 2f, y * spacing + spacing / 2f);
                positions.Add(pos);
            }
        }
        
        return positions.ToArray();
    }
    
    private Vector2[] GenerateCirclePositions()
    {
        Vector2[] positions = new Vector2[numberOfAgents];
        float radius = Mathf.Min(spawnAreaSize.x, spawnAreaSize.y) / 2f;
        float angleStep = 360f / numberOfAgents;
        
        for (int i = 0; i < numberOfAgents; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            positions[i] = spawnAreaCenter + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }
        
        return positions;
    }
    
    private Vector2[] GenerateRandomPositions()
    {
        List<Vector2> positions = new List<Vector2>();
        int maxAttempts = numberOfAgents * 10;
        int attempts = 0;
        
        while (positions.Count < numberOfAgents && attempts < maxAttempts)
        {
            attempts++;
            
            Vector2 randomPos = spawnAreaCenter + new Vector2(
                UnityEngine.Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                UnityEngine.Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f)
            );
            
            bool validPosition = true;
            foreach (Vector2 existingPos in positions)
            {
                if (Vector2.Distance(randomPos, existingPos) < minSpawnDistance)
                {
                    validPosition = false;
                    break;
                }
            }
            
            if (validPosition)
            {
                positions.Add(randomPos);
            }
        }
        
        return positions.ToArray();
    }
    
    private Vector2[] GenerateLinePositions()
    {
        Vector2[] positions = new Vector2[numberOfAgents];
        Vector2 startPos = spawnAreaCenter - new Vector2(spawnAreaSize.x / 2f, 0);
        float spacing = spawnAreaSize.x / (numberOfAgents - 1);
        
        for (int i = 0; i < numberOfAgents; i++)
        {
            positions[i] = startPos + new Vector2(spacing * i, 0);
        }
        
        return positions;
    }
    
    #endregion
    
    #region Update Loop
    
    private void Update()
    {
        UpdateAgents();
        while (decisionTimer > 1f) RequestAgentDecitions();
    }
    
    private void UpdateAgents()
    {
        decisionTimer += Time.deltaTime * decisionRequestsPerSecond;
        for (int i = 0; i < spawnedAgents.Count; i++)
        {
            spawnedAgents[i].SetDebugFlag(debugFlags);
            if (spawnedAgents[i].enabled) spawnedAgents[i].UpdateExtern(Time.unscaledDeltaTime, Time.deltaTime);
        }
    }

    private void RequestAgentDecitions()
    {
        decisionTimer -= 1f;
        for (int i = 0; i < spawnedAgents.Count; i++) 
        {
            if (spawnedAgents[i].enabled)  spawnedAgents[i].RequestDecisionExtern(); 
        }
        Academy.Instance.EnvironmentStep();
    }
    
    #endregion
    
    #region Management Methods
    
    [ContextMenu("Clear All Agents")]
    public void ClearAllAgents()
    {
        foreach (var agent in spawnedAgents)
        {
            if (agent != null)
            {
                Destroy(agent.gameObject);
            }
        }
        spawnedAgents.Clear();
        
        foreach (var target in spawnedTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        spawnedTargets.Clear();
    }
    
    [ContextMenu("Reset All Episodes")]
    public void ResetAllAgentEpisodes()
    {
        foreach (var agent in spawnedAgents)
        {
            if (agent != null)
            {
                agent.EndEpisode();
            }
        }
    }
    
    
    public List<PlayerMLAgent> GetAllAgents()
    {
        return new List<PlayerMLAgent>(spawnedAgents);
    }
    
    #endregion
    
    #region Debug & Gizmos
    
    private void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label($"ML Training Manager", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
        GUILayout.Label($"Active Agents: {spawnedAgents.Count}");
        
        if (synchronizeEpisodes)
        {
            GUILayout.Label($"Episode Timer: {globalEpisodeTimer:F2}s / {episodeDuration:F2}s");
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Spawn Agents"))
        {
            SpawnAllAgents();
        }
        
        if (GUILayout.Button("Clear Agents"))
        {
            ClearAllAgents();
        }
        
        if (GUILayout.Button("Reset Episodes"))
        {
            ResetAllAgentEpisodes();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    [SerializeField]
    WeaponSelectionOrder[] weaponSelections;

    [Serializable]
    public struct WeaponSelectionOrder
    {
        public WeaponBuilder primary;
        public WeaponBuilder secondary;
    }
   
    
    #endregion
}
