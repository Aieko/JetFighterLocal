using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] private Canvas gameFieldCanvas;

    public float CanvasWidth { get; private set; }
    public float CanvasHeight { get; private set; }

    private void Awake()
    {
        MakeInstance();

        var rectTransform = gameFieldCanvas.gameObject.GetComponentInChildren<Image>().GetComponent<RectTransform>();
        CanvasWidth = rectTransform.rect.width - 5f;
        CanvasHeight = rectTransform.rect.height - 5f;
    }

    private void MakeInstance()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != null)
        {
            Destroy(gameObject);
        }
    }

    public bool ConstrainToMap(Vector2 position, out Vector2 resultPosition)
    {
        if (position.x < -CanvasWidth)
        {
            resultPosition = new Vector2(CanvasWidth, position.y);
            return true;

        }

        if (position.x > CanvasWidth)
        {
            resultPosition = new Vector2(-CanvasWidth, position.y);
            return true;
        }

        if (position.y > CanvasHeight)
        {
            resultPosition = new Vector2(position.x, -CanvasHeight);
            return true;
        }

        if (position.y < -CanvasHeight)
        {
            resultPosition = new Vector2(position.x, CanvasHeight);
            return true;
        }

        resultPosition = position;
        return false;
    }
}
