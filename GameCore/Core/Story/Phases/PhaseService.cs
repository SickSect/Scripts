using System.Collections.Generic;
using System.Linq;
using Core.Common;
using Core.DI;
using Core.Flags;
using Core.Inventory;
using R3;
using UnityEngine;

namespace Core.Story.Phases
{
    /// <summary>
    /// Сердце цикла фаз. Держит текущий шаг + активный вариант, умеет:
    ///  - Activate(variant): спавнит наполнение варианта в якоря сцены, выставляет метку;
    ///  - Advance(): завершить текущую фазу, перейти к следующему шагу — выбрать вариант
    ///    с выполненным условием и активировать его.
    ///
    /// Фазу заканчивает игрок (через AdvancePhaseAction на кровати/событии).
    /// Смены сцены при переходе нет — фаза живёт в текущей сцене.
    /// </summary>
    public class PhaseService
    {
        private readonly PhaseGraph _graph;
        private readonly DIContainer _root;

        public PhaseData Data { get; private set; } = new();

        /// <summary>Текущий активный вариант (для квестов/визуала). Может быть null до активации.</summary>
        public ReactiveProperty<PhaseVariant> Current { get; } = new(null);

        public PhaseService(PhaseGraph graph, DIContainer root)
        {
            _graph = graph;
            _root = root;
        }

        // ---------- загрузка/сохранение ----------

        public void LoadFrom(PhaseData data) => Data = data.Clone();
        public void SaveInto(PhaseData data)
        {
            data.currentStep = Data.currentStep;
            data.currentVariantId = Data.currentVariantId;
            data.dayNumber = Data.dayNumber;
        }

        // ---------- активация текущей фазы на сцене ----------

        /// <summary>
        /// Активировать вариант, соответствующий текущему состоянию (шаг + сохранённый id).
        /// Вызывается при входе на сцену (InitStep). Спавнит наполнение в якоря.
        /// </summary>
        public void ActivateCurrentOnScene()
        {
            var variant = ResolveCurrentVariant();
            if (variant == null)
            {
                CoreLog.Debug($"[Phase] нет варианта для шага {Data.currentStep}");
                return;
            }
            Activate(variant);
        }

        private PhaseVariant ResolveCurrentVariant()
        {
            // Если id сохранён — берём именно его (чтобы не перевыбрать после загрузки).
            if (!string.IsNullOrEmpty(Data.currentVariantId))
            {
                var byId = _graph.FindById(Data.currentStep, Data.currentVariantId);
                if (byId != null) return byId;
            }
            // Иначе выбираем по условию (первый подходящий).
            return SelectVariant(Data.currentStep);
        }

        private void Activate(PhaseVariant variant)
        {
            Data.currentStep = variant.step;
            Data.currentVariantId = variant.variantId;

            // Спавн наполнения в якоря сцены.
            var anchors = CollectAnchors();
            var ctx = new PhaseSpawnContext(_root, anchors);
            foreach (var spawn in variant.spawns)
            {
                if (spawn == null) continue;
                if (spawn.IsConsumed(ctx))
                {
                    CoreLog.Debug($"[Phase] спавн '{spawn.name}' пропущен (уже получено)");
                    continue;
                }
                spawn.Spawn(ctx);
            }

            // Метка активной фазы.
            if (variant.onActivateTrigger != null && _root.TryResolve<FlagService>(out var flags))
                flags.Set(variant.onActivateTrigger);

            Current.Value = variant;
            CoreLog.Debug($"[Phase] активирована фаза шаг {variant.step} вариант '{variant.variantId}'");
        }

        // ---------- переход к следующей фазе ----------

        /// <summary>Завершить текущую фазу и перейти к следующему шагу (выбор варианта по условию).</summary>
        public void Advance()
        {
            int nextStep = Data.currentStep + 1;

            // Дни: если шагов больше нет — новый день, шаг 1.
            if (nextStep > _graph.MaxStep)
            {
                Data.dayNumber++;
                nextStep = 1;
                CoreLog.Debug($"[Phase] новый день {Data.dayNumber}");
            }

            var next = SelectVariant(nextStep);
            if (next == null)
            {
                CoreLog.Debug($"[Phase] нет подходящего варианта для шага {nextStep}");
                return;
            }

            Data.currentStep = nextStep;
            Data.currentVariantId = next.variantId;
            Activate(next);
        }

        /// <summary>Выбрать вариант шага: первый, чьё условие выполнено (пустое условие = fallback).</summary>
        private PhaseVariant SelectVariant(int step)
        {
            _root.TryResolve<FlagService>(out var flags);
            _root.TryResolve<InventoryService>(out var inventory);
            var condCtx = new ConditionContext(flags, inventory);

            foreach (var variant in _graph.VariantsOfStep(step))
            {
                if (variant.activationCondition == null) return variant;      // fallback
                if (variant.activationCondition.Evaluate(condCtx)) return variant;
            }
            return null;
        }

        private Dictionary<string, Transform> CollectAnchors()
        {
            var map = new Dictionary<string, Transform>();
            foreach (var a in Object.FindObjectsByType<SpawnAnchor>(FindObjectsInactive.Include))
            {
                if (a == null || string.IsNullOrEmpty(a.anchorId)) continue;
                if (!map.ContainsKey(a.anchorId)) map[a.anchorId] = a.transform;
            }
            return map;
        }
    }
}
