using UnityEngine;

public class OcclusionCamera : MonoBehaviour
{
    [SerializeField] private Transform m_target;
    [SerializeField] private float m_distance = 10;
    [SerializeField] private LayerMask m_layerMask;

    private Transform m_transform;
    private Vector3 m_rayDirection;
    private RaycastHit m_raycastHit;
    private Ray m_ray;

    private void Awake()
    {
        m_transform = transform;
    }

    void Update()
    {
        m_transform.LookAt(m_target);

        m_rayDirection = m_transform.position - m_target.position;
        m_rayDirection.Normalize();

        m_ray = new Ray(m_target.position, m_rayDirection * m_distance);

        if(Physics.Raycast(m_ray, out m_raycastHit, m_layerMask))
        {
            m_transform.position = m_raycastHit.point;
        }
        else
        {
            m_transform.position = m_target.position - m_transform.forward * m_distance;
        }
    }
}
