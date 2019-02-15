using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class OcclusionRimEffectCamera : MonoBehaviour
{
    [SerializeField] private Transform[] m_defaultTargets;
    public Shader Shader;
    public LayerMask LayerMask;
    public Color RimColor = new Color(0.75f, 1.0f, 1.0f);
    public float RimPower = 1.0f;
    
    private Camera m_camera;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();

        if (m_defaultTargets != null && m_defaultTargets.Length > 0)
        {
            for(int i = 0; i < m_defaultTargets.Length; i++)
            {
                AddTarget(m_defaultTargets[i]);
            }
        }
    }

    private void OnValidate()
    {
        foreach (KeyValuePair<Transform, OcclusionRimEffect> kvp in m_targetTransforms)
        {
            kvp.Value.UpdateCommandBuffer();
        }
    }

    private Dictionary<Transform, OcclusionRimEffect> m_targetTransforms = new Dictionary<Transform, OcclusionRimEffect>();
    private void AddTarget(Transform targetTransform)
    {
        if(targetTransform == null)
        {
            return;
        }

        if(m_targetTransforms.ContainsKey(targetTransform))
        {
            return;
        }

        m_targetTransforms.Add(targetTransform, new OcclusionRimEffect(this, targetTransform));
    }

    public void RemoveTarget(Transform targetTransform)
    {
        if (targetTransform == null)
        {
            return;
        }

        if (m_targetTransforms.ContainsKey(targetTransform))
        {
            m_targetTransforms.Remove(targetTransform);
        }
    }

    private List<Transform> m_nullTransforms = new List<Transform>();
    private void Update()
    {
        m_nullTransforms.Clear();

        foreach (KeyValuePair<Transform, OcclusionRimEffect> kvp in m_targetTransforms)
        {
            if(kvp.Key == null)
            {
                m_nullTransforms.Add(kvp.Key);
                continue;
            }

            kvp.Value.Update(Time.deltaTime);
        }

        if(m_nullTransforms.Count != 0)
        {
            for(int i = 0, count = m_nullTransforms.Count; i < count; i++)
            {
                m_targetTransforms.Remove(m_nullTransforms[i]);
            }
        }
    }

    public void OnAddCommandBuffer(CommandBuffer commandBuffer)
    {
        m_camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
    }

    public void RemoveCommandBuffer(CommandBuffer commandBuffer)
    {
        m_camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
    }
}
