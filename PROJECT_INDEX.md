# BalanStick — оглавление проекта

> Живая навигационная карта проекта. Обновляйте этот файл при изменении игрового цикла, состава сцен, модулей, конфигураций или существенных технических рисков.

Последняя сверка с проектом: 2026-08-26  
Версия Unity: 2022.3.62f3  
Основная платформа: мобильные устройства, landscape, управление акселерометром

## 1. Назначение проекта

BalanStick — мобильная физическая аркада. Игрок наклоном телефона удерживает вертикально стоящую бейсбольную биту, набирает очки за длительность попытки, движение и контролируемый наклон, заряжает временный усилитель и взаимодействует с появляющимися препятствиями.

## 2. Быстрый вход в проект

- Основная сцена: [`Assets/Scenes/Game.unity`](Assets/Scenes/Game.unity).
- Корневой игровой префаб: [`Assets/Prefabs/Game.prefab`](Assets/Prefabs/Game.prefab).
- Игровые менеджеры: [`Assets/Prefabs/GameManager.prefab`](Assets/Prefabs/GameManager.prefab).
- Игровой объект биты: [`Assets/Prefabs/Stick.prefab`](Assets/Prefabs/Stick.prefab).
- Интерфейс: [`Assets/Prefabs/Canvas.prefab`](Assets/Prefabs/Canvas.prefab).
- Код проекта: [`Assets/Scripts`](Assets/Scripts).
- Конфигурация прогрессии: [`Assets/Assets/Progression/ProgressionConfig.asset`](Assets/Assets/Progression/ProgressionConfig.asset).
- Список пакетов: [`Packages/manifest.json`](Packages/manifest.json).
- Настройки проекта: [`ProjectSettings`](ProjectSettings).

В Build Settings включена только `Assets/Scenes/Game.unity`.

## 3. Игровой цикл

1. При запуске игра ждёт, пока телефон будет расположен экраном вверх.
2. После короткого удержания выполняется калибровка акселерометра и разблокируется управление.
3. Наклон телефона преобразуется в горизонтальную силу, воздействующую на Rigidbody биты.
4. Пока попытка активна, игрок получает очки и заряжает boost движением и наклоном биты.
5. Полностью заряженный boost можно активировать кнопкой; он временно увеличивает множитель очков.
6. По мере роста счёта карта масштабируется и переключает текстуры. В заданных диапазонах счёта могут появляться шары.
7. Если угол биты превышает порог проигрыша, управление и начисление очков блокируются, результат обрабатывает прогрессия, UI показывает retry.
8. Retry возвращает Rigidbody биты в исходное состояние, сбрасывает состояние попытки и запускает новый раунд.

## 4. Карта основных зависимостей

```text
Input.acceleration
       |
       v
StickTiltForce ------------------------------+
  |      |          |          |             |
  v      v          v          v             v
Score  Boost   BalloonManager  Retry UI  ProgressionManager
  |      |          |                        |
  +------+          v                        v
  |             Balloon                 PlayerPrefs
  v
MapController / BoneScaleByScore
```

Центральный компонент — `StickTiltForce`. Он управляет вводом, стартовым допуском и состоянием проигрыша, а также публикует события:

- `StartGateStateChanged` — телефон прошёл стартовую проверку и управление разблокировано;
- `RetryStateChanged` — попытка проиграна либо состояние retry очищено.

На эти события подписаны подсчёт очков, boost, система шаров, прогрессия и UI. Большинство ссылок задаётся через Inspector. Часть связей между вложенными префабами назначена overrides непосредственно в `Game.unity`, поэтому итоговую конфигурацию следует проверять в сцене, а не только в исходных префабах.

## 5. Рабочие модули

### 5.1. Управление битой и состояние попытки

- [`StickTiltForce.cs`](Assets/Scripts/StickTiltForce.cs) — читает `Input.acceleration`, калибрует нейтральный наклон, фильтрует ввод, прикладывает силы к Rigidbody, определяет проигрыш и принимает внешние толчки.
- [`StickRetryController.cs`](Assets/Scripts/StickRetryController.cs) — возвращает позицию, вращение и скорости биты в исходное состояние, затем снимает retry-блокировку.
- [`StickLossDragByScore.cs`](Assets/Scripts/StickLossDragByScore.cs) — после проигрыша изменяет drag и angular drag в зависимости от набранного счёта и текущего падения.
- Физические материалы: [`Assets/Physics`](Assets/Physics).

