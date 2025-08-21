using UnityEngine;

public class PeseroManager : MonoBehaviour
{
    [Header("Puntos de Ruta")]
    public Vector3[] routePoints; // Array de coordenadas que define el camino a seguir

    [Header("Velocidad")]
    public float startSpeed = 5f;          // Velocidad inicial del objeto
    public float speedIncrease = 0.5f;     // Cuánto aumenta la velocidad por segundo
    public float brakingForce = 2f;        // Fuerza de frenado al acercarse al último punto
    public float stopDistance = 5f;        // Distancia desde el último punto donde inicia el frenado

    [Header("Rotación")]
    public float rotationSpeed = 5f;       // Velocidad de rotación suave hacia el objetivo
    public float turnRadius = 3f;          // Radio de giro del camión (simula que gira fuera de su eje)
    public float maxTurnAngle = 45f;       // Ángulo máximo de giro por segundo

    // Variables de control interno
    private int currentPoint = 0;    // Índice del punto actual al que nos dirigimos
    private float currentSpeed;      // Velocidad actual (aumenta con el tiempo)
    private Vector3 movementDirection; // Dirección continua de movimiento
    private bool hasFinishedRoute = false; // Indica si ya terminó la ruta

    void Start()
    {
        // Inicializar la velocidad actual con la velocidad de inicio
        currentSpeed = startSpeed;

        // Si no se definieron puntos de ruta, crear una ruta básica hacia adelante
        if (routePoints.Length == 0)
        {
            routePoints = new Vector3[]
            {
                transform.position,                        // Posición actual
                transform.position + Vector3.forward * 10, // 10 unidades adelante
                transform.position + Vector3.forward * 20, // 20 unidades adelante
                transform.position + Vector3.forward * 30  // 30 unidades adelante
            };
        }

        // Establecer la dirección inicial hacia el primer punto objetivo
        if (routePoints.Length > 1)
        {
            movementDirection = (routePoints[1] - transform.position).normalized;
        }
    }

    void Update()
    {
        // Verificar si estamos cerca del último punto de la ruta
        bool shouldBrake = false;
        if (routePoints.Length > 0 && currentPoint >= routePoints.Length - 1)
        {
            Vector3 lastPoint = routePoints[routePoints.Length - 1];
            float distanceToLastPoint = Vector3.Distance(transform.position, lastPoint);

            // Activar frenos si estamos cerca del último punto
            shouldBrake = distanceToLastPoint <= stopDistance;

            // Marcar como terminada la ruta si llegamos muy cerca del último punto
            if (distanceToLastPoint <= 0.5f)
            {
                hasFinishedRoute = true;
            }
        }

        // Control de velocidad con fuerzas
        if (hasFinishedRoute)
        {
            // Si ya terminó la ruta, detener completamente
            currentSpeed = 0f;
        }
        else if (shouldBrake)
        {
            // Aplicar fuerza de frenado (resta de la velocidad)
            currentSpeed -= brakingForce * Time.deltaTime;

            // No permitir velocidad negativa
            currentSpeed = Mathf.Max(0f, currentSpeed);
        }
        else
        {
            // Aplicar fuerza de aceleración (suma a la velocidad)
            currentSpeed += speedIncrease * Time.deltaTime;
        }

        // Solo mover si tenemos velocidad
        if (currentSpeed > 0f && !hasFinishedRoute)
        {
            transform.position += movementDirection * currentSpeed * Time.deltaTime;
        }

        // Verificar si aún tenemos puntos de ruta por recorrer
        if (currentPoint < routePoints.Length)
        {
            // Obtener el punto de destino actual
            Vector3 target = routePoints[currentPoint];

            // Verificar si hemos llegado cerca del punto objetivo
            if (Vector3.Distance(transform.position, target) < 1f)
            {
                // Avanzar al siguiente punto de la ruta
                currentPoint++;

                // Si aún hay más puntos, actualizar la dirección hacia el siguiente
                if (currentPoint < routePoints.Length)
                {
                    Vector3 nextTarget = routePoints[currentPoint];
                    movementDirection = (nextTarget - transform.position).normalized;
                }
                // Si ya no hay más puntos, mantener la última dirección para seguir recto
                // (movementDirection se mantiene igual)
            }
            else
            {
                // Actualizar constantemente la dirección hacia el punto objetivo actual
                // Para simular el giro de camión, no cambiar la dirección tan bruscamente
                Vector3 targetDirection = (target - transform.position).normalized;

                // Calcular el ángulo entre la dirección actual y la dirección objetivo
                float angleToTarget = Vector3.SignedAngle(movementDirection, targetDirection, Vector3.up);

                // Limitar el ángulo de giro por frame para simular el radio de giro del camión
                float maxTurnThisFrame = maxTurnAngle * Time.deltaTime;
                float actualTurnAngle = Mathf.Clamp(angleToTarget, -maxTurnThisFrame, maxTurnThisFrame);

                // Aplicar la rotación limitada a la dirección de movimiento
                movementDirection = Quaternion.AngleAxis(actualTurnAngle, Vector3.up) * movementDirection;

                // Simular que el camión gira fuera de su eje aplicando un desplazamiento lateral
                Vector3 lateralOffset = Vector3.Cross(movementDirection, Vector3.up) * (actualTurnAngle * turnRadius * Time.deltaTime);
                transform.position += lateralOffset;
            }
        }

        // Rotar el objeto para que siempre apunte hacia la dirección de movimiento
        // El frente del objeto es la cara lateral derecha (eje X positivo)
        if (movementDirection != Vector3.zero)
        {
            // Calcular la rotación que debe tener el objeto basada en la dirección de movimiento
            // Usar Vector3.right como frente en lugar del Vector3.forward predeterminado
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

            // Aplicar rotación adicional de 90 grados en Y para que el lado derecho sea el frente
            targetRotation *= Quaternion.Euler(0, -90, 0);

            // Aplicar la rotación de manera suave para evitar cambios bruscos
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // Método para visualizar la ruta en el editor de Unity (solo en Scene view)
    void OnDrawGizmos()
    {
        // Verificar que tenemos al menos 2 puntos para dibujar líneas
        if (routePoints == null || routePoints.Length < 2) return;

        // Dibujar líneas amarillas conectando cada punto de la ruta
        Gizmos.color = Color.yellow;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            Gizmos.DrawLine(routePoints[i], routePoints[i + 1]);
        }

        // Dibujar esferas rojas en cada punto de la ruta
        Gizmos.color = Color.red;
        foreach (Vector3 point in routePoints)
        {
            Gizmos.DrawSphere(point, 1.0f);
        }

        // Dibujar una línea verde desde el objeto hacia su dirección de movimiento
        // (Solo visible en modo de juego para debug)
        if (Application.isPlaying && movementDirection != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, movementDirection * 6f);
        }
    }
}