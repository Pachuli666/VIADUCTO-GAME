using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 5f;
    public float acceleration = 1f;
    public float maxSpeed = 20f;
    public float sideSpeed = 10f;
    public float brakeMultiplier = 0.5f;

    [Header("Road Reference")]
    public Transform roadPlane;

    private float currentSpeed;
    private float leftLimit;
    private float rightLimit;

    void Start()
    {
        currentSpeed = baseSpeed;

        // Fijar rotación inicial
        transform.rotation = Quaternion.identity;

        // Calcular límites basados en el plano
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
        // Acelerar o frenar
        if (Input.GetKey(KeyCode.Space))
        {
            currentSpeed = baseSpeed * brakeMultiplier;
        }
        else
        {
            currentSpeed += acceleration * Time.deltaTime;
            if (currentSpeed > maxSpeed) currentSpeed = maxSpeed;
        }

        // Obtener posición actual
        Vector3 pos = transform.position;

        // Movimiento hacia adelante (eje X)
        pos.x += currentSpeed * Time.deltaTime;

        // Movimiento lateral (eje Z)
        float horizontal = Input.GetAxis("Horizontal");
        pos.z -= horizontal * sideSpeed * Time.deltaTime;
        pos.z = Mathf.Clamp(pos.z, leftLimit, rightLimit);

        // Aplicar nueva posición
        transform.position = pos;

        // Mantener rotación fija
        transform.rotation = Quaternion.identity;
    }
}