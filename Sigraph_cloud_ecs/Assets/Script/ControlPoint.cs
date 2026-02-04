using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ControlPoint : MonoBehaviour
{
    [Header("Info")]
    public Vector3Int gridCoord;
    public Vector3 originalPosition;

    [Header("Visual")]
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Color dragColor = Color.red;

    private CloudMeshController controller;
    private List<ConnectionData> connections = new List<ConnectionData>();

    // Interaction
    private bool isDragging = false;
    private bool isHovering = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private Renderer meshRenderer;

    private class ConnectionData
    {
        public ControlPoint otherPoint;
        public LineRenderer lineRenderer;
        public int lineIndex; // 0 = début, 1 = fin
    }

    void Awake()
    {
        mainCamera = Camera.main;
        meshRenderer = GetComponent<Renderer>();
    }

    public void Initialize(Vector3Int coord, Vector3 position, CloudMeshController ctrl)
    {
        gridCoord = coord;
        originalPosition = position;
        controller = ctrl;
        transform.position = position;

        UpdateColor();
    }

    public void AddConnection(ControlPoint other, LineRenderer lr, int index)
    {
        connections.Add(new ConnectionData
        {
            otherPoint = other,
            lineRenderer = lr,
            lineIndex = index
        });
    }

    public void UpdateConnections()
    {
        foreach (var conn in connections)
        {
            if (conn.lineRenderer != null)
            {
                conn.lineRenderer.SetPosition(conn.lineIndex, transform.position);
            }
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;
        UpdateColor();
    }

    void OnMouseExit()
    {
        isHovering = false;
        UpdateColor();
    }

    void OnMouseDown()
    {
        isDragging = true;

        // Calculer l'offset entre la souris et le point
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPos;

        UpdateColor();
    }

    void OnMouseDrag()
    {
        if (isDragging)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos + dragOffset;
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        UpdateColor();
    }

    Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();

        // Garder la distance Z de la caméra constante
        float distanceToCamera = Vector3.Distance(mainCamera.transform.position, transform.position);
        mouseScreenPos.z = distanceToCamera;

        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    void UpdateColor()
    {
        if (meshRenderer == null) return;

        Color targetColor = normalColor;

        if (isDragging)
            targetColor = dragColor;
        else if (isHovering)
            targetColor = hoverColor;

        meshRenderer.material.SetColor("_EmissionColor", targetColor);
        meshRenderer.material.SetColor("_Color", targetColor);
    }

    // Méthode pour réinitialiser la position
    public void ResetPosition()
    {
        transform.position = originalPosition;
    }
}