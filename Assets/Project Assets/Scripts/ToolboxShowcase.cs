using UnityEngine;
using System.Collections.Generic;
using Toolbox.Attributes; // Para [ProgressBar], [ReorderableList], etc.
using Toolbox;            // Para SerializedScene, SerializedType, etc.

public class ToolboxShowcase : MonoBehaviour
{
    [Label("1. Atributos de Inspector", skinStyle: SkinStyle.Box, Alignment = TextAnchor.MiddleCenter)]

    [TagSelector]
    public string tagDePrueba;

    [ProgressBar("Carga de Energía", minValue: 0, maxValue: 100)]
    public float energia = 50.0f;

    [SceneName]
    public string siguienteEscena;

    // --- TIPOS SERIALIZABLES ---
    [SpaceArea(20)]
    [Label("2. Tipos Especiales (Toolbox)", skinStyle: SkinStyle.Box)]

    // Si 'SerializedScene' te da error aquí, es posible que 
    // necesites habilitarlo en el EditorSettings del asset.
    public SerializedScene escenaDelNivel;

    [TypeConstraint(typeof(AudioSource))]
    public SerializedType tipoDeComponente;

    // --- LISTAS ---
    [ReorderableList(ListStyle.Lined, "Elemento")]
    public List<string> listaOrganizada;

    [EditorButton(nameof(TestLog), "Probar Botón", activityType: ButtonActivityType.Everything)]
    public bool miBoton;

    private void TestLog()
    {
        Debug.Log("¡Funciona perfectamente!");
    }
}