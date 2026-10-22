
using UnityEngine;
using UnityEditor;

public static class ZombieGroundAligner
{
    [MenuItem("Tools/Align Selected Object To Ground")]
    static void AlignToGround()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            Debug.LogWarning("Please select Zombie1 first.");
            return;
        }

        Renderer[] renderers =
            obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found.");
            return;
        }

        float minY = float.PositiveInfinity;

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled)
                continue;

            minY = Mathf.Min(minY, renderer.bounds.min.y);
        }

        if (float.IsPositiveInfinity(minY))
            return;

        Undo.RecordObject(obj.transform, "Align To Ground");

        Vector3 pos = obj.transform.position;
        pos.y -= minY;
        obj.transform.position = pos;

        Debug.Log("Aligned " + obj.name + " to Y = 0");
    }
}

