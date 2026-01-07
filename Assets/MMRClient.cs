using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class MMRClient
{
    private static readonly HttpClient _httpClient = new HttpClient();
    private readonly string _serverUrl;

    public MMRClient(string serverUrl)
    {
        _serverUrl = serverUrl ?? string.Empty;
    }

    #region DTOs
    private struct PlayerScore
    {
        [JsonProperty("userUniqueId")]
        public ulong UserUniqueId;

        [JsonProperty("score")]
        public int Score;
    }

    private struct MMRMatchScoresRequest
    {
        [JsonProperty("players")]
        public PlayerScore[] Players;
    }

    public struct MMRData
    {
        [JsonProperty("userUniqueId")]
        public ulong UserUniqueId;

        [JsonProperty("previousMatchUserScore")]
        public int PreviousMatchUserScore;

        [JsonProperty("mmr")]
        public double MMR;
    }

    public struct MMRCalculationResponse
    {
        [JsonProperty("players")]
        public MMRData[] Players;
    }
    #endregion

    public async ValueTask<MMRCalculationResponse?> CalculateMMRAsync(
        List<(ulong userId, int score)> playerScores)
    {
        if (string.IsNullOrEmpty(_serverUrl) || playerScores == null || playerScores.Count < 2)
            return null;

        var requestObj = new MMRMatchScoresRequest
        {
            Players = new PlayerScore[playerScores.Count]
        };

        for (int i = 0; i < playerScores.Count; i++)
        {
            requestObj.Players[i] = new PlayerScore
            {
                UserUniqueId = playerScores[i].userId,
                Score = playerScores[i].score
            };
        }

        string json = JsonConvert.SerializeObject(requestObj);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(_serverUrl, content).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<MMRCalculationResponse>(responseJson);
        }

        return null;
    }

    public void CalculateMMR(
        List<(ulong userId, int score)> playerScores,
        Action<MMRCalculationResponse?> onResponse)
    {
        if (playerScores == null || playerScores.Count < 2)
        {
            onResponse?.Invoke(null);
            return;
        }

        _ = Task.Run(async () =>
        {
            var result = await CalculateMMRAsync(playerScores);
            onResponse?.Invoke(result);
        });
    }
}