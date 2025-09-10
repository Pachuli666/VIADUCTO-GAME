using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float acceleration = 8f;         // Aceleraci�n autom�tica
    public float maxSpeed = 20f;
    public float sideSpeed = 10f;

    [Header("Braking Settings")]
    public float brakeForce = 15f;           // Fuerza de frenado
    public float minSpeed = 0.1f;            // Velocidad m�nima antes de parar completamente

    [Header("Road Reference")]
    public Transform roadPlane;
    public float sideMargin = 0.5f;          // Margen de seguridad en los lados

    private float currentSpeed;
    private float leftLimit;
    private float rightLimit;

    void Start()
    {
        currentSpeed = 0f; // Empezar desde velocidad 0
        CalculateLimits();
    }

    void CalculateLimits()
    {
        if (roadPlane != null)
        {
            // Obtener el Renderer o Collider para calcular los bounds reales
            Renderer planeRenderer = roadPlane.GetComponent<Renderer>();

            if (planeRenderer != null)
            {
                // Usar los bounds del renderer para obtener el tama�o real
                Bounds planeBounds = planeRenderer.bounds;
                leftLimit = planeBounds.min.z + sideMargin;
                rightLimit = planeBounds.max.z - sideMargin;
            }
            else
            {
                // M�todo alternativo usando la escala del transform
                // Para un plano por defecto de Unity (10x10 unidades)
                float realWidth = roadPlane.localScale.z * 10f;
                leftLimit = roadPlane.position.z - (realWidth / 2f) + sideMargin;
                rightLimit = roadPlane.position.z + (realWidth / 2f) - sideMargin;
            }


        }
        else
        {
            // Valores por defecto si no hay plano asignado
            leftLimit = -3f;
            rightLimit = 3f;

        }
    }

    void Update()
    {
        HandleSpeedControl();
        HandleMovement();
    }

    void HandleSpeedControl()
    {
        bool isBraking = Input.GetKey(KeyCode.Space);

        if (isBraking)
        {
            // Aplicar frenado progresivo
            currentSpeed -= brakeForce * Time.deltaTime;
            // No permitir velocidad negativa (marcha atr�s)
            if (currentSpeed < 0f)
                currentSpeed = 0f;
            // Parar completamente si la velocidad es muy baja
            if (currentSpeed < minSpeed)
                currentSpeed = 0f;
        }
        else
        {
            // Aceleraci�n autom�tica y constante hasta velocidad m�xima
            currentSpeed += acceleration * Time.deltaTime;
            if (currentSpeed > maxSpeed)
                currentSpeed = maxSpeed;
        }
    }

    void HandleMovement()
    {
        // Obtener posici�n actual
        Vector3 pos = transform.position;

        // Movimiento hacia adelante (eje X) - solo si hay velocidad
        if (currentSpeed > 0f)
        {
            pos.x += currentSpeed * Time.deltaTime;
        }

        // Movimiento lateral (eje Z) - reducir velocidad lateral si est� frenando
        float horizontal = Input.GetAxis("Horizontal");
        float effectiveSideSpeed = Input.GetKey(KeyCode.Space) ? sideSpeed * 0.6f : sideSpeed;
        pos.z -= horizontal * effectiveSideSpeed * Time.deltaTime;

        // Limitar movimiento dentro del �rea del plano
        pos.z = Mathf.Clamp(pos.z, leftLimit, rightLimit);

        // Aplicar nueva posici�n
        transform.position = pos;
    }

    // M�todo para recalcular l�mites en tiempo de ejecuci�n si es necesario
    public void RecalculateLimits()
    {
        CalculateLimits();
    }

    // M�todo para visualizar los l�mites en el editor
    void OnDrawGizmos()
    {
        if (roadPlane != null)
        {
            Gizmos.color = Color.red;
            Vector3 leftPos = new Vector3(transform.position.x, transform.position.y + 1f, leftLimit);
            Vector3 rightPos = new Vector3(transform.position.x, transform.position.y + 1f, rightLimit);

            // Dibujar l�neas que muestren los l�mites
            Gizmos.DrawLine(leftPos + Vector3.back * 5f, leftPos + Vector3.forward * 5f);
            Gizmos.DrawLine(rightPos + Vector3.back * 5f, rightPos + Vector3.forward * 5f);
        }
    }
}