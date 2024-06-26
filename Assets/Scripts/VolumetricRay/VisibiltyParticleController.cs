using UnityEngine;

public class VisibiltyParticleController : MonoBehaviour
{
public ParticleSystem particleSystem;
    public Transform playerTransform;
    public float maxHeight = 0.0f; // La hauteur maximale à laquelle le joueur peut voir les particules

    void Update()
    {
        if (particleSystem == null || playerTransform == null) return;

        var emission = particleSystem.emission;
        if (playerTransform.position.y > maxHeight)
        {
            emission.enabled = false; // Désactiver l'émission des particules
        }
        else
        {
            emission.enabled = true; // Activer l'émission des particules
        }
    }
}
