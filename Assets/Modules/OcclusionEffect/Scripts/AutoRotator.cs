using UnityEngine;

public class AutoRotator : MonoBehaviour
{
    [SerializeField] private float m_rotateSpeed = 100;

    private void Update()
    {
        transform.Rotate(Vector3.up * m_rotateSpeed * Time.deltaTime);
    }
}