Основной префаб: [`Assets/Prefabs/Stick.prefab`](Assets/Prefabs/Stick.prefab).

### 5.2. Очки и boost

- [`ScoreCouter.cs`](Assets/Scripts/ScoreCouter.cs) — рабочий счётчик. Начисляет базовые очки за время и очки за скорость/наклон; учитывает множитель boost. Имя класса содержит историческую опечатку `Couter`.
- [`BoostChargeBar.cs`](Assets/Scripts/BoostChargeBar.cs) — заряжает шкалу от скорости и наклона, управляет кнопкой boost и временным множителем очков.

Оба компонента находятся в [`GameManager.prefab`](Assets/Prefabs/GameManager.prefab), а ссылки на биту и UI назначены в основной сцене.

### 5.3. Карта и визуальная передача прогресса

- [`MapController.cs`](Assets/Scripts/MapController.cs) — выбирает текстуру карты по диапазону счёта, плавно масштабирует поверхность и выполняет переход к следующей текстуре.
- [`BoneScaleByScore.cs`](Assets/Scripts/BoneScaleByScore.cs) — изменяет масштаб выбранной кости модели биты по мере роста счёта.
- [`HeadController.cs`](Assets/Scripts/HeadController.cs) — поворачивает голову персонажа к цели с ограничениями pitch/yaw/roll; коллайдер головы включается в состоянии retry.

Префаб карты: [`Assets/Prefabs/Map.prefab`](Assets/Prefabs/Map.prefab).  
Текстуры карт: [`Assets/Textures/Map`](Assets/Textures/Map).

### 5.4. Шары и отдельный счётчик попаданий

- [`BalloonManager.cs`](Assets/Scripts/BalloonManager.cs) — создаёт шары в радиальной области в заданном диапазоне игрового счёта, чередует направления появления и обновляет счётчик попаданий.
- [`Balloon.cs`](Assets/Scripts/Balloon.cs) — управляет временем жизни, движением, масштабом, предупреждающим индикатором и толчком биты при столкновении.

Префабы: [`BalloonManager.prefab`](Assets/Prefabs/BalloonManager.prefab) и [`Balloon.prefab`](Assets/Prefabs/Balloon.prefab).

### 5.5. Прогрессия и сохранение

- [`ProgressionConfig.cs`](Assets/Scripts/ProgressionConfig.cs) — ScriptableObject-описание порогов уровней, наград и идентификаторов открываемых возможностей.
- [`ProgressionManager.cs`](Assets/Scripts/ProgressionManager.cs) — обрабатывает итог попытки при переходе в retry, определяет достигнутый уровень и публикует результат.
- [`ProgressionResult.cs`](Assets/Scripts/ProgressionResult.cs) — модели сохранённых данных и результата обработки попытки; это вспомогательные классы, а не MonoBehaviour-компоненты.
- [`PlayerProgressSaveManager.cs`](Assets/Scripts/PlayerProgressSaveManager.cs) — сохраняет JSON в `PlayerPrefs` под ключом `player_progress`.
- [`ProgressionLevelUpPopupUI.cs`](Assets/Scripts/ProgressionLevelUpPopupUI.cs) — показывает окно повышения уровня.
- [`ProgressionResetButtonUI.cs`](Assets/Scripts/ProgressionResetButtonUI.cs) — удаляет сохранённый прогресс.

Текущие пороги уровней в [`ProgressionConfig.asset`](Assets/Assets/Progression/ProgressionConfig.asset): 100, 300 и 1000 очков.

Сейчас сохраняется только номер достигнутого уровня. `softCurrencyReward` и `unlockedFeatureIds` присутствуют в конфигурации, но отдельной логики их применения нет.

### 5.6. UI

