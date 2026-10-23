using System.Text;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Diagnostic only: writes one status line every few seconds (shown in the DebugPanel) so you can see
// whether room scan data arrives and what the tracking floor looks like. It does not move anything.
// The component creates itself when the scene starts, so nothing needs to be added to the scene.
public class ScanStatusLogger : MonoBehaviour
{
    [SerializeField, Min(0.5f)] float m_Interval = 3f;

    ARPlaneManager m_Planes;
    ARBoundingBoxManager m_Boxes;
    XROrigin m_Origin;
    float m_Timer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        new GameObject("ScanStatusLogger").AddComponent<ScanStatusLogger>();
    }

    void Start()
    {
        m_Planes = FindFirstObjectByType<ARPlaneManager>();
        m_Boxes = FindFirstObjectByType<ARBoundingBoxManager>();
        m_Origin = FindFirstObjectByType<XROrigin>();
    }

    bool m_ReportedTracking;

    // Once: what the headset really supports and what the XR Origin ended up using.
    void ReportTracking()
    {
        m_ReportedTracking = true;

        var subsystems = new System.Collections.Generic.List<UnityEngine.XR.XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        var sb = new StringBuilder("TRACKING: ");
        foreach (var s in subsystems)
        {
            sb.Append($"[supported={s.GetSupportedTrackingOriginModes()} current={s.GetTrackingOriginMode()} running={s.running}] ");
        }
        if (subsystems.Count == 0) sb.Append("no XR input subsystem ");

        if (m_Origin != null)
        {
            var offset = m_Origin.CameraFloorOffsetObject;
            sb.Append($"cameraOffsetY={(offset != null ? offset.transform.localPosition.y : float.NaN):F2} cameraYOffsetSetting={m_Origin.CameraYOffset:F2} ");
            sb.Append($"trackablesParentY={(m_Origin.TrackablesParent != null ? m_Origin.TrackablesParent.position.y : float.NaN):F2}");
        }
        Debug.Log(sb.ToString());
    }

    void Update()
    {
        if (!m_ReportedTracking && Time.time > 4f) ReportTracking();

        m_Timer += Time.deltaTime;
        if (m_Timer < m_Interval) return;
        m_Timer = 0f;

        var sb = new StringBuilder("SCAN: ");

        if (m_Planes == null) sb.Append("planeMgr=none ");
        else
        {
            int floor = 0, wall = 0, table = 0, other = 0;
            foreach (var p in m_Planes.trackables)
            {
                if (p.classifications.HasFlag(PlaneClassifications.Floor)) floor++;
                else if (p.classifications.HasFlag(PlaneClassifications.WallFace)) wall++;
                else if (p.classifications.HasFlag(PlaneClassifications.Table)) table++;
                else other++;
            }
            sb.Append($"planeMgr={(m_Planes.enabled ? "on" : "OFF")} running={m_Planes.subsystem != null && m_Planes.subsystem.running} ");
            sb.Append($"planes={m_Planes.trackables.count} (floor {floor}, wall {wall}, table {table}, other {other}) ");
        }

        sb.Append(m_Boxes == null ? "boxMgr=none " : $"boxes={m_Boxes.trackables.count} ");

        var cam = Camera.main;
        if (m_Origin != null)
        {
            // Floor = the floor is Y 0. Device = the world is lifted by Camera Y Offset, which makes everything float.
            sb.Append($"origin={m_Origin.CurrentTrackingOriginMode} (asked {m_Origin.RequestedTrackingOriginMode}) originY={m_Origin.transform.position.y:F2} ");
        }
        if (cam != null)
            sb.Append($"headY={cam.transform.position.y:F2}");

        Debug.Log(sb.ToString());
    }
}
