using System.Collections.Generic;
using UnityEngine;

public class InfiniteRoadGenerator : MonoBehaviour
{
    // Configuracion del sistema de carretera
    [Header("Road Configuration")]
    [SerializeField] private GameObject roadPrefab; // Prefab del segmento de carretera
    [SerializeField] private Transform player; // Referencia al jugador para calcular distancias
    [SerializeField] private int maxRoadSegments = 3; // Numero maximo de segmentos activos

    // Configuracion de obstaculos
    [Header("Obstacle Configuration")]
    [SerializeField] private float laneWidth = 3f; // Ancho entre carriles
    [SerializeField] private int maxObstaclesPerSegment = 8; // Maximo obstaculos por segmento
    [SerializeField] private float obstacleSafeDistance = 6f; // Distancia segura del jugador
    [SerializeField] private float minObstacleSpacing = 6f; // Espaciado minimo entre obstaculos
    [SerializeField] private GameObject[] obstaclePrefabs; // Array de prefabs de obstaculos

    // Configuracion de paradas de autobus
    [Header("Bus Stop Configuration")]
    [SerializeField] private GameObject busStopPrefab; // Prefab de la parada
    [SerializeField] private int busStopInterval = 5; // Cada cuantos segmentos generar parada
    [SerializeField] private float busStopOffsetZ = 2.5f; // Distancia lateral del centro
    [SerializeField] private float busStopSafeDistance = 10f; // Distancia segura del jugador
    [SerializeField] private float busStopClearanceDistance = 5f; // Distancia libre alrededor de paradas

    // Variables internas del sistema
    private float segmentLength; // Longitud de cada segmento
    private float triggerDistance; // Distancia para generar nuevo segmento
    private float roadSegmentGroundY; // Posicion Y del suelo
    private List<GameObject> roadSegments = new List<GameObject>(); // Lista de segmentos activos
    private List<List<GameObject>> obstaclesPerSegment = new List<List<GameObject>>(); // Obstaculos por segmento
    private List<GameObject> busStopsPerSegment = new List<GameObject>(); // Paradas por segmento
    private Vector3 nextSpawnPosition; // Posicion del proximo segmento
    private int segmentCount = 0; // Contador de segmentos generados

    // Variables para controlar el spawning de paradas
    private bool lastBusStopOnLeft = false; // Ultimo lado donde se coloco una parada

    // Posiciones de los 3 carriles izquierda centro derecha
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

    // Configura las posiciones de los 3 carriles
    private void SetupLanes()
    {
        lanePositions[0] = -laneWidth * 0.6f; // Carril izquierdo
        lanePositions[1] = 0f; // Carril central
        lanePositions[2] = laneWidth * 0.6f; // Carril derecho
    }

    // Inicializa el sistema completo de carretera
    private void InitializeRoadSystem()
    {
        if (roadPrefab != null)
        {
            // Crea una instancia temporal para obtener las dimensiones reales
            GameObject tempSegment = Instantiate(roadPrefab, Vector3.zero, roadPrefab.transform.rotation);
            MeshRenderer tempRenderer = tempSegment.GetComponent<MeshRenderer>();

            if (tempRenderer != null)
            {
                // Obtiene la longitud del segmento desde sus bounds
                segmentLength = tempRenderer.bounds.size.x;
                // Obtiene la Y del suelo desde el punto mas alto (superficie de la carretera)
                roadSegmentGroundY = tempRenderer.bounds.max.y;
            }
            else
            {
                // Valores de respaldo si no hay MeshRenderer
                segmentLength = 10f * roadPrefab.transform.localScale.x;
                roadSegmentGroundY = transform.position.y;
            }

            // Destruye la instancia temporal
            DestroyImmediate(tempSegment);
        }
        else
        {
            // Valores por defecto si no hay prefab
            segmentLength = 10f;
            roadSegmentGroundY = transform.position.y;
        }

        // Configura la posicion inicial
        nextSpawnPosition = transform.position;
        // La distancia para generar nuevo segmento es 1.5 veces la longitud
        triggerDistance = segmentLength * 1.5f;

        // Genera los segmentos iniciales
        for (int i = 0; i < maxRoadSegments; i++)
        {
            GenerateRoadSegment();
        }
    }

    // Verifica la posicion del jugador para generar nuevos segmentos
    private void CheckPlayerPosition()
    {
        if (player == null || roadSegments.Count == 0) return;

        // Obtiene el ultimo segmento generado
        GameObject lastSegment = roadSegments[roadSegments.Count - 1];
        // Calcula donde termina ese segmento
        float lastSegmentEnd = lastSegment.transform.position.x + (segmentLength * 0.5f);
        // Distancia del jugador al final del ultimo segmento
        float distanceToEnd = lastSegmentEnd - player.position.x;

        // Si el jugador esta cerca del final genera un nuevo segmento
        if (distanceToEnd <= triggerDistance)
        {
            GenerateSingleSegment();
        }
    }