- [`StickRetryButtonUI.cs`](Assets/Scripts/StickRetryButtonUI.cs) — переключает стартовую подсказку и кнопку retry, вызывает сброс биты.
- `ProgressionLevelUpPopupUI` и `ProgressionResetButtonUI` — интерфейс прогрессии.
- `BoostChargeBar` непосредственно управляет изображением заполнения и доступностью кнопки boost.
- `BalloonManager` обновляет отдельный счётчик попаданий (`MoneyText`). Если ссылки не назначены, он выполняет резервный поиск UI по именам объектов сцены.

Основной UI-префаб: [`Assets/Prefabs/Canvas.prefab`](Assets/Prefabs/Canvas.prefab).

### 5.7. Платформа и рендеринг

- [`ForceLandscapeOrientation.cs`](Assets/Scripts/ForceLandscapeOrientation.cs) — runtime bootstrap, который до загрузки сцены разрешает только landscape-ориентации. Привязка к GameObject ему не требуется.
- Используется legacy Input Manager (`activeInputHandler: 0`), соответствующий обращениям к `Input.acceleration`, `Input.touchCount` и `Input.gyro`.
- Рендеринг: Universal Render Pipeline 14.0.12.
- URP-ассеты: [`Assets/Settings/Rendering`](Assets/Settings/Rendering).
- UI: uGUI и TextMesh Pro 3.0.7.

## 6. Сцены и префабы

| Объект | Роль |
| --- | --- |
| `Game.unity` | Финальная композиция игровых префабов, персонажа, UI и scene overrides |
| `Game.prefab` | Камера, свет, игровая опора/куб, карта, бита и BalloonManager |
| `GameManager.prefab` | Очки, boost, прогрессия и сохранение |
| `Stick.prefab` | Модель и физика биты, ввод, retry и поведение после проигрыша |
| `Map.prefab` | Верхняя и нижняя поверхности карты, `MapController` |
| `Canvas.prefab` | HUD, boost, подсказка старта, retry и интерфейс прогрессии |
| `BalloonManager.prefab` | Настройки и корень создаваемых шаров |
| `Balloon.prefab` | Коллайдер, визуал и предупреждающий индикатор шара |

Сцены из `Assets/TextMesh Pro/Examples & Extras` являются примерами стороннего пакета и не входят в игру или Build Settings.

## 7. Данные и ассеты

- Модели биты: [`Assets/Models`](Assets/Models).
- Персонажи и скачанные исходники: [`Assets/Downloaded`](Assets/Downloaded).
- Материалы: [`Assets/Materials`](Assets/Materials).
- Текстуры: [`Assets/Textures`](Assets/Textures).
- Анимации индикатора: [`Assets/Animations`](Assets/Animations).
- Progression ScriptableObject: [`Assets/Assets/Progression`](Assets/Assets/Progression).
- TextMesh Pro содержит стандартные ресурсы и полный набор `Examples & Extras`; игровые системы не должны зависеть от example-сцен и example-скриптов.

## 8. Экспериментальные и неподключённые компоненты

Следующие MonoBehaviour-скрипты не имеют GUID-ссылок из основной сцены, игровых префабов или ScriptableObject-ассетов на момент последней сверки:

- [`CameraRootFollowCube.cs`](Assets/Scripts/CameraRootFollowCube.cs) — слежение корня камеры за позицией и yaw объекта;
- [`PhoneUserAccelerationLogger.cs`](Assets/Scripts/PhoneUserAccelerationLogger.cs) — диагностический вывод пользовательского ускорения гироскопа;
- [`ShowResetButtonOnStickTilt.cs`](Assets/Scripts/ShowResetButtonOnStickTilt.cs) — старая логика показа кнопки сброса;
- [`StartGameWhenCubeIsHorizontal.cs`](Assets/Scripts/StartGameWhenCubeIsHorizontal.cs) — альтернативный старый стартовый допуск;
- [`StickResetUI.cs`](Assets/Scripts/StickResetUI.cs) — сброс старой системы куба/старта;
- [`StickTiltScore.cs`](Assets/Scripts/StickTiltScore.cs) — альтернативный счётчик очков, не используемый вместо рабочего `ScoreCouter`.

