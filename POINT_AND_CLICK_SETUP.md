# Point-and-Click система взаимодействия

## Обзор

Эта система позволяет реализовать механику point-and-click игр, где:
- Игрок не спавнится на сцене
- Управление идёт напрямую камерой и мышью
- Взаимодействие с объектами происходит по клику мыши
- При наведении на интерактивные объекты показывается подсказка с действием

**Важно:** Система использует новую систему ввода Unity (Input System) через обёртку `GameInput`.

---

## Быстрая настройка (пошаговая инструкция)

### Шаг 1: Настройка GameplayBootstrap

1. Найдите префаб `GameplayBootstrap` в `Resources/Core/GameplayBootstrap`
2. В инспекторе найдите поле **Point-and-Click режим**
3. Установите галочку `_pointAndClickMode = true`
4. (Опционально) Назначьте префаб камеры в `_pointAndClickCameraPrefab`, если хотите использовать специальный префаб

### Шаг 2: Настройка сцены

1. **Удалите игрока со сцены** (если он там есть)
2. **Настройте камеру:**
   - Добавьте компонент `PointAndClickCamera` на основную камеру
   - Или используйте обычную камеру с ручной настройкой позиции
3. **Добавьте MouseInteractor:**
   - Создайте пустой GameObject или добавьте на камеру
   - Добавьте компонент `MouseInteractor`
   - Настройте `Interactable Mask` для слоя интерактивных объектов
4. **Настройте UI подсказок:**
   - На Canvas создайте элемент для подсказки (RectTransform + TMP_Text)
   - Добавьте компонент `ClickInteractionHUD`
   - Назначьте `Hint Root` и `Hint Label` в инспекторе

### Шаг 3: Проверка Input System

Убедитесь, что в вашем Input Action Asset есть действие:
- Карта: `Player`
- Действие: `Interact` (тип Button, привязка к левой кнопке мыши)

### Шаг 4: Запуск

Запустите игру — камера должна управляться мышью и клавишами WASD, а при наведении на интерактивные объекты должны появляться подсказки.

---

## Новые/обновлённые компоненты

### 1. MouseInteractor (`Core.Interaction.MouseInteractor`)

**Расположение:** `/GameCore/Interaction/MouseInteractor.cs`

**Описание:** Обрабатывает взаимодействие через клик мышкой. Пускает рейкаст из камеры в точку курсора и вызывает `Interact()` на объектах с `IInteractable`.

**Обновление:** Теперь поддерживает новый метод `Bind(GameInput, DIContainer)` для работы с системой ввода через `GameInput`.

**Как использовать:**
1. Добавьте компонент на пустой GameObject на сцене или на камеру
2. Настройте параметры:
   - `Max Distance` — дальность рейкаста (по умолчанию 100)
   - `Interactable Mask` — слой интерактивных объектов
   - `Cursor Texture` — текстура курсора (опционально)
   - `Cursor Hotspot` — горячая точка курсора

**Публичные свойства:**
- `HoveredObject` — ReactiveProperty с объектом под курсором
- `HoveredPrompt` — ReactiveProperty с текстом подсказки

**Методы:**
- `Bind(GameInput gameInput, DIContainer root)` — **новый метод** для привязки через GameInput
- `Bind(InputAction clickAction, DIContainer root)` — устаревший метод для обратной совместимости

---

### 2. ClickInteractionHUD (`Core.UI.HUD.ClickInteractionHUD`)

**Расположение:** `/GameCore/Player/ClickInteractionHUD.cs`

**Описание:** Показывает подсказку о действии при наведении курсора на интерактивный объект. Подписывается на `MouseInteractor.HoveredPrompt`.

**Как использовать:**
1. Создайте Canvas с UI элементом для подсказки
2. Добавьте компонент на GameObject HUD
3. Настройте:
   - `Hint Root` — корневой RectTransform подсказки
   - `Hint Label` — TMP_Text для текста подсказки
   - `Offset` — смещение от курсора мыши

**Методы:**
- `SetInteractor(MouseInteractor interactor)` — подключение к MouseInteractor

---

### 3. PointAndClickCamera (`Core.Player.PointAndClickCamera`)

**Расположение:** `/GameCore/Player/PointAndClickCamera.cs`

**Описание:** Камера для режима point-and-click. Позволяет перемещаться по уровню клавишами WASD/стрелками или двигая мышью у краёв экрана.

**Как использовать:**
1. Добавьте компонент на основную камеру сцены
2. Настройте параметры:
   - `Move Speed` — скорость перемещения клавишами
   - `Allow Keyboard Move` — разрешить управление клавишами
   - `Allow Edge Scroll` — разрешить прокрутку у краёв экрана
   - `Edge Thickness` — толщина зоны края экрана
   - `Edge Scroll Speed` — скорость прокрутки у края
   - `Use Bounds` — использовать ограничения камеры
   - `Min/Max Bounds` — границы перемещения
   - `Allow Zoom` — разрешить зум колесом
   - `Zoom Speed` — скорость зума
   - `Min/Max Size` — пределы размера ортографической камеры