    // Genera un nuevo segmento y limpia el mas antiguo si es necesario
    private void GenerateSingleSegment()
    {
        GenerateRoadSegment();

        // Si excede el maximo elimina el segmento mas antiguo
        if (roadSegments.Count > maxRoadSegments)
        {
            // Obtiene referencias a los objetos del primer segmento
            GameObject oldSegment = roadSegments[0];
            List<GameObject> oldObstacles = obstaclesPerSegment[0];
            GameObject oldBusStop = busStopsPerSegment[0];

            // Remueve de las listas
            roadSegments.RemoveAt(0);
            obstaclesPerSegment.RemoveAt(0);
            busStopsPerSegment.RemoveAt(0);

            // Destruye todos los obstaculos del segmento antiguo
            foreach (GameObject obstacle in oldObstacles)
            {
                if (obstacle != null)
                    Destroy(obstacle);
            }

            // Destruye la parada si existe
            if (oldBusStop != null)
                Destroy(oldBusStop);

            // Destruye el segmento
            Destroy(oldSegment);
        }
    }

    // Genera un nuevo segmento de carretera completo
    private void GenerateRoadSegment()
    {
        if (roadPrefab == null) return;

        // Si ya hay segmentos calcula la posicion del siguiente
        if (roadSegments.Count > 0)
        {
            GameObject lastSegment = roadSegments[roadSegments.Count - 1];
            MeshRenderer lastRenderer = lastSegment.GetComponent<MeshRenderer>();

            if (lastRenderer != null)
            {
                // Usa los bounds reales para colocar el siguiente segmento
                float lastSegmentRightEdge = lastRenderer.bounds.max.x;

                nextSpawnPosition = new Vector3(
                    lastSegmentRightEdge + (segmentLength * 0.5f), // X donde termina el anterior
                    lastSegment.transform.position.y, // Misma Y
                    lastSegment.transform.position.z // Misma Z
                );

                // Actualiza la Y del suelo (superficie superior de la carretera)
                roadSegmentGroundY = lastRenderer.bounds.max.y;
            }
            else
            {
                // Metodo de respaldo sin bounds
                nextSpawnPosition = new Vector3(
                    lastSegment.transform.position.x + segmentLength,
                    lastSegment.transform.position.y,
                    lastSegment.transform.position.z
                );
            }
        }

        // Crea el nuevo segmento
        GameObject newSegment = Instantiate(roadPrefab, nextSpawnPosition, roadPrefab.transform.rotation);

        // Desactiva el colisionador para evitar problemas fisicos
        Collider newCollider = newSegment.GetComponent<Collider>();
        if (newCollider != null)
        {
            newCollider.enabled = false;
        }

        // Agrega a la lista y nombra el objeto
        roadSegments.Add(newSegment);
        newSegment.name = $"RoadSegment_{roadSegments.Count}";

        // Incrementa el contador
        segmentCount++;

        // PRIMERO genera parada de autobus si corresponde
        GameObject busStop = GenerateBusStopForSegment(nextSpawnPosition);
        busStopsPerSegment.Add(busStop);

        // DESPUES genera obstaculos para este segmento (evitando las paradas)
        List<GameObject> segmentObstacles = GenerateObstaclesForSegment(nextSpawnPosition);
        obstaclesPerSegment.Add(segmentObstacles);
    }

