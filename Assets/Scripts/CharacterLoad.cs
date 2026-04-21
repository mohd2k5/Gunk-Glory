using UnityEngine;

public class CharacterLoad : MonoBehaviour
{
    [SerializeField] private float verticalOffset = -0.5f;
    [SerializeField] private float orbitRadius = 1f;
    [SerializeField] private float scoreRadiusMultiplier = 0.1f;
    [SerializeField] private float followSmoothTime = 0.06f;
    [SerializeField] private float rotateSmoothSpeed = 12f;

    private int lastSkin = -1;
    private Vector3 velocity;

    private void LateUpdate()
    {
        if (PlayerSingleton.Instance == null) return;
        if (CharacterSelectSingleton.Instance == null) return;

        UpdateSkin();

        Transform ball = PlayerSingleton.Instance.transform;
        Camera cam = Camera.main;
        if (cam == null) return;

        KatamariController controller = PlayerSingleton.Instance.GetComponent<KatamariController>();
        if (controller == null) return;

        Vector3 flatCameraForward = cam.transform.forward;
        flatCameraForward.y = 0f;

        if (flatCameraForward.sqrMagnitude < 0.001f) return;

        flatCameraForward.Normalize();

        float currentRadius = orbitRadius + controller.Score.Value * scoreRadiusMultiplier;
        Vector3 horizontalOffset = -flatCameraForward * currentRadius;

        Vector3 targetPosition = ball.position + horizontalOffset + Vector3.up * verticalOffset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            followSmoothTime
        );

        Quaternion targetRotation = Quaternion.LookRotation(flatCameraForward, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * rotateSmoothSpeed
        );
    }

    private void UpdateSkin()
    {
        int skin = CharacterSelectSingleton.Instance.skin;
        if (skin == lastSkin) return;

        lastSkin = skin;

        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i == skin);
        }
    }
}