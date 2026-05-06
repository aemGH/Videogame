using UnityEngine;

[ExecuteAlways]
public class FogManager : MonoBehaviour
{
    public static FogManager Instance { get; private set; }

    [Header("Paramètres de brouillard")]
    [Tooltip("La distance où le brouillard devient 100% opaque (Mur absolu / Zone de Spawn)")]
    public float visibilityRadius = 60f;
    
    [Tooltip("La distance où le brouillard commence à apparaître (Illusion d'ambiance)")]
    public float fogStartDistance = 10f;
    
    public Color fogColor = new Color(0.6f, 0.8f, 0.9f);

    private void Awake()
    {
        // Mise en place du Singleton
        if (Instance != null && Instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Appliquer les paramètres de brouillard en mode linéaire
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogEndDistance = visibilityRadius; // La couche absolue
        RenderSettings.fogStartDistance = fogStartDistance; // La couche progressive
        
        // Mettre à jour la caméra pour que le fond corresponde au brouillard
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = fogColor;
        }
    }

    /// <summary>
    /// Retourne le rayon de spawn des mobs juste en dehors du brouillard.
    /// </summary>
    public float GetSpawnRadius()
    {
        return visibilityRadius + 2f;
    }
}
