using System; 
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class NewLevelCreator : MonoBehaviour
{

    [SerializeField]
    TMP_InputField inputField;
    DragAndScrollMod _dragMod;
    Action<string> beforeClearFunc = (levelName) => { };
    Action<string> afterClearFunc = (levelName) => { };

    private void Awake()
    {
        _dragMod = FindAnyObjectByType<DragAndScrollMod>();
        _dragMod.DisableEditInputs();
        _dragMod.SetTabFlag(false);
    }

    private void Start()
    {
        ActivateInput();
    }

    private void ActivateInput()
    {
        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        inputField.ActivateInputField();
    }


    public void BeforeClearFunc(Action<string> func) => beforeClearFunc = func;
    public void AfterClearFunc(Action<string> func) => afterClearFunc = func;

    public void FINISH_NAMING()
    {

        string sanetizedName = SanitizeToAlphanumeric(inputField.text);

        if(sanetizedName.Length == 0) { Destroy(gameObject); return; }

        ListPersistendLevels.levelPathPointer.EnsurePath(sanetizedName);
        ListPersistendLevels.levelPathPointer.SavePaths();

        _dragMod.EnableEditInputs();
        beforeClearFunc(sanetizedName);
        _dragMod.ClearAll(true);
        afterClearFunc(sanetizedName);
        Debug.Log($"Level created! {sanetizedName}");
        _dragMod.SetTabFlag(false);
        _dragMod.activeLevelName = sanetizedName;
        NewLevelListing.AllowNewCration();
        Destroy(gameObject);

    }

    public static string SanitizeToAlphanumeric(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        StringBuilder sb;
        int str_len;
        char c;

        sb = new StringBuilder(input.Length);
        str_len = input.Length;
        for (int i = 0; i < str_len; i++) { c = input[i]; if (char.IsLetterOrDigit(c)) sb.Append(c); }
        return sb.ToString();
    }

}
