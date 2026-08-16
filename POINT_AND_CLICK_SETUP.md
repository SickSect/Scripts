# Настройка Point-and-Click режима

## Обзор изменений

Все скрипты адаптированы под новую систему ввода Unity (Input System) с использованием событий `performed`/`canceled`. Старая mob реализация сохранена и работает в классическом режиме.

## Изменённые файлы

### 1. `PointAndClickCamera.cs`
- Добавлен метод `BindInput(InputAction)` для работы с новой системой ввода
- Использует события `performed`/`canceled` вместо прямого опроса клавиатуры
- Добавлены методы `OnEnable`/`OnDisable`/`OnDestroy` для безопасного управления подписками
- Движение камеры теперь работает через `_moveInput` из Input Action

### 2. `ClickInteractionInitStep.cs`
- Добавлена настройка `PointAndClickCamera` автоматически при инициализации
- Находит основную камеру и добавляет компонент `PointAndClickCamera` если его нет
- Биндит действие `Move` к камере через `BindInput()`

### 3. `GameplayBootstrap.cs`
- Убрано поле `_pointAndClickCameraPrefab` (теперь камера настраивается автоматически)
- Упрощена логика инициализации point-and-click режима

## Пошаговая инструкция настройки

### Шаг 1: Включить Point-and-Click режим

1. Найдите префаб `GameplayBootstrap` на сцене или в Resources
2. В инспекторе найдите секцию **"Point-and-Click режим"**
3. Установите галочку **Point And Click Mode** = `true`

### Шаг 2: Подготовка сцены

1. **Удалите игрока** со сцены (если есть префаб игрока)
2. Убедитесь, что на сцене есть:
   - **Основная камера** (Camera.main)
   - **MouseInteractor** (создаётся автоматически, если нет)
   - **ClickInteractionHUD** (для отображения подсказок)

### Шаг 3: Настройка компонентов

#### Для камеры:
1. Выберите основную камеру на сцене
2. Добавьте компонент `PointAndClickCamera` (добавится автоматически при запуске)
3. Настройте параметры:
   - **Move Speed** — скорость перемещения клавишами
   - **Edge Scroll Speed** — скорость скроллинга у краёв экрана
   - **Zoom Speed** — скорость зума колесом мыши
   - **Use Bounds** — ограничить движение камеры (опционально)

#### Для MouseInteractor:
1. Создайте пустой GameObject "MouseInteractor" или найдите существующий
2. Добавьте компонент `MouseInteractor`
3. Настройте:
   - **Max Distance** — максимальная дистанция взаимодействия
   - **Interactable Mask** — слой интерактивных объектов
   - **Cursor Texture** — текстура курсора (опционально)

#### Для ClickInteractionHUD:
1. Создайте/найдите UI элемент для подсказок
2. Добавьте компонент `ClickInteractionHUD`
3. Настройте отображение подсказок

### Шаг 4: Настройка ввода

Убедитесь, что в вашем Input Action Asset (`MainInputSystem.inputactions`) есть:
- **Map: Player**
  - **Action: Move** (тип: Value, Control Type: Vector2) — для движения камеры
  - **Action: Interact** (тип: Button) — для клика мышкой

Пример привязок:
- **Move**: WASD или стрелки клавиатуры
- **Interact**: Левая кнопка мыши

### Шаг 5: Интерактивные объекты

Для объектов, с которыми можно взаимодействовать:
1. Добавьте на объект любой компонент, реализующий интерфейс `IInteractable`
2. Укажите свойство `Prompt` — текст подсказки при наведении
3. Реализуйте метод `Interact(InteractionContext ctx)`

Примеры существующих реализаций:
- `WorldItemPickup` — подбор предметов
- Двери — открытие/закрытие
- Другие интерактивные объекты

## Как это работает

1. При запуске сцены `GameplayBootstrap` проверяет `_pointAndClickMode`
2. Если `true`:
   - Игрок **НЕ спавнится**
   - Вызывается `ClickInteractionInitStep`
   - Создаётся/находится `MouseInteractor`
   - На основную камеру добавляется `PointAndClickCamera`
   - Биндится ввод через `GameInput`
3. Игрок управляет камерой через:
   - **WASD/стрелки** — перемещение камеры
   - **Движение мыши у краёв экрана** — скроллинг
   - **Колесо мыши** — зум (для orthographic камеры)
   - **Левый клик** — взаимодействие с объектами

## Переключение между режимами

### Классический режим (игрок):
- `_pointAndClickMode = false`
- Спавнится префаб игрока
- Работает `PlayerMovement`, `PlayerLook`, `PlayerInteractor`
- Камера управляется через Cinemachine

### Point-and-Click режим:
- `_pointAndClickMode = true`
- Игрок не спавнится
- Камера управляется напрямую через `PointAndClickCamera`
- Взаимодействие через `MouseInteractor`

## Отладка

В консоли Unity вы увидите логи:
```
[GameplayBootstrap] Инициализация point-and-click режима
[ClickInteractionInitStep] MouseInteractor привязан к клику через GameInput
[ClickInteractionInitStep] ClickInteractionHUD подключён к MouseInteractor
[ClickInteractionInitStep] Добавлен компонент PointAndClickCamera на основную камеру
[ClickInteractionInitStep] PointAndClickCamera привязана к Move через GameInput
```

## Возврат к классическому режиму

Просто установите `_pointAndClickMode = false` в `GameplayBootstrap`. Вся старая логика игрока останется без изменений.
