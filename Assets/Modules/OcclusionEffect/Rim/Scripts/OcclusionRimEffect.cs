using UnityEngine;
using UnityEngine.Rendering;
using JSLCore.Event;
using System.Collections.Generic;

public class OcclusionRimEffect : MonoBehaviour
{
    private const int TRANSPARENT_RENDER_QUEUE = 3000;
    private readonly int RIM_COLOR_ID = Shader.PropertyToID("_RimColor");
    private readonly int RIM_POWER_ID = Shader.PropertyToID("_RimPower");
    
    [SerializeField] private Material m_occludeEffectMaterial;
    [SerializeField] private bool m_forceOn;
    [SerializeField] private LayerMask m_colliderLayerMask;
    [SerializeField] private Color m_rimColor = new Color(0.75f, 1.0f, 1.0f);
    [SerializeField] private float m_rimPower = 1.0f;

    private Transform m_cameraTransform;
    private List<Transform> m_targetTransforms;
    private int m_targetCount;
    private int m_targetIndex;
    private Renderer[] m_renderes;
    private int[] m_cacheRenderQueues;
    private CommandBuffer m_commandBuffer;
    private Color m_lastRimColor;
    private float m_lastRimPower;
    private bool m_isOn;

    private void Start()
    {
        EventResult eventResult = EventManager.Instance.SendEvent((int)CameraEvents.GET_COMMAND_BUFFER_CAMERA);
        if(eventResult == null || eventResult.Response == null)
        {
            enabled = false;
            return;
        }

        m_cameraTransform = (Transform)eventResult.Response;

        m_targetTransforms = new List<Transform>();
        m_targetTransforms.Add(transform);

        Animator animator = GetComponent<Animator>();
        if(animator != null)
        {
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.Spine));
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.Head));
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.LeftHand));
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.RightHand));
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
            m_targetTransforms.Add(animator.GetBoneTransform(HumanBodyBones.RightFoot));
        }
        m_targetCount = m_targetTransforms.Count;
        m_targetIndex = -1;
        
        m_renderes = GetComponentsInChildren<Renderer>();

        m_cacheRenderQueues = new int[m_renderes.Length];
        for(int i = 0; i < m_cacheRenderQueues.Length; i++)
        {
            m_cacheRenderQueues[i] = m_renderes[i].material.renderQueue;
        }

        if(m_renderes.Length > 0)
        {
            UpdateCommandBuffer();
        }
    }
    
    private void UpdateCommandBuffer()
    {
        if(m_renderes == null || m_renderes.Length == 0)
        {
            return;
        }

        if(m_lastRimColor == m_rimColor && m_lastRimPower == m_rimPower)
        {
            return;
        }

        if (m_commandBuffer == null)
        {
            m_commandBuffer = new CommandBuffer();
        }

        m_commandBuffer.Clear();
        
        m_lastRimColor = m_rimColor;
        m_lastRimPower = m_rimPower;

        m_occludeEffectMaterial.SetColor(RIM_COLOR_ID, m_lastRimColor);
        m_occludeEffectMaterial.SetFloat(RIM_POWER_ID, m_lastRimPower);

        for (int i = 0; i < m_renderes.Length; i++)
        {
            if (m_cacheRenderQueues[i] <= TRANSPARENT_RENDER_QUEUE)
            {
                m_commandBuffer.DrawRenderer(m_renderes[i], m_occludeEffectMaterial);
            }
        }
    }
    
    private void OnDestroy()
    {
        SetEffect(false);
    }

    private bool m_cacheOn;
	private void Update()
    {
        if(m_cameraTransform == null)
        {
            return;
        }

        if(!m_forceOn)
        {
            m_targetIndex++;
            m_targetIndex %= m_targetCount;

            if (m_targetIndex != 0)
            {
                return;
            }

            m_cacheOn = false;
            for (int i = 0; i < m_targetTransforms.Count; i++)
            {
                m_cacheOn = Physics.Linecast(m_cameraTransform.position, m_targetTransforms[i].position, m_colliderLayerMask);
                if (m_cacheOn)
                {
                    break;
                }
            }
        }
        else
        {
            m_cacheOn = true;
        }

        SetEffect(m_cacheOn);
    }
    
    private void SetEffect(bool on)
    {
        if (m_renderes == null)
        {
            return;
        }

        int count = m_renderes.Length;
        if (count == 0)
        {
            return;
        }

        if(m_isOn == on)
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
                    m_renderes[i].material.renderQueue = TRANSPARENT_RENDER_QUEUE + 1;
                }
            }

            EventManager.Instance.SendEvent((int)CameraEvents.ADD_COMMAND_BUFFER, m_commandBuffer);
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (m_cacheRenderQueues[i] <= TRANSPARENT_RENDER_QUEUE)
                {
                    m_renderes[i].material.renderQueue = m_cacheRenderQueues[i];
                }
            }

            EventManager.Instance.SendEvent((int)CameraEvents.REMOVE_COMMAND_BUFFER);
        }
    }
}