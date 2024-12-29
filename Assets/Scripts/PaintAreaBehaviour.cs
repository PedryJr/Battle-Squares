using System.Collections.Generic;
using UnityEngine;

public class PaintAreaBehaviour : MonoBehaviour
{

    public List<SkinData.SkinFrame> liveFrameEdit = new List<SkinData.SkinFrame>();
    public SkinData.SkinFrame defaultFrame = new SkinData.SkinFrame();

    private void Awake()
    {

        defaultFrame.frame = new bool[116];
        for (int i = 0; i < defaultFrame.frame.Length; i++)
        {
            defaultFrame.frame[i] = true;
        }

        liveFrameEdit.Add(defaultFrame);

    }

}
