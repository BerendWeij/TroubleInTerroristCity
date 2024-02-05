using UnityEngine;

public class Rotator : MonoBehaviour
{
    public float rotationSpeed = 50f;

    void Update()
    {
        // Rotate the GameObject around its own axis
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}