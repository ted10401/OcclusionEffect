using UnityEngine;
using System.Collections.Generic;

public class OcclusionTransparentEffectCamera : MonoBehaviour
{
    public class TransparentInfo
    {
        public Material[] materials;
        public Material[] sharedMaterials;
        public float currentFadeTime;
        public bool isTransparent;
    }

    private readonly int COLOR_ID = Shader.PropertyToID("_Color");

    [SerializeField] private Transform m_targetTransform;
    public Shader Shader { set { m_shader = value; } }
    [SerializeField] private Shader m_shader;
    public LayerMask LayerMask { set { m_layerMask = value; } }
    [SerializeField] private LayerMask m_layerMask;
    [SerializeField] private float m_destinationAlpha = 0.25f;
    [SerializeField] private float m_fadeDuration = 0.25f;
    private Transform m_cameraTransform;
    private Dictionary<Renderer, TransparentInfo> m_transparentDic = new Dictionary<Renderer, TransparentInfo>();
    private List<Renderer> m_clearList = new List<Renderer>();

    private void Awake()
    {
        m_cameraTransform = transform;
    }

    private void Update()
    {
        if(m_targetTransform == null)
        {
            return;
        }

        UpdateTransparentEffects();
        UpdateTransparentObjects();
        RemoveUnusedTransparent();
    }

    private float m_lerp;
    private float m_originalAlpha = 1.0f;
    private Color m_cacheColor;
    private void UpdateTransparentEffects()
    {
        foreach(KeyValuePair<Renderer, TransparentInfo> kvp in m_transparentDic)
        {
            if(kvp.Value.isTransparent)
            {
                kvp.Value.currentFadeTime += Time.deltaTime;
            }
            else
            {
                kvp.Value.currentFadeTime -= Time.deltaTime;
            }

            kvp.Value.currentFadeTime = Mathf.Clamp(kvp.Value.currentFadeTime, 0f, m_fadeDuration);
            m_lerp = kvp.Value.currentFadeTime / m_fadeDuration;

            for(int i = 0; i < kvp.Value.materials.Length; i++)
            {
                m_cacheColor = kvp.Value.materials[i].GetColor(COLOR_ID);
                m_cacheColor.a = Mathf.Lerp(m_originalAlpha, m_destinationAlpha, m_lerp);
                kvp.Value.materials[i].SetColor(COLOR_ID, m_cacheColor);
            }
        }
    }

    private Vector3 m_viewDirection;
    private float m_distance;
    private Ray m_ray;
    private RaycastHit[] m_raycastHits;
    private List<Renderer> m_renderers = new List<Renderer>();
    private Renderer[] m_cacheRenderers;
    private void UpdateTransparentObjects()
    {
        m_viewDirection = m_targetTransform.position - m_cameraTransform.position;
        m_distance = Vector3.Distance(m_cameraTransform.position, m_targetTransform.position);
        m_ray = new Ray(m_cameraTransform.position, m_viewDirection);
        m_raycastHits = Physics.RaycastAll(m_ray, m_distance, m_layerMask);

        m_renderers.Clear();
        for (int i = 0; i < m_raycastHits.Length; i++)
        {
            m_cacheRenderers = m_raycastHits[i].collider.GetComponentsInChildren<Renderer>();
            m_renderers.AddRange(m_cacheRenderers);

            for (int j = 0; j < m_cacheRenderers.Length; j++)
            {
                AddTransparent(m_cacheRenderers[j]);
            }
        }

        foreach (KeyValuePair<Renderer, TransparentInfo> kvp in m_transparentDic)
        {
            if (!m_renderers.Contains(kvp.Key))
            {
                kvp.Value.isTransparent = false;
            }
        }
    }

    private TransparentInfo m_cacheTransparentInfo;
    private void AddTransparent(Renderer renderer)
    {
        m_cacheTransparentInfo = null;
        m_transparentDic.TryGetValue(renderer, out m_cacheTransparentInfo);

        if (m_cacheTransparentInfo == null)
        {
            m_cacheTransparentInfo = new TransparentInfo();
            m_transparentDic.Add(renderer, m_cacheTransparentInfo);

            m_cacheTransparentInfo.sharedMaterials = renderer.sharedMaterials;
            m_cacheTransparentInfo.materials = renderer.materials;

            for (int i = 0; i < m_cacheTransparentInfo.materials.Length; i++)
            {
                m_cacheTransparentInfo.materials[i].shader = m_shader;
            }
        }

        m_cacheTransparentInfo.isTransparent = true;
    }

    private void RemoveUnusedTransparent()
    {
        m_clearList.Clear();

        foreach(KeyValuePair<Renderer, TransparentInfo> kvp in m_transparentDic)
        {
            if(!kvp.Value.isTransparent && kvp.Value.currentFadeTime <= 0)
            {
                kvp.Key.materials = kvp.Value.sharedMaterials;
                m_clearList.Add(kvp.Key);
            }
        }

        foreach(Renderer renderer in m_clearList)
        {
            m_transparentDic.Remove(renderer);
        }
    }
}
