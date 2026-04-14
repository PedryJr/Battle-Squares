using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ContentScanner : MonoBehaviour
{
    [SerializeField] RectTransform contentContainer;
    [SerializeField] ContentListing contentListingPrefab;
    [SerializeField] Material TMPSpriteMat;

    [SerializeField] RectTransform topBorder;
    [SerializeField] RectTransform botBorder;

    [SerializeField] public string rootDirectory = string.Empty;

    [Header("Formatting")]
    [SerializeField] ColorOption directoryColor = ColorOption.Gray;
    [SerializeField] ColorOption fileNameColor = ColorOption.White;
    [SerializeField] ColorOption extensionColor = ColorOption.Gold;
    [SerializeField] ColorOption separatorColor = ColorOption.DarkGray;
    [SerializeField] ColorOption rootMarkerColor = ColorOption.Gold;

    [Header("Tree Drawing")]
    [SerializeField] string vertical = "│";
    [SerializeField] string branchMid = "├";
    [SerializeField] string branchEnd = "└";
    [SerializeField] string horizontal = "─";

    [SerializeField] int indentWidth = 4;
    [SerializeField] int branchHorizontalCount = 2;
    [SerializeField] int extraSpacing = 1;
    [SerializeField] int chainedIndentPadding = 0;

    [SerializeField] float fadeDistance = 1.0f;
    [SerializeField] AnimationCurve fadeAnimation;

/*    public List<ContentListing> activeListings;

    private void Awake()
    {
        activeListings = new List<ContentListing>();
    }*/

/*    private void Update()
    {
        for (int i = 0; i < activeListings.Count; i++) activeListings[i].UpdateFade();
    }*/

    [ContextMenu("Test")]
    public void Test()
    {
        UpdateContent();
    }

    public void UpdateContent()
    {
        if (string.IsNullOrEmpty(rootDirectory)) return;
        if (!Directory.Exists(rootDirectory)) return;

        TMPSpriteMat.color = MyExtentions.CodeToColor((char)separatorColor);
        CleanOutOldContent();

        ContentNode root = BuildTree();
        SpawnTree(root);
    }

    void CleanOutOldContent()
    {
        for (int i = contentContainer.childCount - 1; i >= 0; i--) DestroyImmediate(contentContainer.GetChild(i).gameObject);
    }

    ContentNode BuildTree()
    {
        ContentNode root = new ContentNode
        {
            name = new DirectoryInfo(rootDirectory).Name,
            isFile = false,
            isRoot = true
        };

        string[] files = Directory.GetFiles(rootDirectory, "*.png", SearchOption.AllDirectories);

        foreach (string fullPath in files)
        {
            string relativePath = Path.GetRelativePath(rootDirectory, fullPath);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar);

            ContentNode current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                string folder = parts[i];

                if (!current.children.ContainsKey(folder))
                {
                    current.children[folder] = new ContentNode
                    {
                        name = folder,
                        isFile = false
                    };
                }

                current = current.children[folder];
            }

            string file = parts[^1];

            current.children[file] = new ContentNode
            {
                name = Path.GetFileNameWithoutExtension(file),
                extension = Path.GetExtension(file),
                isFile = true
            };
        }

        return root;
    }

    void SpawnTree(ContentNode root)
    {
        SpawnNodeRecursive(root, new List<bool>());
    }

    void SpawnNodeRecursive(ContentNode node, List<bool> isLastFlags)
    {
        bool isLast = isLastFlags.Count > 0 && isLastFlags[^1];

        ContentListing listing = Instantiate(contentListingPrefab, contentContainer);
        listing.Initialize(node, this, isLastFlags);

        if (!node.isFile)
        {
            var children = new List<ContentNode>(node.children.Values).OrderBy(n => n.isFile).ThenBy(n => n.name).ToList();

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                List<bool> nextFlags = new List<bool>(isLastFlags) { i == children.Count - 1 };
                SpawnNodeRecursive(child, nextFlags);
            }
        }
    }

    public ColorOption DirectoryColor => directoryColor;
    public ColorOption FileNameColor => fileNameColor;
    public ColorOption ExtensionColor => extensionColor;
    public ColorOption SeparatorColor => separatorColor;
    public ColorOption RootMarkerColor => rootMarkerColor;

    public string Vertical => vertical;
    public string BranchMid => branchMid;
    public string BranchEnd => branchEnd;
    public string Horizontal => horizontal;

    public int IndentWidth => indentWidth;
    public int BranchHorizontalCount => branchHorizontalCount;
    public int ExtraSpacing => extraSpacing;
    public int ChainedIndentPadding => chainedIndentPadding;

    public RectTransform TopBorder => topBorder;
    public RectTransform BotBorder => botBorder;

    public float FadeDistance => fadeDistance;
    public AnimationCurve FadeAnimation => fadeAnimation;
}

public enum ColorOption
{
    Black = '0',
    DarkBlue = '1',
    DarkGreen = '2',
    DarkAqua = '3',
    DarkRed = '4',
    DarkPurple = '5',
    Gold = '6',
    Gray = '7',
    DarkGray = '8',
    Blue = '9',
    Green = 'a',
    Aqua = 'b',
    Red = 'c',
    LightPurple = 'd',
    Yellow = 'e',
    White = 'f',
}


public class ContentNode
{
    public string name;
    public bool isFile;
    public bool isRoot;
    public string extension;
    public Dictionary<string, ContentNode> children = new Dictionary<string, ContentNode>();
}