using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class SimpleDOTweenCarousel : MonoBehaviour
{
    [Header("Referencias de UI (Arrastra los objetos aquí)")]
    // Arrastra aquí tus 3 imágenes en orden: [0]Izquierda, [1]Centro, [2]Derecha
    public RectTransform[] slots = new RectTransform[3];

    [Header("Datos")]
    public List<Sprite> listaDeSprites; // Todas tus imágenes para mostrar

    [Header("Configuración")]
    public float tiempoTransicion = 0.4f;
    private float distanciaMovimiento; // Se calculará sola
    private int indiceSpriteActual = 0; // Qué sprite de la lista estamos viendo
    private bool isAnimating = false;

    [SerializeField] private float inputtest;

    void Start()
    {
        // Calculamos cuánto hay que moverse basándonos en la distancia entre el centro y la derecha
        // Asumimos que están posicionados simétricamente al inicio.
        distanciaMovimiento = slots[2].anchoredPosition.x;

        ActualizarSpritesEnSlots();
    }

    void Update()
    {
        if (isAnimating) return;

        // Usamos GetAxisRaw para que detecte una sola pulsación rápida
        inputtest = Input.GetAxisRaw("Horizontal");

        if (inputtest > 0.5f) MoverSiguiente(); // Flecha Derecha o 'D'
        else if (inputtest < -0.5f) MoverAnterior();  // Flecha Izquierda o 'A'
    }

    // Lógica: Todo se mueve a la izquierda. El de la derecha entra al centro.
    void MoverSiguiente()
    {
        isAnimating = true;
        indiceSpriteActual++; // Avanzamos en la lista de datos

        Sequence seq = DOTween.Sequence();

        // 1. Animar: El Centro se va a la Izquierda
        seq.Join(slots[1].DOAnchorPosX(-distanciaMovimiento, tiempoTransicion));
        // 2. Animar: La Derecha se va al Centro
        seq.Join(slots[2].DOAnchorPosX(0, tiempoTransicion));

        seq.OnComplete(() =>
        {
            // --- EL TRUCO DE MAGIA ---
            // Al terminar la animación, reorganizamos el array de slots.

            // El slot[0] (que estaba a la izquierda) ya no sirve ahí. Lo mandamos instantáneamente
            // a la posición de la derecha para que esté listo para la próxima vez.
            slots[0].anchoredPosition = new Vector2(distanciaMovimiento, 0);

            // Rotamos las referencias en el array:
            RectTransform viejoIzquierda = slots[0];
            slots[0] = slots[1]; // El centro ahora es la izquierda
            slots[1] = slots[2]; // La derecha ahora es el centro
            slots[2] = viejoIzquierda; // La vieja izquierda ahora es la derecha

            // Cargamos las imágenes correctas en las nuevas posiciones
            ActualizarSpritesEnSlots();
            isAnimating = false;
        });
    }

    // Lógica inversa: Todo se mueve a la derecha. El de la izquierda entra al centro.
    void MoverAnterior()
    {
        isAnimating = true;
        indiceSpriteActual--;

        Sequence seq = DOTween.Sequence();
        // Centro se va a la Derecha
        seq.Join(slots[1].DOAnchorPosX(distanciaMovimiento, tiempoTransicion));
        // Izquierda se va al Centro
        seq.Join(slots[0].DOAnchorPosX(0, tiempoTransicion));

        seq.OnComplete(() =>
        {
            // El slot[2] (que estaba a la derecha) lo mandamos a la izquierda instantáneamente
            slots[2].anchoredPosition = new Vector2(-distanciaMovimiento, 0);

            // Rotamos referencias al revés
            RectTransform viejoDerecha = slots[2];
            slots[2] = slots[1];
            slots[1] = slots[0];
            slots[0] = viejoDerecha;

            ActualizarSpritesEnSlots();
            isAnimating = false;
        });
    }

    // Función auxiliar para cargar los sprites correctos basándose en el índice central
    void ActualizarSpritesEnSlots()
    {
        if (listaDeSprites.Count == 0) return;

        int total = listaDeSprites.Count;
        // Usamos módulo (%) para asegurar que el índice siempre sea válido (bucle infinito)
        int indiceCentro = (indiceSpriteActual % total + total) % total;
        int indiceIzq = ((indiceSpriteActual - 1) % total + total) % total;
        int indiceDer = ((indiceSpriteActual + 1) % total + total) % total;

        slots[0].GetComponent<Image>().sprite = listaDeSprites[indiceIzq];
        slots[1].GetComponent<Image>().sprite = listaDeSprites[indiceCentro];
        slots[2].GetComponent<Image>().sprite = listaDeSprites[indiceDer];
    }
}