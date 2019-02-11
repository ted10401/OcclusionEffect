using UnityEngine;
using JSLCore.Event;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class OcclusionRimEffectCamera : MonoBehaviour
{
    private Camera m_camera;
    private EventListener m_eventListener;
    private CommandBuffer m_commandBuffer;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
        m_eventListener = new EventListener();
        m_eventListener.ListenForEvent((int)CameraEvents.GET_COMMAND_BUFFER_CAMERA, OnGetCommandBufferCamera);
        m_eventListener.ListenForEvent((int)CameraEvents.ADD_COMMAND_BUFFER, OnAddCommandBuffer);
        m_eventListener.ListenForEvent((int)CameraEvents.REMOVE_COMMAND_BUFFER, OnRemoveCommandBuffer);
    }

    private EventResult OnGetCommandBufferCamera(object eventData)
    {
        return new EventResult(transform);
    }

    private EventResult OnAddCommandBuffer(object eventData)
    {
        m_commandBuffer = (CommandBuffer)eventData;
        if(m_commandBuffer == null)
        {
            return null;
        }

        m_camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, m_commandBuffer);
        return null;
    }

    private EventResult OnRemoveCommandBuffer(object eventData)
    {
        if(m_commandBuffer == null)
        {
            return null;
        }

        m_camera.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, m_commandBuffer);
        return null;
    }
}
