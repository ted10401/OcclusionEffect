using JSLCore;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class OcclusionRimEffect : IUpdate
{
    private const int TRANSPARENT_RENDER_QUEUE = 3000;
    private readonly int RIM_COLOR_ID = Shader.PropertyToID("_RimColor");
    private readonly int RIM_POWER_ID = Shader.PropertyToID("_RimPower");

    private OcclusionRimEffectCamera m_effectCamera;
    private Transform m_targetTransform;

    private Material m_material;
    private List<Transform> m_targetBoneTransforms = new List<Transform>();
    private int m_targetBoneCount;
    private int m_targetBoneIndex;
    private Renderer[] m_renderers;
    private int[] m_cacheRenderQueues;
    private Color m_lastRimColor;
    private float m_lastRimPower;
    private bool m_isOn;
    private CommandBuffer m_commandBuffer;

    public OcclusionRimEffect(OcclusionRimEffectCamera camera, Transform targetTransform)
    {
        if(camera == null)
        {
            return;
        }

        if (targetTransform == null)
        {
            return;
        }

        if(camera.Shader == null)
        {
            return;
        }

        m_effectCamera = camera;
        m_targetTransform = targetTransform;

        m_material = new Material(m_effectCamera.Shader);
        m_targetBoneTransforms = new List<Transform>();
        m_targetBoneTransforms.Add(targetTransform);

        Animator animator = targetTransform.GetComponent<Animator>();
        if (animator != null)
        {
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.Spine));
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.Head));
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
            m_targetBoneTransforms.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
        }
        m_targetBoneCount = m_targetBoneTransforms.Count;
        m_targetBoneIndex = -1;

        m_renderers = targetTransform.GetComponentsInChildren<Renderer>();

        m_cacheRenderQueues = new int[m_renderers.Length];
        for (int i = 0; i < m_cacheRenderQueues.Length; i++)
        {
            m_cacheRenderQueues[i] = m_renderers[i].material.renderQueue;
        }

        if (m_renderers.Length > 0)
        {
            UpdateCommandBuffer();
        }
    }

    public void UpdateCommandBuffer()
    {
        if (m_renderers == null || m_renderers.Length == 0)
        {
            return;
        }

        if (m_lastRimColor == m_effectCamera.RimColor && m_lastRimPower == m_effectCamera.RimPower)
        {
            return;
        }

        if (m_commandBuffer == null)
        {
            m_commandBuffer = new CommandBuffer();
        }

        m_commandBuffer.Clear();

        m_lastRimColor = m_effectCamera.RimColor;
        m_lastRimPower = m_effectCamera.RimPower;

        m_material.SetColor(RIM_COLOR_ID, m_lastRimColor);
        m_material.SetFloat(RIM_POWER_ID, m_lastRimPower);

        for (int i = 0; i < m_renderers.Length; i++)
        {
            if (m_cacheRenderQueues[i] <= TRANSPARENT_RENDER_QUEUE)
            {
                m_commandBuffer.DrawRenderer(m_renderers[i], m_material);
            }
        }
    }

    private bool m_cacheOn;
    public void Update(float deltaTime)
    {
        m_targetBoneIndex++;
        m_targetBoneIndex %= m_targetBoneCount;

        if (m_targetBoneIndex != 0)
        {
            return;
        }

        m_cacheOn = false;
        for (int i = 0; i < m_targetBoneCount; i++)
        {
            m_cacheOn = Physics.Linecast(m_effectCamera.transform.position, m_targetBoneTransforms[i].position, m_effectCamera.LayerMask);
            if (m_cacheOn)
            {
                break;
            }
        }

        SetEffect(m_cacheOn);
    }

    private void SetEffect(bool on)
    {
        if (m_renderers == null)
        {
            return;
        }

        int count = m_renderers.Length;
        if (count == 0)
        {
            return;
        }

        if (m_isOn == on)
        {
            return;
        }

        m_isOn = on;

        if (on)
        {
            UpdateCommandBuffer();

            for (int i = 0; i < count; i++)
            {
                if (m_cacheRenderQueues[i] <= TRANSPARENT_RENDER_QUEUE)
                {
                    m_renderers[i].material.renderQueue = TRANSPARENT_RENDER_QUEUE + 1;
                }
            }

            m_effectCamera.OnAddCommandBuffer(m_commandBuffer);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (m_cacheRenderQueues[i] <= TRANSPARENT_RENDER_QUEUE)
                {
                    m_renderers[i].material.renderQueue = m_cacheRenderQueues[i];
                }
            }

            m_effectCamera.RemoveCommandBuffer(m_commandBuffer);
        }
    }
}
