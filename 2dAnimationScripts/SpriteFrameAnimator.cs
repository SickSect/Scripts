using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 12f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool randomStartFrame = false;

    private SpriteRenderer sr;
    private float timer;
    private int index;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (randomStartFrame && frames.Length > 0)
            index = Random.Range(0, frames.Length);
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float step = 1f / fps;

        while (timer >= step)
        {
            timer -= step;
            index++;

            if (index >= frames.Length)
            {
                if (loop) index = 0;
                else { index = frames.Length - 1; enabled = false; break; }
            }
        }

        sr.sprite = frames[index];
    }
}