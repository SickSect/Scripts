using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private bool lockY = true;

    private Transform cam;

    void LateUpdate()
    {
        // Ленивый поиск: в Start() камера игрока может быть ещё не заспавнена
        // (PlayerInitStep, Order 10) — Camera.main возвращал null и падал NullReference.
        if (cam == null)
        {
            var main = Camera.main;
            if (main == null) return;
            cam = main.transform;
        }

        Vector3 dir = cam.position - transform.position;
        if (lockY) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(-dir);
    }
}
