using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CloudMeshController : MonoBehaviour
{
    [Header("Grid Configuration")]
    [SerializeField] private Vector3Int gridSize = new Vector3Int(5, 3, 5);
    [SerializeField] private float spacing = 2f;
    [SerializeField] private bool generateOnStart = true;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject controlPointPrefab;
    [SerializeField] private Material lineMaterial;

    [Header("Line Settings")]
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = Color.white;

    [Header("Smoke Settings")]
    [SerializeField] private bool enablePointSmoke = true;
    [SerializeField] private bool enableLineSmoke = true;
    [SerializeField] private bool enableVolumeFill = true;
    [SerializeField] private float pointSmokeRate = 15f;
    [SerializeField] private int lineParticlesCount = 15;
    [SerializeField] private int volumeFillRate = 50;

    [Header("Visual Settings")]
    [SerializeField] private float smokeOpacity = 0.35f;
    [SerializeField] private Color globalSmokeColor = new Color(0.85f, 0.9f, 1f, 1f);

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private List<ControlPoint> controlPoints = new List<ControlPoint>();
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    private CloudVolumeFiller volumeFiller;
    private bool isGenerated = false;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateGrid();
        }
    }

    void Update()
    {
        if (!isGenerated) return;

        // Mettre à jour toutes les connexions
        foreach (var point in controlPoints)
        {
            point.UpdateConnections();
        }

        // Raccourcis clavier
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetAllPoints();
            }

            if (Keyboard.current.gKey.wasPressedThisFrame)
            {
                ClearGrid();
                GenerateGrid();
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                ToggleSmoke();
            }

            // Nouveau : V pour toggle le volume fill
            if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                ToggleVolumeFill();
            }
        }
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        if (controlPointPrefab == null)
        {
            Debug.LogError("ControlPoint Prefab is not assigned!");
            return;
        }

        if (lineMaterial == null)
        {
            Debug.LogError("Line Material is not assigned!");
            return;
        }

        ClearGrid();

        Debug.Log($"Generating grid: {gridSize.x}x{gridSize.y}x{gridSize.z}");

        // Générer tous les points de contrôle
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    CreateControlPoint(x, y, z);
                }
            }
        }

        // Créer toutes les connexions
        CreateAllConnections();

        // Créer le système de remplissage de volume
        if (enableVolumeFill)
        {
            CreateVolumeFiller();
        }

        isGenerated = true;

        Debug.Log($"Grid generated: {controlPoints.Count} points, {lineRenderers.Count} connections");
    }

    void CreateControlPoint(int x, int y, int z)
    {
        Vector3 position = new Vector3(
            x * spacing - (gridSize.x - 1) * spacing / 2f,
            y * spacing,
            z * spacing - (gridSize.z - 1) * spacing / 2f
        );

        GameObject pointObj = Instantiate(controlPointPrefab, position, Quaternion.identity, transform);
        pointObj.name = $"Point_{x}_{y}_{z}";

        ControlPoint cp = pointObj.GetComponent<ControlPoint>();
        if (cp == null)
        {
            cp = pointObj.AddComponent<ControlPoint>();
        }

        cp.Initialize(new Vector3Int(x, y, z), position, this);
        controlPoints.Add(cp);

        // Ajouter le système de fumée amélioré
        if (enablePointSmoke)
        {
            ControlPointSmoke smoke = pointObj.AddComponent<ControlPointSmoke>();
            // Appliquer la couleur et l'opacité globales
            // Note: Les paramètres par défaut du script sont déjà bons
        }
    }

    void CreateAllConnections()
    {
        for (int i = 0; i < controlPoints.Count; i++)
        {
            Vector3Int coord = controlPoints[i].gridCoord;

            TryCreateConnection(coord, coord + Vector3Int.right);
            TryCreateConnection(coord, coord + Vector3Int.up);
            TryCreateConnection(coord, coord + new Vector3Int(0, 0, 1));
        }
    }

    void TryCreateConnection(Vector3Int from, Vector3Int to)
    {
        ControlPoint fromPoint = GetControlPoint(from);
        ControlPoint toPoint = GetControlPoint(to);

        if (fromPoint != null && toPoint != null)
        {
            CreateLine(fromPoint, toPoint);
        }
    }

    void CreateLine(ControlPoint point1, ControlPoint point2)
    {
        GameObject lineObj = new GameObject($"Line_{point1.gridCoord}_{point2.gridCoord}");
        lineObj.transform.parent = transform;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.SetPosition(0, point1.transform.position);
        lr.SetPosition(1, point2.transform.position);

        point1.AddConnection(point2, lr, 0);
        point2.AddConnection(point1, lr, 1);

        lineRenderers.Add(lr);

        // Ajouter le système de particules volumétrique le long de la ligne
        if (enableLineSmoke)
        {
            LineParticleEmitter emitter = lineObj.AddComponent<LineParticleEmitter>();
        }
    }

    void CreateVolumeFiller()
    {
        GameObject volumeObj = new GameObject("VolumeFiller");
        volumeObj.transform.parent = transform;
        volumeObj.transform.localPosition = new Vector3(0, (gridSize.y - 1) * spacing / 2f, 0);

        volumeFiller = volumeObj.AddComponent<CloudVolumeFiller>();

        // Calculer la taille du volume basée sur la grille
        Vector3 volumeSize = new Vector3(
            (gridSize.x - 1) * spacing + 2f, // +2 pour avoir de la marge
            (gridSize.y - 1) * spacing + 2f,
            (gridSize.z - 1) * spacing + 2f
        );

        volumeFiller.UpdateVolumeSize(volumeSize);
        volumeFiller.SetEmissionRate(volumeFillRate);
    }

    ControlPoint GetControlPoint(Vector3Int coord)
    {
        return controlPoints.Find(cp => cp.gridCoord == coord);
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        foreach (var point in controlPoints)
        {
            if (point != null && point.gameObject != null)
            {
                DestroyImmediate(point.gameObject);
            }
        }
        controlPoints.Clear();

        foreach (var line in lineRenderers)
        {
            if (line != null && line.gameObject != null)
            {
                DestroyImmediate(line.gameObject);
            }
        }
        lineRenderers.Clear();

        if (volumeFiller != null && volumeFiller.gameObject != null)
        {
            DestroyImmediate(volumeFiller.gameObject);
            volumeFiller = null;
        }

        isGenerated = false;
    }

    [ContextMenu("Reset All Points")]
    public void ResetAllPoints()
    {
        foreach (var point in controlPoints)
        {
            point.ResetPosition();
        }

        Debug.Log("All points reset to original positions");
    }

    [ContextMenu("Toggle Smoke")]
    public void ToggleSmoke()
    {
        enablePointSmoke = !enablePointSmoke;
        enableLineSmoke = !enableLineSmoke;

        foreach (var point in controlPoints)
        {
            ControlPointSmoke smoke = point.GetComponent<ControlPointSmoke>();
            if (smoke != null)
            {
                smoke.SetEmissionEnabled(enablePointSmoke);
            }
        }

        foreach (var line in lineRenderers)
        {
            LineParticleEmitter emitter = line.GetComponent<LineParticleEmitter>();
            if (emitter != null)
            {
                emitter.enabled = enableLineSmoke;
            }
        }

        Debug.Log($"Smoke: Point={enablePointSmoke}, Line={enableLineSmoke}");
    }

    [ContextMenu("Toggle Volume Fill")]
    public void ToggleVolumeFill()
    {
        enableVolumeFill = !enableVolumeFill;

        if (volumeFiller != null)
        {
            volumeFiller.gameObject.SetActive(enableVolumeFill);
        }

        Debug.Log($"Volume Fill: {enableVolumeFill}");
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !isGenerated) return;

        Gizmos.color = Color.green;

        Vector3 size = new Vector3(
            (gridSize.x - 1) * spacing,
            (gridSize.y - 1) * spacing,
            (gridSize.z - 1) * spacing
        );

        Gizmos.DrawWireCube(transform.position + new Vector3(0, size.y / 2f, 0), size);
    }
}