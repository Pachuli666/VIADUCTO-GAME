using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadGenerator : MonoBehaviour
{
    [Header("Road Configuration")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxRoadSegments = 3;

    [Header("Obstacle Configuration")]
    [SerializeField] private GameObject[] obstaclePrefabs; // Array de prefabs de obstáculos
    [SerializeField] private float laneWidth = 3f; // Ancho de cada carril
    [SerializeField] private int maxObstaclesPerSegment = 2; // Máximo obstáculos por segmento
    [SerializeField] private float obstacleSafeDistance = 8f; // Distancia segura del jugador

    private float segmentLength;
    private float triggerDistance;
    private List<GameObject> roadSegments = new List<GameObject>();
    private List<List<GameObject>> obstaclesPerSegment = new List<List<GameObject>>();
    private Vector3 nextSpawnPosition;

    // Posiciones de los 3 carriles (izquierda, centro, derecha)
    private float[] lanePositions = new float[3];

    void Start()
    {
        InitializeRoadSystem();
        SetupLanes();
    }

    void Update()
    {
        CheckPlayerPosition();
    }

    private void SetupLanes()
    {
        // Configurar las posiciones Z de los 3 carriles más centradas en el plano
        lanePositions[0] = -laneWidth * 0.6f; // Carril izquierdo (más hacia el centro)
        lanePositions[1] = 0f;                 // Carril central
        lanePositions[2] = laneWidth * 0.6f;   // Carril derecho (más hacia el centro)
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
            List<GameObject> oldObstacles = obstaclesPerSegment[0];

            roadSegments.RemoveAt(0);
            obstaclesPerSegment.RemoveAt(0);

            // Destruir obstáculos del segmento antiguo
            foreach (GameObject obstacle in oldObstacles)
            {
                if (obstacle != null)
                    Destroy(obstacle);
            }

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

        // Generar obstáculos para este segmento
        List<GameObject> segmentObstacles = GenerateObstaclesForSegment(nextSpawnPosition);
        obstaclesPerSegment.Add(segmentObstacles);
    }

    private List<GameObject> GenerateObstaclesForSegment(Vector3 segmentPosition)
    {
        List<GameObject> obstacles = new List<GameObject>();

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return obstacles;

        // Verificar si el segmento está demasiado cerca del jugador
        if (IsPositionNearPlayer(segmentPosition))
        {
            return obstacles; // No generar obstáculos si está muy cerca del jugador
        }

        // Generar número aleatorio de obstáculos (0 a maxObstaclesPerSegment)
        int numObstacles = Random.Range(0, maxObstaclesPerSegment + 1);

        // Si no hay obstáculos, retornar lista vacía
        if (numObstacles == 0) return obstacles;

        // Lista para rastrear carriles ocupados
        List<int> occupiedLanes = new List<int>();

        for (int i = 0; i < numObstacles; i++)
        {
            Vector3 obstaclePosition = GetRandomObstaclePosition(segmentPosition, occupiedLanes);

            if (obstaclePosition != Vector3.zero) // Posición válida encontrada
            {
                // Seleccionar prefab aleatorio de obstáculo
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

                // Los obstáculos están parados, así que rotación mínima
                Quaternion obstacleRotation = Quaternion.Euler(0, Random.Range(-5f, 5f), 0);

                GameObject newObstacle = Instantiate(obstaclePrefab, obstaclePosition, obstacleRotation);
                newObstacle.name = $"Obstacle_{i}_Segment_{roadSegments.Count}";

                // Asegurar que tenga colisionador para detectar colisiones
                if (newObstacle.GetComponent<Collider>() == null)
                {
                    newObstacle.AddComponent<BoxCollider>();
                }

                obstacles.Add(newObstacle);
            }
        }

        return obstacles;
    }

    private Vector3 GetRandomObstaclePosition(Vector3 segmentCenter, List<int> occupiedLanes)
    {
        int attempts = 10; // Máximo número de intentos

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            // Seleccionar carril aleatorio que no esté ocupado
            List<int> availableLanes = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                if (!occupiedLanes.Contains(i))
                {
                    availableLanes.Add(i);
                }
            }

            // Si no hay carriles disponibles, salir
            if (availableLanes.Count == 0) break;

            int laneIndex = availableLanes[Random.Range(0, availableLanes.Count)];
            float laneZ = segmentCenter.z + lanePositions[laneIndex];

            // Posición X más centrada en el segmento para mejor distribución
            float randomX = Random.Range(
                segmentCenter.x - segmentLength * 0.25f,
                segmentCenter.x + segmentLength * 0.25f
            );

            Vector3 potentialPosition = new Vector3(
                randomX,
                segmentCenter.y + 0.5f, // Elevar sobre el plano
                laneZ
            );

            // Verificar si está muy cerca del jugador
            if (!IsPositionNearPlayer(potentialPosition))
            {
                // Marcar carril como ocupado
                occupiedLanes.Add(laneIndex);
                return potentialPosition;
            }
        }

        return Vector3.zero; // No se encontró posición válida
    }

    private bool IsPositionNearPlayer(Vector3 position)
    {
        if (player == null) return false;

        float distance = Vector3.Distance(position, player.position);
        return distance < obstacleSafeDistance;
    }

    // Método público para obtener información de debug
    public void GetDebugInfo()
    {
        Debug.Log($"Segmentos activos: {roadSegments.Count}");
        Debug.Log($"Longitud de segmento: {segmentLength}");

        int totalObstacles = 0;
        foreach (var obstacleList in obstaclesPerSegment)
        {
            totalObstacles += obstacleList.Count;
        }
        Debug.Log($"Total de obstáculos activos: {totalObstacles}");
    }

    // Método para cambiar dificultad dinámicamente
    public void SetDifficulty(int maxObstacles, float safeDistance)
    {
        maxObstaclesPerSegment = Mathf.Clamp(maxObstacles, 0, 3); // Máximo 3 (uno por carril)
        obstacleSafeDistance = safeDistance;
    }
}