Перед удалением или повторным подключением этих компонентов нужно отдельно проверить историю и целевое поведение. `ForceLandscapeOrientation` и классы из `ProgressionResult.cs` к этому списку не относятся: они используются без сериализованной ссылки.

## 9. Известные риски и технический долг

### Высокий приоритет

- В `Balloon.Initialize` закомментировано присваивание `targetPoint = sourceTargetPoint`. `BalloonManager` при этом создаёт объект `Target` для каждого шара. Цель не используется и не уничтожается вместе с шаром, поэтому дочерние объекты могут накапливаться во время длинной сессии.
- `StickTiltForce.EvaluateExternalPushByTiltCurve` возвращает `1 + значение кривой`, а результат затем используется как параметр `Mathf.Lerp`. Из-за ограничения параметра интерполяции внешний толчок, вероятно, почти всегда получает максимальный tilt-множитель.

### Средний приоритет

- В проекте нет automated tests и assembly definitions; весь пользовательский код входит в общую `Assembly-CSharp`.
- Ключевые ссылки между системами хранятся в overrides `Game.unity`, что повышает риск незаметно сломать конфигурацию при замене вложенного префаба.
- `BalloonManager` использует глобальный рекурсивный поиск UI по строковым именам как fallback.
- Рабочий класс `ScoreCouter` назван с опечаткой; исправление требует контролируемой миграции Unity-ссылок.
- У `MapController` остались закомментированные следы поддержки normal map, а сериализованные префабы могут содержать устаревшие поля после изменений класса `Map`.

### Низкий приоритет

- В `Assets` хранится полный набор примеров TextMesh Pro, который усложняет навигацию и увеличивает объём проекта.
- Некоторые настройки ориентации в `ProjectSettings` шире, чем фактическое runtime-ограничение в `ForceLandscapeOrientation`.
- Архитектура преимущественно основана на прямых Inspector-ссылках и MonoBehaviour-событиях; границы модулей пока не закреплены `.asmdef` или интерфейсами.

## 10. Проверки перед изменением игровых систем

### Управление или физика

- Проверить старт с телефоном экраном вверх.
- Проверить калибровку, dead zone и обе landscape-ориентации.
- Проверить достижение retry-порога и отсутствие повторного срабатывания сразу после сброса.
- Проверить Rigidbody после retry: позицию, вращение, velocity и angular velocity.

### Очки или boost

- Проверить остановку начисления до стартового допуска и после проигрыша.
- Проверить вклад времени, скорости и угла отдельно.
- Проверить заполнение, доступность кнопки, длительность и множитель boost.
- Проверить сброс счёта и заряда при retry.

### Карта или прогрессия

- Проверить границы всех диапазонов текстур и отсутствие визуального скачка масштаба.
- Проверить пороги уровней точно ниже, на уровне и выше требуемого счёта.
- Проверить сохранение после перезапуска приложения и удаление прогресса.

### Шары

- Проверить диапазоны счёта, интервалы и противоположные направления появления.
- Проверить удаление истёкшего шара и всех связанных runtime-объектов.
- Проверить толчок биты, счётчик попаданий и очистку при retry.

## 11. Правила обновления этого файла

Обновляйте `PROJECT_INDEX.md` в той же задаче, если изменение:

- добавляет, удаляет или переименовывает сцену, игровой префаб либо основной компонент;
- меняет игровой цикл, стартовый допуск, проигрыш, retry или сохранение;
- добавляет новое событие или зависимость между модулями;
- переносит ссылку между префабом и scene override;
- добавляет ScriptableObject, постоянные данные или новый save key;
- подключает экспериментальный компонент либо делает рабочий компонент неиспользуемым;
- устраняет или добавляет существенный пункт технического долга.

При обновлении:

1. Меняйте дату «Последняя сверка с проектом».
2. Описывайте текущее состояние, а не историю всех промежуточных решений.
3. Сохраняйте ссылки относительными корню проекта.
4. Не документируйте `Library`, `Temp`, `Logs`, `obj` и `UserSettings`.
5. Для нового модуля фиксируйте ответственность, точку подключения, входные события/данные и способ проверки.
6. Удаляйте закрытые риски из раздела технического долга или заменяйте их описанием принятого решения в соответствующем модуле.