    // Genera una parada de autobus para el segmento dado
    private GameObject GenerateBusStopForSegment(Vector3 segmentPosition)
    {
        // No generar si no hay prefab
        if (busStopPrefab == null) return null;

        // Solo generar parada cada 'busStopInterval' segmentos
        if (segmentCount % busStopInterval != 0) return null;

        // No generar si esta muy cerca del jugador
        if (IsPositionNearPlayer(segmentPosition, busStopSafeDistance)) return null;

        // Determinar en que lado colocar la parada (alternando)
        bool spawnOnLeft = !lastBusStopOnLeft;

        // Actualizar el registro del ultimo lado usado
        lastBusStopOnLeft = spawnOnLeft;

        // Calcula la posicion Z lateral
        float busStopZ = segmentPosition.z + (spawnOnLeft ? -busStopOffsetZ : busStopOffsetZ);

        // Calcula la Y correcta para que toque el suelo
        float busStopY = CalculateBusStopGroundY();

        Vector3 busStopPosition = new Vector3(
            segmentPosition.x, // Misma X del segmento
            busStopY, // Y ajustada al suelo
            busStopZ // Z lateral
        );

        // Rotacion base del prefab
        Quaternion busStopRotation = busStopPrefab.transform.rotation;
        // Si esta del lado derecho rota 180 grados
        if (!spawnOnLeft)
        {
            busStopRotation *= Quaternion.Euler(0f, 180f, 0f);
        }

        // Crea la parada
        GameObject newBusStop = Instantiate(busStopPrefab, busStopPosition, busStopRotation);
        newBusStop.name = $"BusStop_Segment_{segmentCount}_{(spawnOnLeft ? "Left" : "Right")}";

        // Agrega colisionador si no tiene
        if (newBusStop.GetComponent<Collider>() == null)
        {
            newBusStop.AddComponent<BoxCollider>().isTrigger = true;
        }

        Debug.Log($"Bus stop generated at segment {segmentCount} on {(spawnOnLeft ? "RIGHT" : "LEFT")} side");

        return newBusStop;
    }

    // Calcula la Y correcta para que la parada toque el suelo
    private float CalculateBusStopGroundY()
    {
        if (busStopPrefab == null) return roadSegmentGroundY;

        // Crea instancia temporal para obtener dimensiones
        GameObject tempBusStop = Instantiate(busStopPrefab, Vector3.zero, busStopPrefab.transform.rotation);
        MeshRenderer busStopRenderer = tempBusStop.GetComponent<MeshRenderer>();

        float adjustedY = roadSegmentGroundY;

        if (busStopRenderer != null)
        {
            // Calcula cuanto esta la parte inferior por debajo del centro
            float busStopBottomOffset = busStopRenderer.bounds.min.y - tempBusStop.transform.position.y;
            // Ajusta la Y para que la parte inferior toque el suelo
            adjustedY = roadSegmentGroundY - busStopBottomOffset;
        }
        else
        {
            // Valor de respaldo
            adjustedY = roadSegmentGroundY;
        }

        // Destruye la instancia temporal
        DestroyImmediate(tempBusStop);

        return adjustedY;
    }

    // Calcula la Y correcta para que el obstáculo toque el suelo
    private float CalculateObstacleGroundY(GameObject obstaclePrefab)
    {
        if (obstaclePrefab == null) return roadSegmentGroundY;

        // Crea instancia temporal para obtener dimensiones
        GameObject tempObstacle = Instantiate(obstaclePrefab, Vector3.zero, obstaclePrefab.transform.rotation);
        MeshRenderer obstacleRenderer = tempObstacle.GetComponent<MeshRenderer>();

        float adjustedY = roadSegmentGroundY;

        if (obstacleRenderer != null)
        {
            // Calcula cuanto esta la parte inferior por debajo del centro
            float obstacleBottomOffset = obstacleRenderer.bounds.min.y - tempObstacle.transform.position.y;
            // Ajusta la Y para que la parte inferior toque el suelo
            adjustedY = roadSegmentGroundY - obstacleBottomOffset;
        }
        else
        {
            // Valor de respaldo
            adjustedY = roadSegmentGroundY;
        }

        // Destruye la instancia temporal
        DestroyImmediate(tempObstacle);

        return adjustedY;
    }

