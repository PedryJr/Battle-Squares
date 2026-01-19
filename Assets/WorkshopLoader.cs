using Steamworks.Ugc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Item = Steamworks.Ugc.Item;

public class WorkshopLoader : MonoBehaviour
{
    [SerializeField]
    WorkshopItemBehaviour workshopItemBehaviour;

    [SerializeField]
    List<WorkshopItemBehaviour> workshopItems = new List<WorkshopItemBehaviour>();

    string currentSearchTerm = "";

    const int MAX_RESULTS = 100;

    private void OnEnable()
    {
        transform.localPosition = Vector3.zero;
        UpdateSearch();
    }

    public void UpdateSearch()
    {
        DelistItems();
        StartEnlistItems(currentSearchTerm);
    }

    public void UpdateSearch(TMP_InputField search)
    {
        currentSearchTerm = MyExtentions.RemoveInvisibleChars(search.text.ToLower());
        DelistItems();
        StartEnlistItems(currentSearchTerm);
    }


    private const int BUFFER_SIZE = 200;
    private const float REFRESH_INTERVAL = 0.1f;

    private ConcurrentQueue<Item> itemBuffer = new ConcurrentQueue<Item>();
    private ConcurrentBag<Item> allBufferedItems = new ConcurrentBag<Item>();

    private volatile bool isBuffering = false;
    private volatile bool bufferComplete = false;
    private CancellationTokenSource bufferCts;

    private float lastRefreshTime;

    private void Update() => TryRefreshLobbyBuffer();

    void TryRefreshLobbyBuffer()
    {
        if (Time.time - lastRefreshTime < REFRESH_INTERVAL) return;
        lastRefreshTime = Time.time;

        int processed = 0;
        while (processed < 10 && itemBuffer.TryDequeue(out Item item))
        {
            if (workshopItems.Count >= MAX_RESULTS) break;

            if (PassesFilter(item))
            {
                WorkshopItemBehaviour newItem = Instantiate(workshopItemBehaviour, transform);
                newItem.StartInitialize(item, this);
                workshopItems.Add(newItem);
            }
            processed++;
        }
    }

    public void DelistItems()
    {
        bufferCts?.Cancel();

        for (int i = workshopItems.Count - 1; i >= 0; i--)
        {
            if (workshopItems[i] != null) Destroy(workshopItems[i].gameObject);
        }
        workshopItems.Clear();

        while (itemBuffer.TryDequeue(out _)) { }
        allBufferedItems = new ConcurrentBag<Item>();
        bufferComplete = false;
    }

    public async void StartEnlistItems(string searchTerm = "")
    {
        currentSearchTerm = searchTerm;
        DelistItems();

        bufferCts = new CancellationTokenSource();
        _ = BufferItemsAsync(bufferCts.Token);
    }

    private async Task BufferItemsAsync(CancellationToken ct)
    {
        if (isBuffering) return;
        isBuffering = true;
        bufferComplete = false;

        try
        {
            await Task.Run(async () =>
            {
                Query q = WorkshopFilterSettings.GetUGCQuery();
                int pageCounter = 1;
                int totalBuffered = 0;

                while (totalBuffered < BUFFER_SIZE && !ct.IsCancellationRequested)
                {
                    ResultPage? pr = await q.GetPageAsync(pageCounter);
                    if (!pr.HasValue) break;

                    ResultPage p = pr.Value;

                    foreach (Item item in p.Entries)
                    {
                        if (ct.IsCancellationRequested) break;
                        if (totalBuffered >= BUFFER_SIZE) break;

                        // Pre-filter in background thread
                        if (PreFilterItem(item))
                        {
                            allBufferedItems.Add(item);
                            itemBuffer.Enqueue(item);
                            totalBuffered++;
                        }
                    }

                    pageCounter++;
                }
            }, ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error buffering items: {ex.Message}");
        }
        finally
        {
            isBuffering = false;
            bufferComplete = true;
        }
    }

    private bool PreFilterItem(Item item)
    {
        bool isDiscoverMode = WorkshopFilterSettings.ugcOwnershipType == UgcOwnershipType.Discover;

        if (isDiscoverMode)
        {
            if (item.IsSubscribed || item.Owner.IsMe) return false;
        }

        return true;
    }

    private bool PassesFilter(Item item)
    {
        if (string.IsNullOrEmpty(currentSearchTerm)) return true;

        return item.Title.IndexOf(currentSearchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnDestroy()
    {
        bufferCts?.Cancel();
        bufferCts?.Dispose();
    }

    public void RemoveSingleItem(WorkshopItemBehaviour item)
    {
        workshopItems.Remove(item);
        Destroy(item.gameObject);
    }
}