---

### 4. ClickInteractionInitStep (`Core.Player.ClickInteractionInitStep`)

**Расположение:** `/GameCore/Player/ClickInteractionInitStep.cs`

**Описание:** InitStep для инициализации системы point-and-click взаимодействия. Находит/создаёт MouseInteractor и связывает его с ClickInteractionHUD.

**Обновление:** Теперь использует `GameInput` вместо прямого `InputAction`.

**Как использовать:**
Автоматически добавляется через `GameplayBootstrap` при включении `_pointAndClickMode`.

---

### 5. GameplayBootstrap (обновлённый)

**Расположение:** `/GameCore/Core/Boot/GameplayBootstrap.cs`

**Новые поля:**
- `_pointAndClickMode` — переключатель режима (false = классический с игроком, true = point-and-click)
- `_pointAndClickCameraPrefab` — опциональный префаб камеры для point-and-click режима

**Логика:**
При `_pointAndClickMode = true`:
- Игрок НЕ спавнится
- Добавляется `ClickInteractionInitStep` для инициализации мышиного взаимодействия
- Опционально спавнится камера из префаба

При `_pointAndClickMode = false` (по умолчанию):
- Спавнится игрок из `_playerPrefab`
- Добавляется `PlayerInitStep` для классического управления
- Добавляется `CameraInitStep` для привязки Cinemachine

---

## Интерактивные объекты

Все существующие объекты с `IInteractable` продолжают работать без изменений:

```csharp
public class WorldItemPickup : MonoBehaviour, IInteractable
{
    public string Prompt => "Взять";
    
    public void Interact(InteractionContext context)
    {
        // Логика подбора предмета
    }
}
```

**Требования к объекту:**
- Коллайдер на слое, который ловит рейкаст (настроен в `Interactable Mask`)
- Компонент реализующий `IInteractable`

---

## Отличия от старой системы

| Старая система (PlayerInteractor) | Новая система (MouseInteractor) |
|-----------------------------------|---------------------------------|
| Игрок спавнится на сцене | Игрок не спавнится |
| Взаимодействие по клавише (E) | Взаимодействие по клику мыши |
| Рейкаст из центра экрана | Рейкаст из позиции курсора |
| Кроссхейр по центру | Курсор мыши |
| InteractionHUD с кроссхейром | ClickInteractionHUD у курсора |
| Использует InputAction напрямую | Использует GameInput |

---

## Совместимость

- ✅ Существующие `IInteractable` компоненты работают без изменений
- ✅ Можно использовать вместе со старой системой (переключаясь между режимами через `_pointAndClickMode`)
- ✅ `WorldItemPickup`, `LightSwitch`, `SceneTransitionTrigger` и другие интерактивные объекты работают как прежде
- ✅ Обратная совместимость со старым методом `Bind(InputAction, DIContainer)` сохранена (помечен как `[Obsolete]`)

---

## Troubleshooting

**Проблема:** Подсказки не показываются
- Проверьте, что `ClickInteractionHUD.SetInteractor()` был вызван (должен автоматически через `ClickInteractionInitStep`)
- Убедитесь, что `_hintRoot` и `_hintLabel` назначены в инспекторе
- Проверьте, что объект имеет `IInteractable` и возвращает не-null `Prompt`
- Убедитесь, что `_pointAndClickMode = true` в GameplayBootstrap

**Проблема:** Клик не взаимодействует с объектом
- Проверьте, что на объекте есть коллайдер
- Убедитесь, что слой объекта входит в `Interactable Mask`
- Проверьте, что в Input Action Asset есть действие `Player/Interact`
- Проверьте логи консоли на предмет предупреждений от `MouseInteractor`

**Проблема:** Камера не двигается
- Убедитесь, что `PointAndClickCamera` добавлен на камеру
- Проверьте, что `AllowKeyboardMove` или `AllowEdgeScroll` включены
- Для зума убедитесь, что камера в режиме `Orthographic`

**Проблема:** Ошибка "Player.Interact не найден в GameInput"
- Проверьте ваш Input Action Asset
- Убедитесь, что в карте `Player` есть действие `Interact`
- Пересохраните Input Action Asset и перекомпилируйте скрипты

---

## Примечания

1. **Сохранения:** В point-and-click режиме система сохранений продолжает работать, но позиция игрока не сохраняется (так как игрока нет)

2. **Переходы между сценами:** `SceneTransitionTrigger` работает как прежде — при клике на дверь происходит переход на другую сцену

3. **Инвентарь и пауза:** Работают стандартно через `UIScreenManager`

4. **Пререндеренные фоны:** Используйте `PreRenderedBackgroundManager` вместе с этой системой для переключения фонов в зависимости от позиции камеры