    // Verifica si una posición está cerca de alguna parada de autobús
    private bool IsPositionNearBusStop(Vector3 position, float clearanceDistance)
    {
        // Verificar paradas en segmentos actuales
        foreach (GameObject busStop in busStopsPerSegment)
        {
            if (busStop != null)
            {
                float distance = Vector3.Distance(position, busStop.transform.position);
                if (distance < clearanceDistance)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Sobrecarga del método para usar la distancia de clearance por defecto
    private bool IsPositionNearBusStop(Vector3 position)
    {
        return IsPositionNearBusStop(position, busStopClearanceDistance);
    }

    // Genera obstaculos para un segmento dado
    private List<GameObject> GenerateObstaclesForSegment(Vector3 segmentPosition)
    {
        List<GameObject> obstacles = new List<GameObject>();

        // No generar si no hay prefabs de obstaculos
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return obstacles;

        // No generar si esta muy cerca del jugador
        if (IsPositionNearPlayer(segmentPosition, obstacleSafeDistance))
        {
            return obstacles;
        }

        // Numero aleatorio de obstaculos a generar
        int numObstacles = Random.Range(1, maxObstaclesPerSegment + 1);

        // Lista para evitar que los obstaculos se superpongan
        List<Vector3> occupiedPositions = new List<Vector3>();

        for (int i = 0; i < numObstacles; i++)
        {
            // Busca una posicion valida para el obstaculo
            Vector3 obstaclePosition = GetRandomObstaclePosition(segmentPosition, occupiedPositions);

            if (obstaclePosition != Vector3.zero) // Si encontro posicion valida
            {
                // Agrega la posicion a las ocupadas
                occupiedPositions.Add(obstaclePosition);

                // Selecciona un prefab aleatorio
                GameObject obstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

                // Calcula la Y correcta para que toque el suelo
                float correctY = CalculateObstacleGroundY(obstaclePrefab);
                obstaclePosition.y = correctY;

                // Rotacion aleatoria pequena
                Quaternion obstacleRotation = Quaternion.Euler(0, Random.Range(-5f, 5f), 0);

                // Crea el obstaculo
                GameObject newObstacle = Instantiate(obstaclePrefab, obstaclePosition, obstacleRotation);
                newObstacle.name = $"Obstacle_{i}_Segment_{roadSegments.Count}";

                // Agrega colisionador si no tiene
                if (newObstacle.GetComponent<Collider>() == null)
                {
                    newObstacle.AddComponent<BoxCollider>();
                }

                obstacles.Add(newObstacle);
            }
        }

        return obstacles;
    }

    // Busca una posicion aleatoria valida para un obstaculo
    private Vector3 GetRandomObstaclePosition(Vector3 segmentCenter, List<Vector3> occupiedPositions)
    {
        int maxAttempts = 30; // Aumentamos los intentos

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Selecciona un carril aleatorio de los 3 disponibles
            int laneIndex = Random.Range(0, 3);
            float laneZ = segmentCenter.z + lanePositions[laneIndex];

            // Posicion X aleatoria dentro del segmento
            float randomX = Random.Range(
                segmentCenter.x - segmentLength * 0.4f,
                segmentCenter.x + segmentLength * 0.4f
            );

            Vector3 potentialPosition = new Vector3(
                randomX,
                segmentCenter.y, // Y temporal, se ajustará después
                laneZ
            );

            // Verifica que no este muy cerca del jugador
            if (IsPositionNearPlayer(potentialPosition, obstacleSafeDistance))
            {
                continue;
            }

            // VERIFICACIÓN CORREGIDA: Verifica que no esté frente a paradas de autobús usando la sobrecarga
            if (IsPositionNearBusStop(potentialPosition))
            {
                continue;
            }

            // Verifica que no este muy cerca de otros obstaculos
            bool tooClose = false;
            foreach (Vector3 occupiedPos in occupiedPositions)
            {
                float distance = Vector3.Distance(potentialPosition, occupiedPos);

                // Si estan en el mismo carril necesitan mas separacion en X
                if (Mathf.Approximately(occupiedPos.z, potentialPosition.z))
                {
                    if (Mathf.Abs(occupiedPos.x - potentialPosition.x) < minObstacleSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                // Si estan en carriles diferentes pero muy cerca en X tambien evitar
                else if (Mathf.Abs(occupiedPos.x - potentialPosition.x) < minObstacleSpacing * 0.5f)
                {
                    tooClose = true;
                    break;
                }
            }

            // Si encontro una posicion valida la retorna
            if (!tooClose)
            {
                return potentialPosition;
            }
        }

        // No encontro posicion valida despues de todos los intentos
        return Vector3.zero;
    }

    // Verifica si una posicion esta muy cerca del jugador
    private bool IsPositionNearPlayer(Vector3 position, float customDistance)
    {
        if (player == null) return false;

        float distance = Vector3.Distance(position, player.position);
        return distance < customDistance;
    }

    // Sobrecarga usando la distancia por defecto
    private bool IsPositionNearPlayer(Vector3 position)
    {
        return IsPositionNearPlayer(position, obstacleSafeDistance);
    }

    // Metodo publico para cambiar la dificultad dinamicamente
    public void SetDifficulty(int maxObstacles, float safeDistance)
    {
        maxObstaclesPerSegment = Mathf.Clamp(maxObstacles, 0, 3);
        obstacleSafeDistance = safeDistance;
    }

    // Metodo publico para cambiar el intervalo de paradas
    public void SetBusStopInterval(int interval)
    {
        busStopInterval = Mathf.Max(1, interval); // Minimo 1 segmento
    }

    // Metodo publico para cambiar la distancia lateral de las paradas
    public void SetBusStopOffset(float offset)
    {
        busStopOffsetZ = Mathf.Max(0f, offset); // No valores negativos
    }

    // Método público para cambiar la distancia de clearance de paradas
    public void SetBusStopClearanceDistance(float distance)
    {
        busStopClearanceDistance = Mathf.Max(0f, distance);
    }
}