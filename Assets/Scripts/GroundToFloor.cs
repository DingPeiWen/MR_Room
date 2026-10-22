using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Puts an object's lowest point on the floor at run time, instead of relying on a fixed Y written in the scene.
// The floor is the scanned Floor plane; without one it is the XR Origin's height (the tracking floor).
// The component adds itself to the object named "Zombie1" when the scene starts, so nothing needs to be added to the scene.
public class GroundToFloor : MonoBehaviour
{
    [Tooltip("Wait this long for a scanned floor before using the XR Origin's height.")]
    [SerializeField, Min(0f)] float m_MaxWait = 6f;

    ARPlaneManager m_Planes;
    float m_Waited;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AttachToZombie()
    {
        var zombie = GameObject.Find("Zombie1");
        if (zombie != null && zombie.GetComponent<GroundToFloor>() == null) zombie.AddComponent<GroundToFloor>();
    }

    void Start()
    {
        m_Planes = FindFirstObjectByType<ARPlaneManager>();
    }

    void Update()
    {
        m_Waited += Time.deltaTime;

        bool haveFloor = TryGetFloorY(out float floorY);
        if (!haveFloor && m_Waited < m_MaxWait) return;

        if (!haveFloor)
        {
            var origin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            floorY = origin != null ? origin.transform.position.y : 0f;
        }

        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            transform.position += Vector3.up * (floorY - bounds.min.y);
            Debug.Log($"GroundToFloor: {name} placed on floor Y = {floorY:F2} ({(haveFloor ? "scanned floor" : "tracking floor")})");
        }

        enabled = false;
    }

    bool TryGetFloorY(out float y)
    {
        y = 0f;
        if (m_Planes == null) return false;

        foreach (var plane in m_Planes.trackables)
        {
            if (!plane.classifications.HasFlag(PlaneClassifications.Floor)) continue;
            y = plane.transform.position.y;
            return true;
        }
        return false;
    }
}
