# Point-and-Click система взаимодействия

## Обзор

Эта система позволяет реализовать механику point-and-click игр, где:
- Игрок не спавнится на сцене
- Управление идёт напрямую камерой и мышью
- Взаимодействие с объектами происходит по клику мыши
- При наведении на интерактивные объекты показывается подсказка с действием

## Новые компоненты

### 1. MouseInteractor (`Core.Interaction.MouseInteractor`)

**Расположение:** `/GameCore/Interaction/MouseInteractor.cs`

**Описание:** Обрабатывает взаимодействие через клик мышкой. Пускает рейкаст из камеры в точку курсора и вызывает `Interact()` на объектах с `IInteractable`.

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
- `Bind(InputAction clickAction, DIContainer root)` — привязка к действию клика

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

**Как использовать:**
```csharp
var clickAction = inputAsset.FindAction("Player/Click"); // или ваше действие
var initStep = new ClickInteractionInitStep(clickAction);
initContext.Register(initStep);
```

---

## Настройка сцены

### Шаг 1: Удаление игрока
- Удалите префаб игрока со сцены (или не спавньте его)
- Оставьте только камеру

### Шаг 2: Настройка камеры
1. Добавьте компонент `PointAndClickCamera` на камеру
2. Настройте параметры перемещения и зума
3. Для ортографической камеры включите `Orthographic` в компоненте Camera

### Шаг 3: Настройка взаимодействия
1. Добавьте компонент `MouseInteractor` на камеру или отдельный GameObject
2. Добавьте компонент `ClickInteractionHUD` на Canvas/UI
3. Настройте UI элемент подсказки (RectTransform + TMP_Text)

### Шаг 4: Инициализация
В вашем InitStep (например, в SceneInitStep) добавьте:
```csharp
var clickAction = inputAsset.FindAction("Player/Click");
ctx.Register(new ClickInteractionInitStep(clickAction));
```

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

---

## Совместимость

- Существующие `IInteractable` компоненты работают без изменений
- Можно использовать вместе со старой системой (переключаясь между режимами)
- `WorldItemPickup`, `LightSwitch` и другие интерактивные объекты работают как прежде

---

## Пример настройки Input Action

Создайте Input Action Asset с действием:
- Name: `Click`
- Type: `Button`
- Binding: `Mouse Left Button`

Или используйте стандартное действие из Input System package.

---

## Troubleshooting

**Проблема:** Подсказки не показываются
- Проверьте, что `ClickInteractionHUD.SetInteractor()` был вызван
- Убедитесь, что `_hintRoot` и `_hintLabel` назначены в инспекторе
- Проверьте, что объект имеет `IInteractable` и возвращает не-null `Prompt`

**Проблема:** Клик не взаимодействует с объектом
- Проверьте, что на объекте есть коллайдер
- Убедитесь, что слой объекта входит в `Interactable Mask`
- Проверьте, что `MouseInteractor.Bind()` был вызван с правильным InputAction

**Проблема:** Камера не двигается
- Убедитесь, что `PointAndClickCamera` добавлен на камеру
- Проверьте, что `AllowKeyboardMove` или `AllowEdgeScroll` включены
- Для зума убедитесь, что камера в режиме `Orthographic`
