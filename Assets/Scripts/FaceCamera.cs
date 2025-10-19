using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;
    }
    void LateUpdate()
    {
        Vector3 direction = transform.position - cameraTransform.position;
        direction.y = 0f; // Dont bend the text up towards the camera, stay horizontal
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }
}
