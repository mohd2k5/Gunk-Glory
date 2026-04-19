using UnityEngine;

public class KeepUpright : MonoBehaviour
{
    public float heightAboveBall = 2f;
    private Transform parentObj;

    void Start()
    {
        parentObj = transform.parent;
    }

    void LateUpdate()
    {
        if (PlayerSingleton.Instance.GetComponent<KatamariController>().isStick.Value)
        {
            gameObject.SetActive(false);
        }
        if (parentObj == null) return;

        // Keep the text above the ball in world space
        transform.position = parentObj.position + Vector3.up * heightAboveBall;

        // Keep the text upright
        transform.rotation = Quaternion.identity;
    }
}