using TMPro;
using UnityEngine;

public sealed class MenuToggleButton : MonoBehaviour
{
    
    [SerializeField]
    GameObject[] modsOnObjects;

    [SerializeField]
    GameObject[] modsOffObjects;

    [SerializeField]
    bool oneWay;

    [SerializeField]
    string onName;
    [SerializeField]
    string offName;

    bool modMenuOn;

    [SerializeField]
    bool changeName;

    private void Awake()
    {
        if (!oneWay)
        {
            if (modMenuOn)
            {
                foreach (GameObject obj in modsOnObjects)
                {
                    obj.SetActive(true);
                }

                foreach (GameObject obj in modsOffObjects)
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                foreach (GameObject obj in modsOnObjects)
                {
                    obj.SetActive(false);
                }

                foreach (GameObject obj in modsOffObjects)
                {
                    obj.SetActive(true);
                }
            }
        }
    }

    public void TOGGLE()
    {

        if (oneWay)
        {

            foreach (GameObject obj in modsOnObjects)
            {
                obj.SetActive(true);
            }

            foreach (GameObject obj in modsOffObjects)
            {
                obj.SetActive(false);
            }

        }
        else
        {

            modMenuOn = !modMenuOn;
            
            if (modMenuOn)
            {

                if (changeName) GetComponentInChildren<TMP_Text>().text = onName;

                foreach (GameObject obj in modsOnObjects)
                {
                    obj.SetActive(true);
                }

                foreach (GameObject obj in modsOffObjects)
                {
                    obj.SetActive(false);
                }

            }
            else
            {

                if (changeName) GetComponentInChildren<TMP_Text>().text = offName;

                foreach (GameObject obj in modsOnObjects)
                {
                    obj.SetActive(false);
                }

                foreach (GameObject obj in modsOffObjects)
                {
                    obj.SetActive(true);
                }

            }

        }

    }

}
