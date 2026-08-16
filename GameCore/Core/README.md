# Core — ядро игры (чистое, без механик)

Это вычлененное из твоего проекта ядро: запуск, инициализация, загрузка сцен,
сохранения и сущность состояния игры. Никаких конкретных механик (инвентарь,
время, диалоги, магазин, NPC и т.д.) здесь нет — только каркас, в который они втыкаются.

## Что происходит после нажатия Play

1. `GameBootstrap.AutoStart()` вызывается автоматически (`[RuntimeInitializeOnLoadMethod]`)
   до загрузки первой сцены. Тебе не нужно вешать его на объект.
2. Поднимается root-`DIContainer`, регистрируются ядровые системы
   (`SceneLoader`, `JsonSaveProvider`, `GameStateService`) и ядровые сигналы.
3. Грузится сцена `MainMenuScene`, на ней ищется `MainMenuBootstrap`.
4. Меню возвращает поток сигналов (`NEW_GAME` / `LOAD_GAME` / `EXIT_GAME`).
5. По `NEW_GAME` создаётся дефолтный `GameStateData`, по `LOAD_GAME` — грузится из слота;
   затем грузится первая игровая сцена и `GameplayBootstrap`.
6. `GameplayBootstrap` прогоняет `Initializer` (список шагов) и возвращает поток
   сигналов перехода наружу (в меню / на другую сцену / выход).

Смена сцен идёт только через один канал — `SceneTransitionParameters`
(в нём едет снапшот состояния, имя следующей сцены и spawnId).

## Структура

- `Boot/` — точка входа и bootstrap-ы сцен.
  - `GameBootstrap` — автозапуск, root-контейнер, навигация между сценами.
  - `SceneBootstrapBase` — общий контракт: `Initialize(ctx) -> Observable<переход>`.
  - `MainMenuBootstrap`, `GameplayBootstrap`.
- `DI/` — `DIContainer` (root/scene) и `Coroutines`.
- `SceneLoader/` — `SceneLoader` + ScriptableObject-ы `SceneGraph`, `SceneNode`,
  `SpawnNode` и компонент `Spawn`.
- `Save/` — `JsonSaveProvider` (сохранение снапшота по слотам в JSON).
- `State/` — **сущность состояния игры**:
  - `GameStateData` — сам снапшот (сериализуется в файл).
  - `SceneTransitionParameters` — что передаётся между сценами.
  - `GameStateService` — держит рантайм-снапшот, собирает его и сохраняет.
  - `IStateContributor` — как механика попадает в снапшот.
- `Init/` — **механизм инициализации**:
  - `IInitStep` — один шаг инициализации сцены.
  - `Initializer` — прогоняет шаги по `Order`.
  - `InitContext` — root/scene контейнеры + параметры перехода.
  - `BindSpawnsInitStep` — ядровой шаг (пример).
  - `_ExampleMechanicInitStep.cs.txt` — шаблон для новой механики.
- `Signals/` — `CoreSignals` (теги ядровых сигналов).

## Как добавить механику (инвентарь/время/что угодно)

1. Добавь её данные полем в `GameStateData` и скопируй их в `Clone()`.
2. Сделай её сервис и зарегистрируй в контейнере (root — если живёт между сценами,
   scene — если пересоздаётся).
3. Напиши свой `IInitStep`, где всё связывается, и добавь его строкой в
   `GameplayBootstrap.BuildInitializer()`.
4. Если механика сохраняется — реализуй `IStateContributor` и зарегистрируй его в
   `GameStateService` (обычно внутри своего InitStep).

Тело bootstrap-ов при этом не меняется — только список шагов.

## Что нужно настроить в Unity

- Установить зависимости из исходного проекта: **R3** (реактивные `Subject`/`Observable`)
  и DI. Здесь используется собственный минимальный `DIContainer` вместо BaCon —
  если хочешь оставить BaCon, замени `Core.DI.DIContainer` на него (API совместим по смыслу).
- Создать ассеты:
  - `Resources/Core/SceneGraph` — `SceneGraph` со списком сцен (`scenes[0]` = меню,
    `scenes[1]` = первая игровая сцена).
  - `Resources/Core/GameplayBootstrap` — префаб с компонентом `GameplayBootstrap`.
- Сцены `MainMenuScene` и игровые сцены добавить в **Build Settings**.
- На `MainMenuScene` положить объект с `MainMenuBootstrap`.
- На игровых сценах создать объект `Spawns` с дочерними `Spawn` (у каждого — свой `SpawnNode`).
- Кнопки меню должны пушить в root-сигналы (`CoreSignals.NEW_GAME` и т.д.).

## Отличия от исходника (что почистил)

- Убраны все механики: инвентарь, записки, время, диалоги, стори-триггеры, NPC,
  магазин, воркстейшн, эффекты, UI-контроллеры.
- Длинный ручной список `XxxInitialization()` в геймплейном bootstrap заменён на
  расширяемый `Initializer` + `IInitStep`.
- `CoreProcessService` → `GameStateService` с чистым разделением origin/runtime и
  расширением через `IStateContributor`.
- `GameStateData` сведён к ядровым полям + точки расширения под механики.
