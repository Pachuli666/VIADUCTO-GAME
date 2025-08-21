using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float playerSpeed; // Velocidad del jugador
   
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal") * playerSpeed;
        //float vertical = Input.GetAxis("Vertical") * playerSpeed;

        //Se crea un vector donde se designa el eje y la fuerza del movimiento
        Vector3 movement = new Vector3(horizontal, 0, 0) * playerSpeed * Time.deltaTime;


        //Con translate se aplica el vector en el objeto
        transform.Translate(movement);

    }
}
