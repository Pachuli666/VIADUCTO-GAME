using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadGenerator : MonoBehaviour
{
    [Header("Road Configuration")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxRoadSegments = 3;

    private float segmentLength;
    private float triggerDistance;
    private List<GameObject> roadSegments = new List<GameObject>();
    private Vector3 nextSpawnPosition;

    void Start()
    {
        InitializeRoadSystem();
    }

    void Update()
    {
        CheckPlayerPosition();
    }

    private void InitializeRoadSystem()
    {
        // Calcular dimensiones del segmento
        MeshRenderer thisRenderer = GetComponent<MeshRenderer>();

        if (thisRenderer != null)
        {
            segmentLength = thisRenderer.bounds.size.x;
            float rightEdge = thisRenderer.bounds.max.x;
            nextSpawnPosition = new Vector3(
                rightEdge + (segmentLength * 0.5f),
                transform.position.y,
                transform.position.z
            );
        }
        else
        {
            segmentLength = 10f * transform.localScale.x;
            nextSpawnPosition = transform.position + Vector3.right * segmentLength;
        }

        triggerDistance = segmentLength * 1.5f;

        // Generar segmentos iniciales
        for (int i = 0; i < maxRoadSegments; i++)
        {
            GenerateRoadSegment();
        }
    }

    private void CheckPlayerPosition()
    {
        if (player == null || roadSegments.Count == 0) return;

        GameObject lastSegment = roadSegments[roadSegments.Count - 1];
        float lastSegmentEnd = lastSegment.transform.position.x + (segmentLength * 0.5f);
        float distanceToEnd = lastSegmentEnd - player.position.x;

        if (distanceToEnd <= triggerDistance)
        {
            GenerateSingleSegment();
        }
    }

    private void GenerateSingleSegment()
    {
        GenerateRoadSegment();

        // Eliminar segmento más antiguo si excede el máximo
        if (roadSegments.Count > maxRoadSegments)
        {
            GameObject oldSegment = roadSegments[0];
            roadSegments.RemoveAt(0);
            Destroy(oldSegment);
        }
    }

    private void GenerateRoadSegment()
    {
        if (roadPrefab == null) return;

        // Calcular posición del nuevo segmento
        if (roadSegments.Count > 0)
        {
            GameObject lastSegment = roadSegments[roadSegments.Count - 1];
            nextSpawnPosition = new Vector3(
                lastSegment.transform.position.x + segmentLength,
                lastSegment.transform.position.y,
                lastSegment.transform.position.z
            );
        }

        // Crear nuevo segmento
        GameObject newSegment = Instantiate(roadPrefab, nextSpawnPosition, roadPrefab.transform.rotation);

        // Desactivar colisionador para evitar conflictos físicos
        Collider newCollider = newSegment.GetComponent<Collider>();
        if (newCollider != null)
        {
            newCollider.enabled = false;
        }

        roadSegments.Add(newSegment);
        newSegment.name = $"RoadSegment_{roadSegments.Count}";
    }
}