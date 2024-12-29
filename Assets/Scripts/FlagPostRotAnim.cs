using UnityEngine;

public sealed class FlagPostRotAnim : MonoBehaviour
{
    [SerializeField]
    Transform flagTransform;
    FlagPost flagPost;

    [SerializeField]
    Transform child;

    private void Awake()
    {

        flagPost = flagTransform.GetComponent<FlagPost>();
        flagTransform.position = transform.position;
        flagTransform.rotation = Quaternion.Euler(0, 0,
            transform.rotation.eulerAngles.z
            + flagPost.rotation
            );
        if (flagPost.localFlag)
        {

            flagPost.spawn.position = child.position;

        }

    }

    private void LateUpdate()
    {
        
        flagTransform.position = transform.position;
        flagTransform.rotation = Quaternion.Euler(0, 0,
            transform.rotation.eulerAngles.z
            + flagPost.rotation
            );
        if (flagPost.localFlag)
        {

            flagPost.spawn.position = child.position;

        }

    }

}
