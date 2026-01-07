using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MMRClient;

public class GenericTester : MonoBehaviour
{
    [SerializeField] private string serverUrl = "http://localhost:5000/mmr/calculate";

    private MMRClient _mmrClient;
    private int _completedRequests;
    private int _failedRequests;
    private float _totalResponseTime;
    private float _testStartTime;

    public int requestIterations = 100;
    public float requestInterval = 0.05f;
    public int minScore = 0;
    public int maxScore = 10;
    public int playersPerRequest = 4;

    [Header("Output")]
    [SerializeField] private bool logResponses = false;
    [SerializeField] private bool logErrors = true;

    private StringBuilder _testLog = new StringBuilder();
    private List<MMRCalculationResponse?> _responses = new List<MMRCalculationResponse?>();

    [ContextMenu("Start MMR Test")]
    private void CalculateTestMMR()
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            Debug.LogError("Server URL is not set!");
            return;
        }

        _mmrClient = new MMRClient(serverUrl);
        _completedRequests = 0;
        _failedRequests = 0;
        _totalResponseTime = 0f;
        _responses.Clear();
        _testLog.Clear();

        Debug.Log($"Starting MMR test: {requestIterations} iterations, {playersPerRequest} players per request");
        _testStartTime = Time.realtimeSinceStartup;

        StartCoroutine(RunMMRTest());
    }

    [ContextMenu("Run Single Test")]
    private void RunSingleTest()
    {
        if (string.IsNullOrEmpty(serverUrl))
        {
            Debug.LogError("Server URL is not set!");
            return;
        }

        _mmrClient = new MMRClient(serverUrl);

        var players = new List<(ulong, int)>();
        for (int i = 0; i < playersPerRequest; i++)
        {
            players.Add((RandomID, UnityEngine.Random.Range(minScore, maxScore + 1)));
        }

        Debug.Log($"Sending single test with {playersPerRequest} players");
        LogPlayerScores(players);

        _mmrClient.CalculateMMR(players, response =>
        {
            if (response.HasValue)
            {
                Debug.Log("? Request successful!");
                LogResponse(response.Value);
            }
            else
            {
                Debug.LogError("? Request failed - no response received");
            }
        });
    }

    private System.Collections.IEnumerator RunMMRTest()
    {
        for (int iteration = 0; iteration < requestIterations; iteration++)
        {
            var players = new List<(ulong, int)>();
            for (int i = 0; i < playersPerRequest; i++)
            {
                players.Add((RandomID, UnityEngine.Random.Range(minScore, maxScore + 1)));
            }

            if (logResponses && iteration == 0)
            {
                Debug.Log($"First request players:");
                LogPlayerScores(players);
            }

            float requestStartTime = Time.realtimeSinceStartup;
            int currentIteration = iteration;

            _mmrClient.CalculateMMR(players, response =>
            {
                float responseTime = Time.realtimeSinceStartup - requestStartTime;
                _totalResponseTime += responseTime;
                _completedRequests++;

                if (response.HasValue)
                {
                    _responses.Add(response.Value);

                    if (logResponses && currentIteration == 0)
                    {
                        LogResponse(response.Value);
                    }

                    if (logResponses && currentIteration % 10 == 0)
                    {
                        _testLog.AppendLine($"Iteration {currentIteration}: Success ({(responseTime * 1000):F0}ms)");
                    }
                }
                else
                {
                    _failedRequests++;
                    _responses.Add(null);

                    if (logErrors)
                    {
                        _testLog.AppendLine($"Iteration {currentIteration}: Failed ({(responseTime * 1000):F0}ms)");
                    }
                }

                if (_completedRequests + _failedRequests >= requestIterations)
                {
                    CompleteTest();
                }
            });

            if (requestInterval > 0)
            {
                yield return new UnityEngine.WaitForSeconds(requestInterval);
            }
            else
            {
                yield return null;
            }
        }
    }

    private void CompleteTest()
    {
        float totalTestTime = Time.realtimeSinceStartup - _testStartTime;
        float avgResponseTime = _totalResponseTime / (_completedRequests + _failedRequests) * 1000;

        StringBuilder summary = new StringBuilder();
        summary.AppendLine("\n========================================");
        summary.AppendLine("MMR TEST COMPLETE");
        summary.AppendLine("========================================");
        summary.AppendLine($"Total Time: {totalTestTime:F2}s");
        summary.AppendLine($"Requests Sent: {requestIterations}");
        summary.AppendLine($"Successful: {_completedRequests}");
        summary.AppendLine($"Failed: {_failedRequests}");
        summary.AppendLine($"Success Rate: {(_completedRequests * 100f / requestIterations):F1}%");
        summary.AppendLine($"Average Response Time: {avgResponseTime:F0}ms");
        summary.AppendLine($"Requests per Second: {(_completedRequests / totalTestTime):F1}");

        if (_responses.Count > 0 && _responses[0].HasValue)
        {
            summary.AppendLine("\nSample Response Data:");
            var sample = _responses[0].Value;
            for (int i = 0; i < Mathf.Min(2, sample.Players.Length); i++)
            {
                var player = sample.Players[i];
                summary.AppendLine($"  Player {i + 1}: ID={player.UserUniqueId}, MMR={player.MMR:F2}, PrevScore={player.PreviousMatchUserScore}");
            }
        }

        summary.AppendLine("\nTest Log:");
        summary.Append(_testLog.ToString());

        Debug.Log(summary.ToString());
    }

    private void LogPlayerScores(List<(ulong userId, int score)> players)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Players being sent:");
        for (int i = 0; i < players.Count; i++)
        {
            sb.AppendLine($"  Player {i + 1}: ID={players[i].userId}, Score={players[i].score}");
        }
        Debug.Log(sb.ToString());
    }

    private void LogResponse(MMRCalculationResponse response)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MMR Calculation Response:");
        for (int i = 0; i < response.Players.Length; i++)
        {
            var player = response.Players[i];
            sb.AppendLine($"  Player {i + 1}:");
            sb.AppendLine($"    ID: {player.UserUniqueId}");
            sb.AppendLine($"    Previous Score: {player.PreviousMatchUserScore}");
            sb.AppendLine($"    MMR: {player.MMR:F2}");
        }
        Debug.Log(sb.ToString());
    }

    private ulong RandomID => (ulong)UnityEngine.Random.Range(0, uint.MaxValue);

    void OnDestroy()
    {
        // Clean up any running coroutines
        StopAllCoroutines();
    }
}