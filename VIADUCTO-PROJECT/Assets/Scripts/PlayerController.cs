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
    private float currentSpeed;
    private float leftLimit;
    private float rightLimit;
    void Start()
    {
        currentSpeed = 0f; // Empezar desde velocidad 0
        // Calcular l�mites basados en el plano
        if (roadPlane != null)
        {
            float planeWidth = roadPlane.localScale.z * 10f;
            leftLimit = roadPlane.position.z - planeWidth / 2f + 0.5f;
            rightLimit = roadPlane.position.z + planeWidth / 2f - 0.5f;
        }
        else
        {
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
        pos.z = Mathf.Clamp(pos.z, leftLimit, rightLimit);
        // Aplicar nueva posici�n
        transform.position = pos;
    }
}