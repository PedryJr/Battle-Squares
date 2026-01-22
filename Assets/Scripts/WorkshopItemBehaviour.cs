using Steamworks.Ugc;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WorkshopItemBehaviour : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] Image thumbNail;

    private Item? ugcItem;
    private WorkshopLoader workshopLoader;
    private bool lateDespawnFlag = false;
    private bool isDestroyed = false;

    [SerializeField] int barLength = 12;
    [SerializeField] string ringPattern = "+---";
    [SerializeField] float ringBufferSpeed = 8f;
    [SerializeField] bool testRingBuffer;

    private float ringBufferTimer;
    private char[] ringBuffer;

    private Coroutine initCoroutine;

    private void Awake()
    {
        ringBuffer = new char[barLength];
    }

    private void Start()
    {
        enabled = false;
    }

    private void Update()
    {
        if (isDestroyed) return;

        if (ringBuffer.Length != barLength)
            ringBuffer = new char[barLength];

        ringBufferTimer += Time.deltaTime * 10f;

        if (!ugcItem.HasValue) return;
        Item item = ugcItem.Value;

        if (item.IsDownloadPending || item.IsDownloading)
        {
            lateDespawnFlag = true;
            ringBufferTimer += Time.deltaTime * ringBufferSpeed;
            int offset = (int)ringBufferTimer % barLength;

            BuildRingBuffer(offset);
            titleText.text = new string(ringBuffer);
            return;
        }

        if (item.IsInstalled && lateDespawnFlag)
        {
            Debug.Log($"{item.Directory}");
            workshopLoader?.RemoveSingleItem(this);
        }
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        isDestroyed = true;

        if (initCoroutine != null)
        {
            StopCoroutine(initCoroutine);
            initCoroutine = null;
        }

        ugcItem = null;
        workshopLoader = null;
        ringBuffer = null;

        if (thumbNail != null && thumbNail.sprite != null)
        {
            Texture2D texture = thumbNail.sprite.texture;
            Destroy(thumbNail.sprite);
            if (texture != null) Destroy(texture);
        }
    }

    void BuildRingBuffer(int offset)
    {
        for (int i = 0; i < barLength; i++)
            ringBuffer[i] = '-';

        for (int i = 0; i < ringPattern.Length; i++)
        {
            int index = (offset + i) % barLength;
            ringBuffer[index] = ringPattern[i];
        }
    }

    public void StartInitialize(Item ugcItem, WorkshopLoader loader)
    {
        if (initCoroutine != null)
            StopCoroutine(initCoroutine);

        initCoroutine = StartCoroutine(Initialize(ugcItem, loader));
    }

    private IEnumerator Initialize(Item ugcItem, WorkshopLoader loader)
    {
        this.ugcItem = ugcItem;
        workshopLoader = loader;

        if (isDestroyed || !this || !gameObject)
            yield break;

        titleText.text = MyExtentions.SanitizeMessage(ugcItem.Title);

        if (thumbNail)
            thumbNail.color = new Color(1f, 1f, 1f, 0f);

        UnityWebRequest www = null;

        try
        {
            www = UnityWebRequestTexture.GetTexture(ugcItem.PreviewImageUrl);
            var op = www.SendWebRequest();

            while (!op.isDone)
            {
                if (isDestroyed || !this || !gameObject)
                {
                    www?.Dispose();
                    yield break;
                }
                yield return null;
            }

            if (isDestroyed || !this || !gameObject)
            {
                www?.Dispose();
                yield break;
            }

            if (www.result != UnityWebRequest.Result.Success) yield break;

            Texture2D texture = DownloadHandlerTexture.GetContent(www);

            if (!texture || !thumbNail || isDestroyed)
            {
                if (texture) Destroy(texture);
                yield break;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (isDestroyed || !this || !thumbNail)
            {
                if (sprite) Destroy(sprite);
                if (texture) Destroy(texture);
                yield break;
            }

            thumbNail.sprite = sprite;

            float fadeTime = 0.25f;
            float t = 0f;

            while (t < fadeTime)
            {
                if (isDestroyed || !this || !gameObject || !thumbNail) yield break;

                t += Time.deltaTime;
                float alpha = Mathf.Clamp01(t / fadeTime);
                thumbNail.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            if (thumbNail && !isDestroyed) thumbNail.color = Color.white;
        }
        finally
        {
            www?.Dispose();
            initCoroutine = null;
        }
    }

    public void Subscribe()
    {
        if (isDestroyed || !ugcItem.HasValue) return;

        Item item = this.ugcItem.Value;

        if (item.IsDownloadPending || item.IsDownloading) return;

        if (!item.Owner.IsMe)
        {
            if (item.IsInstalled)
            {
                item.Unsubscribe();
                workshopLoader?.RemoveSingleItem(this);
            }
            else
            {
                item.Subscribe();
                item.Download(true);
                enabled = true;
            }
        }
    }
}