using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LoadableUIImageBehaviour : MonoBehaviour
{

    Image image;

    [SerializeField] string pathToPng;
    [ContextMenu("TextSet")]
    public void Test() => SetImage(pathToPng);

    public void SetImage(string PathToPNG)
    {
        string usedPath = string.Copy(PathToPNG);

        if (!File.Exists(usedPath)) usedPath = string.Copy(PathToPNG + ".png");
        if (!File.Exists(usedPath)) usedPath = string.Copy(PathToPNG + ".jpg");
        if (!File.Exists(usedPath)) usedPath = string.Copy(PathToPNG + ".jpeg");
        if (!File.Exists(usedPath)) usedPath = string.Copy(PathToPNG + ".gif");

        if (!image) image = GetComponent<Image>();
        Texture2D t = MyExtentions.LoadTexture(usedPath);
        image.sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(t.width / 2, t.height / 2));
    }
}
