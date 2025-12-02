using UnityEngine;

public class TrafficLightVisual : MonoBehaviour
{
    public Renderer targetRenderer;      // El renderer del semáforo completo
    public Color redColor = Color.red;
    public Color greenColor = Color.green;

    private Material _matInstance;

    void Awake()
    {
        // Si no lo asignas a mano, toma el Renderer del mismo objeto
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
        {
            // Crea instancia del material para no modificar el material global
            _matInstance = targetRenderer.material;
        }
    }

    public void SetGreen(bool isGreen)
    {
        if (_matInstance == null) return;

        _matInstance.color = isGreen ? greenColor : redColor;
    }
}