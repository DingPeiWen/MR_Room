using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(ARPlaneManager))]
[RequireComponent(typeof(ARAnchorManager))]
[RequireComponent(typeof(ARBoundingBoxManager))]
public class SceneController : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference _togglePlanesAction;
    [SerializeField] private InputActionReference _leftActivateAction;
    [SerializeField] private InputActionReference _rightActivateAction;

    [Header("Prefabs")]
    [SerializeField] private GameObject _grabbableCube;
    [SerializeField] private GameObject _prefab;

    [Header("Left Ray Interactor")]
    [SerializeField] private NearFarInteractor _leftRayInteractor;

    private ARPlaneManager _planeManager;
    private ARAnchorManager _anchorManager;
    private ARBoundingBoxManager _boundingBoxManager;

    private readonly List<ARAnchor> _anchors = new List<ARAnchor>();

    // 0 = None, 1 = Planes, 2 = Bounding Boxes
    private int _visualizationMode = 0;

    private int _numPlanesAddedOccurred = 0;

    // --------------------------------------------------
    // Start
    // --------------------------------------------------

    private void Start()
    {
        Debug.Log("-> SceneController::Start()");

        _planeManager = GetComponent<ARPlaneManager>();
        _anchorManager = GetComponent<ARAnchorManager>();
        _boundingBoxManager = GetComponent<ARBoundingBoxManager>();

        if (_planeManager == null ||
            _anchorManager == null ||
            _boundingBoxManager == null)
        {
            Debug.LogError(
                "AR Plane, Anchor or Bounding Box Manager missing!");
            return;
        }

        if (_togglePlanesAction != null &&
            _togglePlanesAction.action != null)
        {
            _togglePlanesAction.action.performed +=
                OnTogglePlanesAction;

            _togglePlanesAction.action.Enable();
        }

        if (_leftActivateAction != null &&
            _leftActivateAction.action != null)
        {
            _leftActivateAction.action.performed +=
                OnLeftActivateAction;

            _leftActivateAction.action.Enable();
        }

        if (_rightActivateAction != null &&
            _rightActivateAction.action != null)
        {
            _rightActivateAction.action.performed +=
                OnRightActivateAction;

            _rightActivateAction.action.Enable();
        }

        // Register tracking events
        _planeManager.trackablesChanged.AddListener(
            OnPlanesChanged);

        _boundingBoxManager.trackablesChanged.AddListener(
            OnBoxesChanged);

        _anchorManager.trackablesChanged.AddListener(
            OnAnchorsChanged);

        // Initial visualization: None
        SetVisualization(_visualizationMode);
    }

    // --------------------------------------------------
    // Visualization mode switching
    // --------------------------------------------------

    private void OnTogglePlanesAction(
        InputAction.CallbackContext context)
    {
        // 0 = None
        // 1 = Planes
        // 2 = Bounding Boxes

        _visualizationMode =
            (_visualizationMode + 1) % 3;

        SetVisualization(_visualizationMode);

        Debug.Log(
            "-> Visualization Mode: " +
            _visualizationMode);
    }

    private void SetVisualization(int mode)
    {
        switch (mode)
        {
            case 1:
                // Show AR Planes
                SetPlanesVisibility(true);
                SetBoundingBoxesVisibility(false);
                break;

            case 2:
                // Show Bounding Boxes
                SetPlanesVisibility(false);
                SetBoundingBoxesVisibility(true);
                break;

            default:
                // Hide all visualizations
                SetPlanesVisibility(false);
                SetBoundingBoxesVisibility(false);
                break;
        }
    }

    // --------------------------------------------------
    // Plane visibility
    // --------------------------------------------------

    private void SetPlanesVisibility(bool isVisible)
    {
        float fillAlpha = isVisible ? 0.3f : 0f;
        float lineAlpha = isVisible ? 1.0f : 0f;

        foreach (var plane in _planeManager.trackables)
        {
            SetTrackableAlpha(
                plane,
                fillAlpha,
                lineAlpha);
        }
    }

    // --------------------------------------------------
    // Bounding Box visibility
    // --------------------------------------------------

    private void SetBoundingBoxesVisibility(bool isVisible)
    {
        float fillAlpha = isVisible ? 0.3f : 0f;
        float lineAlpha = isVisible ? 1.0f : 0f;

        foreach (var box in _boundingBoxManager.trackables)
        {
            SetTrackableAlpha(
                box,
                fillAlpha,
                lineAlpha);
        }
    }

    // --------------------------------------------------
    // Shared transparency control
    // --------------------------------------------------

    private void SetTrackableAlpha(
        ARTrackable trackable,
        float fillAlpha,
        float lineAlpha)
    {
        if (trackable == null)
        {
            return;
        }

        // Each plane / box gets its own simple transparent material ("Sprites/Default" is always in the build),
        // because the prefab's URP material and the built-in line material do not reliably show up on the headset.
        Color baseColor = GetTrackableColor(trackable);

        MeshRenderer meshRenderer =
            trackable.GetComponentInChildren<MeshRenderer>(true);

        if (meshRenderer != null)
        {
            UseSpriteMaterial(meshRenderer);

            Color fill = baseColor;
            fill.a = fillAlpha;

            meshRenderer.sharedMaterial.color = fill;
            meshRenderer.enabled = fillAlpha > 0f;
        }

        LineRenderer lineRenderer =
            trackable.GetComponentInChildren<LineRenderer>(true);

        if (lineRenderer != null)
        {
            UseSpriteMaterial(lineRenderer);

            Color line = baseColor;
            line.a = lineAlpha;

            lineRenderer.startColor = line;
            lineRenderer.endColor = line;
            lineRenderer.enabled = lineAlpha > 0f;
        }
    }

    private static void UseSpriteMaterial(Renderer renderer)
    {
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return;
        }

        if (renderer.sharedMaterial == null ||
            renderer.sharedMaterial.shader != shader)
        {
            renderer.sharedMaterial = new Material(shader);
        }
    }

    private static Color GetTrackableColor(ARTrackable trackable)
    {
        if (trackable is ARPlane plane)
        {
            var c = plane.classifications;

            if (c.HasFlag(PlaneClassifications.Floor)) return Color.green;
            if (c.HasFlag(PlaneClassifications.WallFace)) return Color.white;
            if (c.HasFlag(PlaneClassifications.Ceiling)) return Color.red;
            if (c.HasFlag(PlaneClassifications.Table)) return Color.yellow;
            if (c.HasFlag(PlaneClassifications.Couch) ||
                c.HasFlag(PlaneClassifications.Seat)) return Color.blue;
            if (c.HasFlag(PlaneClassifications.WallArt)) return new Color(1f, 0.4f, 0f);
            if (c.HasFlag(PlaneClassifications.DoorFrame)) return Color.magenta;
            if (c.HasFlag(PlaneClassifications.WindowFrame)) return Color.cyan;

            return Color.gray;
        }

        return Color.cyan; // bounding boxes
    }

    // --------------------------------------------------
    // Print trackable information
    // --------------------------------------------------

    private void PrintTrackableInfo(ARTrackable trackable)
    {
        string classification = "";

        if (trackable is ARPlane plane)
        {
            classification =
                plane.classifications.ToString();
        }
        else if (trackable is ARBoundingBox boundingBox)
        {
            classification =
                boundingBox.classifications.ToString();
        }

        string log =
            $"ID: {trackable.trackableId}, " +
            $"Classif: {classification}";

        Debug.Log(log);
    }

    // --------------------------------------------------
    // Plane tracking
    // --------------------------------------------------

    private void OnPlanesChanged(
        ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (args.added.Count > 0)
        {
            _numPlanesAddedOccurred++;

            foreach (var plane in args.added)
            {
                PrintTrackableInfo(plane);
            }

            Debug.Log(
                "-> Number of planes: " +
                _planeManager.trackables.count);

            Debug.Log(
                "-> Num Planes Added Occurred: " +
                _numPlanesAddedOccurred);
        }

        // Apply the current mode to newly detected planes
        if (args.added.Count > 0 ||
            args.updated.Count > 0)
        {
            SetVisualization(_visualizationMode);
        }
    }

    // --------------------------------------------------
    // Bounding Box tracking
    // --------------------------------------------------

    private void OnBoxesChanged(
        ARTrackablesChangedEventArgs<ARBoundingBox> args)
    {
        if (args.added.Count > 0)
        {
            foreach (var box in args.added)
            {
                PrintTrackableInfo(box);
            }

            Debug.Log(
                "-> OnBoxesChanged() - Num boxes: " +
                _boundingBoxManager.trackables.count);
        }

        // Apply the current mode to newly detected boxes
        if (args.added.Count > 0 ||
            args.updated.Count > 0)
        {
            SetVisualization(_visualizationMode);
        }
    }

    // --------------------------------------------------
    // Left controller
    // --------------------------------------------------

    private void OnLeftActivateAction(
        InputAction.CallbackContext context)
    {
        Debug.Log("-> Left Activate");

        CheckIfRayHitsCollider();
    }

    // --------------------------------------------------
    // Right controller
    // --------------------------------------------------

    private void OnRightActivateAction(
        InputAction.CallbackContext context)
    {
        Debug.Log("-> Right Activate");

        SpawnGrabbableCube();
    }

    // --------------------------------------------------
    // Spawn cubes above detected tables
    // --------------------------------------------------

    private void SpawnGrabbableCube()
    {
        if (_grabbableCube == null)
        {
            Debug.LogWarning(
                "Grabbable Cube is not assigned!");
            return;
        }

        foreach (var plane in _planeManager.trackables)
        {
            if ((plane.classifications &
                 PlaneClassifications.Table) != 0)
            {
                Vector3 position =
                    plane.transform.position;

                position.y += 0.3f;

                Instantiate(
                    _grabbableCube,
                    position,
                    Quaternion.identity);
            }
        }
    }

    // --------------------------------------------------
    // Left ray detection
    // --------------------------------------------------

    private void CheckIfRayHitsCollider()
    {
        if (_leftRayInteractor == null ||
            _prefab == null)
        {
            Debug.LogWarning(
                "Left Interactor or Prefab missing!");
            return;
        }

        var pointType =
            _leftRayInteractor.TryGetCurveEndPoint(
                out Vector3 hitPoint);

        if (pointType.ToString() != "RaycastHit")
        {
            Debug.Log(
                "-> No raycast hit detected! Type: " +
                pointType);
            return;
        }

        _leftRayInteractor.TryGetCurveEndNormal(
            out Vector3 hitNormal);

        if (hitNormal.sqrMagnitude < 0.0001f)
        {
            hitNormal = Vector3.up;
        }

        Quaternion rotation;

        if (Mathf.Abs(
            Vector3.Dot(
                hitNormal.normalized,
                Vector3.up)) < 0.999f)
        {
            rotation = Quaternion.LookRotation(
                hitNormal,
                Vector3.up);
        }
        else
        {
            rotation = Quaternion.FromToRotation(
                Vector3.forward,
                hitNormal);
        }

        Debug.Log(
            "-> Raycast endpoint detected: " +
            hitPoint);

        CreateAnchoredObjectAsync(
            hitPoint,
            rotation);
    }

    // --------------------------------------------------
    // Create AR Anchor
    // --------------------------------------------------

    private async void CreateAnchoredObjectAsync(
        Vector3 position,
        Quaternion rotation)
    {
        if (_anchorManager == null ||
            _anchorManager.subsystem == null ||
            !_anchorManager.subsystem.running)
        {
            Debug.LogWarning(
                "AR Anchor subsystem is not running.");
            return;
        }

        Pose pose = new Pose(
            position,
            rotation);

        try
        {
            var result =
                await _anchorManager.TryAddAnchorAsync(pose);

            if (this == null)
            {
                return;
            }

            if (result.status.IsSuccess() &&
                result.value != null)
            {
                ARAnchor anchor = result.value;

                if (!_anchors.Contains(anchor))
                {
                    _anchors.Add(anchor);
                }

                GameObject instance = Instantiate(
                    _prefab,
                    anchor.transform.position,
                    anchor.transform.rotation);

                instance.transform.SetParent(
                    anchor.transform,
                    true);

                Debug.Log(
                    "-> AR Anchor created successfully: " +
                    anchor.trackableId);
            }
            else
            {
                Debug.LogWarning(
                    "-> AR Anchor creation failed.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "-> Anchor creation exception: " +
                ex.Message);
        }
    }

    // --------------------------------------------------
    // Anchor tracking
    // --------------------------------------------------

    private void OnAnchorsChanged(
        ARTrackablesChangedEventArgs<ARAnchor> args)
    {
        foreach (var addedAnchor in args.added)
        {
            if (!_anchors.Contains(addedAnchor))
            {
                _anchors.Add(addedAnchor);
            }

            Debug.Log(
                "-> Anchor added: " +
                addedAnchor.trackableId);
        }

        foreach (var removedAnchor in args.removed)
        {
            ARAnchor anchor = removedAnchor.Value;

            if (anchor != null)
            {
                _anchors.Remove(anchor);

                Debug.Log(
                    "-> Anchor removed: " +
                    anchor.trackableId);
            }
        }
    }

    // --------------------------------------------------
    // Cleanup
    // --------------------------------------------------

    private void OnDestroy()
    {
        Debug.Log(
            "-> SceneController::OnDestroy()");

        if (_togglePlanesAction != null &&
            _togglePlanesAction.action != null)
        {
            _togglePlanesAction.action.performed -=
                OnTogglePlanesAction;
        }

        if (_leftActivateAction != null &&
            _leftActivateAction.action != null)
        {
            _leftActivateAction.action.performed -=
                OnLeftActivateAction;
        }

        if (_rightActivateAction != null &&
            _rightActivateAction.action != null)
        {
            _rightActivateAction.action.performed -=
                OnRightActivateAction;
        }

        if (_planeManager != null)
        {
            _planeManager.trackablesChanged.RemoveListener(
                OnPlanesChanged);
        }

        if (_boundingBoxManager != null)
        {
            _boundingBoxManager.trackablesChanged.RemoveListener(
                OnBoxesChanged);
        }

        if (_anchorManager != null)
        {
            _anchorManager.trackablesChanged.RemoveListener(
                OnAnchorsChanged);
        }

        _anchors.Clear();
    }
}
