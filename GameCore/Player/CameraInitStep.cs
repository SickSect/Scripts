using Core.Init;
using UnityEngine;

namespace Core.Player
{
    /// <summary>
    /// Подключает камеру к заспавненному игроку. Поддерживает два режима:
    ///
    ///  1) FPS — <see cref="FirstPersonCamera"/> живёт ВНУТРИ префаба игрока.
    ///     Если найдена и включена, третье лицо не ищется вовсе.
    ///  2) TPS — <see cref="ThirdPersonCamera"/> живёт на сцене (объект TPS_Rig),
    ///     игрок пересоздаётся, поэтому цель проставляется в рантайме.
    ///
    /// В обоих случаях камера-источник явно прокидывается в LookTarget —
    /// Camera.main больше не участвует в выборе.
    ///
    /// Порядок: после PlayerInitStep (Order=10). Order=20.
    /// </summary>
    public class CameraInitStep : IInitStep
    {
        public int Order => 20;

        public void Execute(InitContext ctx)
        {
            if (!ctx.Scene.TryResolve<PlayerMovement>(out var player))
            {
                Debug.LogError("[CameraInitStep] Игрок не найден в scene-контейнере.");
                return;
            }

            var look = player.GetComponent<PlayerLook>();
            var lookTarget = player.GetComponentInChildren<LookTarget>(true);

            // --- Режим 1: первое лицо ---------------------------------------
            var fps = player.GetComponentInChildren<FirstPersonCamera>(true);
            if (fps != null && fps.gameObject.activeInHierarchy)
            {
                fps.Bind(player.transform, look);
                AssignSource(lookTarget, fps.Camera, "FPS");
                Debug.Log("[CameraInitStep] режим: первое лицо.");
                return;
            }

            // --- Режим 2: третье лицо ---------------------------------------
            var cam = Object.FindAnyObjectByType<ThirdPersonCamera>();
            if (cam == null)
            {
                Debug.LogError("[CameraInitStep] Ни FirstPersonCamera в префабе, ни ThirdPersonCamera на сцене не найдены.");
                return;
            }

            cam.SetTarget(player.transform);

            // Juice сидит на дочерней Camera — прокинем ему ссылки явно.
            var juice = cam.GetComponentInChildren<CameraJuice>();
            if (juice != null)
            {
                var renderCam = juice.GetComponent<Camera>();
                if (renderCam != null) juice.SetCamera(renderCam);
                juice.SetPlayer(player, look);
            }

            AssignSource(lookTarget, cam.GetComponentInChildren<Camera>(), "TPS");
            Debug.Log("[CameraInitStep] режим: третье лицо.");
        }

        private static void AssignSource(LookTarget lookTarget, Camera camera, string mode)
        {
            if (lookTarget == null)
            {
                Debug.LogWarning($"[CameraInitStep] {mode}: LookTarget на игроке не найден.");
                return;
            }

            if (camera == null)
            {
                Debug.LogWarning($"[CameraInitStep] {mode}: рендер-камера не найдена, LookTarget останется на Camera.main.");
                return;
            }

            lookTarget.SetCamera(camera);
        }
    }
}
