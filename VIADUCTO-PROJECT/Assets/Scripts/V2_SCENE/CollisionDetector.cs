using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    // Reference to the original material
    public Material originalMaterial;
    // Reference to the renderer component
    public Renderer objectRenderer;
    // Red material instance (created at runtime)
    public Material redMaterial;

    // Called when the script instance is being loaded
    void Awake()
    {
        // Get the Renderer component attached to this GameObject
        objectRenderer = GetComponent<Renderer>();
        // Store the original material
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
            // Create a new red material based on the original shader
            redMaterial = new Material(originalMaterial.shader);
            redMaterial.color = Color.red;
        }
    }

    // Called when this collider/rigidbody has begun touching another collider/rigidbody
    void OnTriggerEnter(UnityEngine.Collider other)
    {
        // Check if the other object has the tag "player"
        if (other.gameObject.CompareTag("Player") && objectRenderer != null)
        {
            // Change the material to red
            objectRenderer.material = redMaterial;
            Debug.Log("Collision detected with player!");
            Debug.Log($"Trigger detected with object:  (tag: Player)");
        }
    }

    // Called once per frame for every collider/rigidbody that is touching another collider/rigidbody
    void OnTriggerStay(UnityEngine.Collider other)
    {
        // Ensure the material stays red while in contact
        if (other.gameObject.CompareTag("Player") && objectRenderer != null)
        {
            objectRenderer.material = redMaterial;
        }
    }

    // Called when this collider/rigidbody has stopped touching another collider/rigidbody
    void OnTriggerExit(UnityEngine.Collider other)
    {
        // If the object we stopped colliding with is tagged "player"
        if (other.gameObject.CompareTag("Player") && objectRenderer != null)
        {
            // Restore the original material
            objectRenderer.material = originalMaterial;
        }
    }
}
