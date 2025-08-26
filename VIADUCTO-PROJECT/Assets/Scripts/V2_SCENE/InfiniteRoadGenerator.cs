using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadGenerator : MonoBehaviour
{
    [Header("Road Configuration")]
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private int maxRoadSegments = 3;

    [Header("Obstacle Configuration")]
    [SerializeField] private GameObject[] obstaclePrefabs; // Array de prefabs de obst�culos
    [SerializeField] private float laneWidth = 3f; // Ancho de cada carril
    [SerializeField] private int maxObstaclesPerSegment = 2; // M�ximo obst�culos por segmento
    [SerializeField] private float obstacleSafeDistance = 8f; // Distancia segura del jugador
    [SerializeField] private float minObstacleSpacing = 2f; // Separaci�n m�nima entre obst�culos en X

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
        // Configurar las posiciones Z de los 3 carriles m�s centradas en el plano
        lanePositions[0] = -laneWidth * 0.6f; // Carril izquierdo (m�s hacia el centro)
        lanePositions[1] = 0f;                 // Carril central
        lanePositions[2] = laneWidth * 0.6f;   // Carril derecho (m�s hacia el centro)
    }

    private void InitializeRoadSystem()
    {
        // Calcular dimensiones del segmento creando una instancia temporal
        if (roadPrefab != null)
        {
            // Crear instancia temporal para obtener el tama�o real
            GameObject tempSegment = Instantiate(roadPrefab, Vector3.zero, roadPrefab.transform.rotation);
            MeshRenderer tempRenderer = tempSegment.GetComponent<MeshRenderer>();

            if (tempRenderer != null)
            {
                segmentLength = tempRenderer.bounds.size.x;
            }
            else
            {
                segmentLength = 10f * roadPrefab.transform.localScale.x;
            }

            // Destruir la instancia temporal
            DestroyImmediate(tempSegment);
        }
        else
        {
            segmentLength = 10f;
        }

        // Establecer posici�n inicial
        nextSpawnPosition = transform.position;
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

        // Eliminar segmento m�s antiguo si excede el m�ximo
        if (roadSegments.Count > maxRoadSegments)
        {
            GameObject oldSegment = roadSegments[0];
            List<GameObject> oldObstacles = obstaclesPerSegment[0];

            roadSegments.RemoveAt(0);
            obstaclesPerSegment.RemoveAt(0);

            // Destruir obst�culos del segmento antiguo
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

        // Calcular posici�n del nuevo segmento correctamente
        if (roadSegments.Count > 0)
        {
            GameObject lastSegment = roadSegments[roadSegments.Count - 1];

            // Obtener el renderer del �ltimo segmento instanciado
            MeshRenderer lastRenderer = lastSegment.GetComponent<MeshRenderer>();
            if (lastRenderer != null)
            {
                // Usar los bounds reales del objeto instanciado
                float lastSegmentRightEdge = lastRenderer.bounds.max.x;

                // El nuevo segmento debe colocarse exactamente donde termina el anterior
                nextSpawnPosition = new Vector3(
                    lastSegmentRightEdge + (segmentLength * 0.5f),
                    lastSegment.transform.position.y,
                    lastSegment.transform.position.z
                );
            }
            else
            {
                // Fallback si no hay MeshRenderer
                nextSpawnPosition = new Vector3(
                    lastSegment.transform.position.x + segmentLength,
                    lastSegment.transform.position.y,
                    lastSegment.transform.position.z
                );
            }
        }

        // Crear nuevo segmento
        GameObject newSegment = Instantiate(roadPrefab, nextSpawnPosition, roadPrefab.transform.rotation);

        // Desactivar colisionador para evitar conflictos f�sicos
        Collider newCollider = newSegment.GetComponent<Collider>();
        if (newCollider != null)
        {
            newCollider.enabled = false;
        }

        roadSegments.Add(newSegment);
        newSegment.name = $"RoadSegment_{roadSegments.Count}";

        // Generar obst�culos para este segmento
        List<GameObject> segmentObstacles = GenerateObstaclesForSegment(nextSpawnPosition);
        obstaclesPerSegment.Add(segmentObstacles);
    }

    private List<GameObject> GenerateObstaclesForSegment(Vector3 segmentPosition)
    {
        List<GameObject> obstacles = new List<GameObject>();

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return obstacles;

        // Verificar si el segmento est� demasiado cerca del jugador
        if (IsPositionNearPlayer(segmentPosition))
        {
            return obstacles; // No generar obst�culos si est� muy cerca del jugador
        }

        // Generar n�mero aleatorio de obst�culos (1 a maxObstaclesPerSegment)
        // Cambiar de 0 a 1 para garantizar al menos un obst�culo por segmento
        int numObstacles = Random.Range(1, maxObstaclesPerSegment + 1);

        // Lista para rastrear posiciones ocupadas (con posici�n X y carril)
        List<Vector3> occupiedPositions = new List<Vector3>();

        for (int i = 0; i < numObstacles; i++)
        {
            Vector3 obstaclePosition = GetRandomObstaclePosition(segmentPosition, occupiedPositions);

            if (obstaclePosition != Vector3.zero) // Posici�n v�lida encontrada
            {
                // Agregar la posici�n a las ocupadas
                occupiedPositions.Add(obstaclePosition);

                // Seleccionar prefab aleatorio de obst�culo
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

                // Los obst�culos est�n parados, as� que rotaci�n m�nima
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

    private Vector3 GetRandomObstaclePosition(Vector3 segmentCenter, List<Vector3> occupiedPositions)
    {
        int maxAttempts = 20; // Incrementar intentos

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Seleccionar carril aleatorio
            int laneIndex = Random.Range(0, 3);
            float laneZ = segmentCenter.z + lanePositions[laneIndex];

            // Generar posici�n X aleatoria dentro del segmento
            float randomX = Random.Range(
                segmentCenter.x - segmentLength * 0.4f,
                segmentCenter.x + segmentLength * 0.4f
            );

            Vector3 potentialPosition = new Vector3(
                randomX,
                segmentCenter.y + 0.5f, // Elevar sobre el plano
                laneZ
            );

            // Verificar si est� muy cerca del jugador
            if (IsPositionNearPlayer(potentialPosition))
            {
                continue;
            }

            // Verificar si est� muy cerca de otros obst�culos
            bool tooClose = false;
            foreach (Vector3 occupiedPos in occupiedPositions)
            {
                float distance = Vector3.Distance(potentialPosition, occupiedPos);

                // Si est�n en el mismo carril, necesitan m�s separaci�n en X
                if (Mathf.Approximately(occupiedPos.z, potentialPosition.z))
                {
                    if (Mathf.Abs(occupiedPos.x - potentialPosition.x) < minObstacleSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                // Si est�n en carriles diferentes pero muy cerca en X, tambi�n evitar
                else if (Mathf.Abs(occupiedPos.x - potentialPosition.x) < minObstacleSpacing * 0.5f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                return potentialPosition;
            }
        }

        return Vector3.zero; // No se encontr� posici�n v�lida
    }

    private bool IsPositionNearPlayer(Vector3 position)
    {
        if (player == null) return false;

        float distance = Vector3.Distance(position, player.position);
        return distance < obstacleSafeDistance;
    }

    // M�todo p�blico para obtener informaci�n de debug
    public void GetDebugInfo()
    {
        int totalObstacles = 0;
        foreach (var obstacleList in obstaclesPerSegment)
        {
            totalObstacles += obstacleList.Count;
        }
    }

    // M�todo para cambiar dificultad din�micamente
    public void SetDifficulty(int maxObstacles, float safeDistance)
    {
        maxObstaclesPerSegment = Mathf.Clamp(maxObstacles, 0, 3); // M�ximo 3 (uno por carril)
        obstacleSafeDistance = safeDistance;
    }
}