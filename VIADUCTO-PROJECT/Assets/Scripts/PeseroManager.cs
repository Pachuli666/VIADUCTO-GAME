using UnityEngine;

public class PeseroManager : MonoBehaviour
{
    [Header("Puntos de Ruta")]
    public Vector3[] routePoints; // Array de coordenadas que define el camino a seguir

    [Header("Velocidad")]
    public float startSpeed = 5f;          // Velocidad inicial del objeto
    public float speedIncrease = 0.5f;     // Cu�nto aumenta la velocidad por segundo
    public float brakingForce = 2f;        // Fuerza de frenado al acercarse al �ltimo punto
    public float stopDistance = 5f;        // Distancia desde el �ltimo punto donde inicia el frenado

    [Header("Rotaci�n")]
    public float rotationSpeed = 5f;       // Velocidad de rotaci�n suave hacia el objetivo
    public float turnRadius = 3f;          // Radio de giro del cami�n (simula que gira fuera de su eje)
    public float maxTurnAngle = 45f;       // �ngulo m�ximo de giro por segundo

    // Variables de control interno
    private int currentPoint = 0;    // �ndice del punto actual al que nos dirigimos
    private float currentSpeed;      // Velocidad actual (aumenta con el tiempo)
    private Vector3 movementDirection; // Direcci�n continua de movimiento
    private bool hasFinishedRoute = false; // Indica si ya termin� la ruta

    void Start()
    {
        // Inicializar la velocidad actual con la velocidad de inicio
        currentSpeed = startSpeed;

        // Si no se definieron puntos de ruta, crear una ruta b�sica hacia adelante
        if (routePoints.Length == 0)
        {
            routePoints = new Vector3[]
            {
                transform.position,                        // Posici�n actual
                transform.position + Vector3.forward * 10, // 10 unidades adelante
                transform.position + Vector3.forward * 20, // 20 unidades adelante
                transform.position + Vector3.forward * 30  // 30 unidades adelante
            };
        }

        // Establecer la direcci�n inicial hacia el primer punto objetivo
        if (routePoints.Length > 1)
        {
            movementDirection = (routePoints[1] - transform.position).normalized;
        }
    }

    void Update()
    {
        // Verificar si estamos cerca del �ltimo punto de la ruta
        bool shouldBrake = false;
        if (routePoints.Length > 0 && currentPoint >= routePoints.Length - 1)
        {
            Vector3 lastPoint = routePoints[routePoints.Length - 1];
            float distanceToLastPoint = Vector3.Distance(transform.position, lastPoint);

            // Activar frenos si estamos cerca del �ltimo punto
            shouldBrake = distanceToLastPoint <= stopDistance;

            // Marcar como terminada la ruta si llegamos muy cerca del �ltimo punto
            if (distanceToLastPoint <= 0.5f)
            {
                hasFinishedRoute = true;
            }
        }

        // Control de velocidad con fuerzas
        if (hasFinishedRoute)
        {
            // Si ya termin� la ruta, detener completamente
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
            // Aplicar fuerza de aceleraci�n (suma a la velocidad)
            currentSpeed += speedIncrease * Time.deltaTime;
        }

        // Solo mover si tenemos velocidad
        if (currentSpeed > 0f && !hasFinishedRoute)
        {
            transform.position += movementDirection * currentSpeed * Time.deltaTime;
        }

        // Verificar si a�n tenemos puntos de ruta por recorrer
        if (currentPoint < routePoints.Length)
        {
            // Obtener el punto de destino actual
            Vector3 target = routePoints[currentPoint];

            // Verificar si hemos llegado cerca del punto objetivo
            if (Vector3.Distance(transform.position, target) < 1f)
            {
                // Avanzar al siguiente punto de la ruta
                currentPoint++;

                // Si a�n hay m�s puntos, actualizar la direcci�n hacia el siguiente
                if (currentPoint < routePoints.Length)
                {
                    Vector3 nextTarget = routePoints[currentPoint];
                    movementDirection = (nextTarget - transform.position).normalized;
                }
                // Si ya no hay m�s puntos, mantener la �ltima direcci�n para seguir recto
                // (movementDirection se mantiene igual)
            }
            else
            {
                // Actualizar constantemente la direcci�n hacia el punto objetivo actual
                // Para simular el giro de cami�n, no cambiar la direcci�n tan bruscamente
                Vector3 targetDirection = (target - transform.position).normalized;

                // Calcular el �ngulo entre la direcci�n actual y la direcci�n objetivo
                float angleToTarget = Vector3.SignedAngle(movementDirection, targetDirection, Vector3.up);

                // Limitar el �ngulo de giro por frame para simular el radio de giro del cami�n
                float maxTurnThisFrame = maxTurnAngle * Time.deltaTime;
                float actualTurnAngle = Mathf.Clamp(angleToTarget, -maxTurnThisFrame, maxTurnThisFrame);

                // Aplicar la rotaci�n limitada a la direcci�n de movimiento
                movementDirection = Quaternion.AngleAxis(actualTurnAngle, Vector3.up) * movementDirection;

                // Simular que el cami�n gira fuera de su eje aplicando un desplazamiento lateral
                Vector3 lateralOffset = Vector3.Cross(movementDirection, Vector3.up) * (actualTurnAngle * turnRadius * Time.deltaTime);
                transform.position += lateralOffset;
            }
        }

        // Rotar el objeto para que siempre apunte hacia la direcci�n de movimiento
        // El frente del objeto es la cara lateral derecha (eje X positivo)
        if (movementDirection != Vector3.zero)
        {
            // Calcular la rotaci�n que debe tener el objeto basada en la direcci�n de movimiento
            // Usar Vector3.right como frente en lugar del Vector3.forward predeterminado
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

            // Aplicar rotaci�n adicional de 90 grados en Y para que el lado derecho sea el frente
            targetRotation *= Quaternion.Euler(0, -90, 0);

            // Aplicar la rotaci�n de manera suave para evitar cambios bruscos
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    // M�todo para visualizar la ruta en el editor de Unity (solo en Scene view)
    void OnDrawGizmos()
    {
        // Verificar que tenemos al menos 2 puntos para dibujar l�neas
        if (routePoints == null || routePoints.Length < 2) return;

        // Dibujar l�neas amarillas conectando cada punto de la ruta
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

        // Dibujar una l�nea verde desde el objeto hacia su direcci�n de movimiento
        // (Solo visible en modo de juego para debug)
        if (Application.isPlaying && movementDirection != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, movementDirection * 6f);
        }
    }
}