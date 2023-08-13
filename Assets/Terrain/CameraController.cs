using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float speed = 15.0f;
    public float speedBoostMultiplier = 3.0f;
    public float mouseSensitivity = 2.0f;

    private float verticalRotation = 0.0f;

    void Update()
    {
        float processedSpeed = speed;

        if (Input.GetKey(KeyCode.LeftShift))
            processedSpeed *= speedBoostMultiplier;

        transform.position += GetDirection() * processedSpeed * Time.deltaTime;

        float horizontalRotation = Input.GetAxis("Mouse X") * mouseSensitivity;
        verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Clamping the verticalRotation so that the camera won't flip over
        // and it will at most look straight up or straight down.
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        // We use the verticalRotation first as it is the rotation AROUND the X axis.
        // Even though it is ALONG the Y axis.
        transform.eulerAngles = new Vector3(verticalRotation, transform.eulerAngles.y + horizontalRotation, 0.0f);
    }
    
    Vector3 GetDirection()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            direction += transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction -= transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += transform.right;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction -= transform.right;
        }

        return direction.normalized;
    }
}
