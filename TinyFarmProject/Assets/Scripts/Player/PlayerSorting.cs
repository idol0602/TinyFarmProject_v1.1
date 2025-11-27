using UnityEngine;

public class PlayerSorting : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public float offset = 0f;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        spriteRenderer.sortingOrder = (int)(-(transform.position.y * 100) + offset);
    }
}
