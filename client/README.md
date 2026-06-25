# Клієнтська частина «Є рішення»

Веб-інтерфейс для перегляду стратегій і програм розвитку територіальних громад, областей і районів України. Клієнт працює з REST API бекенду (`server/WebAPI`).

**Стек:** React 19, TypeScript, Vite 8, React Router 7.

---

## Що потрібно для локального запуску

1. **Node.js** 20+ (рекомендовано LTS)
2. **npm** (йде разом із Node.js)
3. **Запущений бекенд** — фронтенд отримує дані через API (за замовчуванням `http://localhost:5257`)

Детальні інструкції з налаштування API та бази даних — у `[../server/README.md](../server/README.md)`.

---

## Як запустити локально

### 1. Встановити залежності

```bash
cd client
npm install
```

### 2. Налаштувати адресу API

Скопіюйте приклад конфігурації:

```bash
cp .env.local.example .env.local
```

За замовчуванням у `.env.local.example` вказано:

```env
VITE_API_BASE_URL=http://localhost:5257
```

Якщо бекенд працює на іншому хості або порту — змініть значення. Якщо `.env.local` відсутній, використовується той самий URL із `src/lib/api.ts`.

### 3. Запустити dev-сервер

```bash
npm run dev
```

Сайт буде доступний за адресою **[http://localhost:5173](http://localhost:5173)**.

Vite автоматично перезавантажує сторінку при зміні коду (HMR).

### 4. Переконатися, що API відповідає

Перед роботою з пошуком, завантаженням або адмін-панеллю переконайтеся, що бекенд запущений і база наповнена (див. `server/README.md`).

---

## Корисні npm-скрипти


| Команда           | Опис                                               |
| ----------------- | -------------------------------------------------- |
| `npm run dev`     | Dev-сервер Vite на порту 5173                      |
| `npm run build`   | Перевірка TypeScript + production-збірка в `dist/` |
| `npm run preview` | Перегляд production-збірки локально                |
| `npm run lint`    | Перевірка ESLint                                   |


---

## Маршрути застосунку


| URL               | Сторінка                                 | Доступ            |
| ----------------- | ---------------------------------------- | ----------------- |
| `/`               | Головна (hero + системна статистика)     | Усі               |
| `/search`         | Пошук територій і стратегій              | Усі               |
| `/strategies/:id` | Перегляд однієї стратегії (дерево цілей) | Усі               |
| `/upload`         | Завантаження JSON-стратегії              | Лише авторизовані |
| `/admin`          | Редагування URL громад/областей          | Лише авторизовані |


Авторизація — через кнопку **«Увійти»** у шапці (`LoginDropdown`). Після входу з’являються пункти «Завантаження» та «Адмін-панель». Захищені маршрути обгорнуті в `ProtectedRoute`.

---

## Структура проєкту

```
client/
├── public/                    # Статичні файли (favicon, іконки)
├── src/
│   ├── main.tsx               # Точка входу React
│   ├── App.tsx                # Маршрутизація та AuthProvider
│   ├── App.css                # Глобальні стилі layout, кнопок, шапки
│   ├── index.css              # CSS-змінні та базові стилі
│   │
│   ├── context/
│   │   └── AuthContext.tsx    # Стан авторизації, sign-in / sign-out
│   │
│   ├── layouts/
│   │   └── MainLayout.tsx     # Шапка + контент + футер
│   │
│   ├── pages/                 # Сторінки (кожна — .tsx + .css)
│   │   ├── HomePage/          # Лендінг, блок «Про сайт», SystemDashboard
│   │   ├── SearchPage/        # Пошук і картки територіальних одиниць
│   │   ├── StrategyPage/      # Деталі стратегії та посилання на документ
│   │   ├── UploadPage/        # Форма завантаження стратегії (JSON)
│   │   ├── AdminPage/         # Адмін-панель URL громад/областей
│   │   └── NotFoundPage/      # Сторінка 404
│   │
│   ├── components/
│   │   ├── layout/            # Header, Footer, Container
│   │   ├── auth/              # LoginDropdown
│   │   ├── dashboard/         # SystemDashboard, StatTile (головна)
│   │   ├── search/            # StrategyGoalsTree, StrategyDetailPanel тощо
│   │   ├── CollapsibleBlock/  # UI-блок з розгортанням
│   │   └── ProtectedRoute.tsx # Редирект для неавторизованих
│   │
│   ├── hooks/
│   │   ├── useCountUp.ts      # Анімація лічильника (дашборд)
│   │   └── useInView.ts       # Intersection Observer для анімацій
│   │
│   ├── lib/                   # Логіка роботи з API та даними
│   │   ├── api.ts             # HTTP-клієнт, CORS, JWT refresh
│   │   ├── strategies.ts      # Каталог стратегій, нормалізація, пошук
│   │   ├── strategyMetrics.ts # Метрики для карток/таблиць стратегії
│   │   └── systemStats.ts     # Запит GET /api/Stats для головної
│   │
│   ├── types/                 # TypeScript-інтерфейси доменної моделі
│   │   ├── strategy.ts
│   │   ├── strategicGoal.ts
│   │   ├── operationalGoal.ts
│   │   ├── programTask.ts
│   │   ├── administrativeUnit.ts
│   │   └── administrativeUnitType.ts
│   │
│   └── vite-env.d.ts          # Типи для Vite (import.meta.env, JSON)
│
├── index.html                 # HTML-шаблон
├── vite.config.ts             # Vite + React Compiler
├── tsconfig.json              # Project references
├── tsconfig.app.json          # Налаштування TS для src/
├── eslint.config.js           # ESLint
├── Dockerfile                 # Production-збірка (nginx)
├── nginx.conf                 # Проксі /api → бекенд у Docker
├── .env.local.example         # Приклад змінних середовища
└── package.json
```

---

## Як організований код

### API-шар (`src/lib/`)

- `**api.ts**` — базовий клієнт: `apiGet`, `apiPost`, `apiPatch`, `apiDelete`, зберігання access token, автоматичний refresh через `/api/refresh`.
- `**strategies.ts**` — завантаження списку стратегій і одиниць, побудова «каталогу» для UI, нормалізація ієрархії цілей.
- `**systemStats.ts**` — агрегована статистика системи для головної сторінки.
- `**strategyMetrics.ts**` — розрахунок показників для компонентів перегляду стратегії.

### Сторінки

Кожна сторінка — окремий каталог з компонентом `.tsx` і стилями `.css`. Дані завантажуються в `useEffect` через функції з `lib/`, помилки та стан завантаження обробляються локально на сторінці.

### Компоненти

- `**components/layout/**` — спільна оболонка сайту.
- `**components/search/**` — перегляд стратегії: дерево цілей (`StrategyGoalsTree`), картки, таблиці.
- `**components/dashboard/**` — блок статистики на головній (`SystemDashboard`).

### Типи (`src/types/`)

Інтерфейси, що відповідають моделі бекенду: стратегія → стратегічна ціль → операційна ціль → програмне завдання.

---

## Production-збірка

```bash
npm run build
```

Артефакти потрапляють у `dist/`. Для Docker-образу використовується `client/Dockerfile`: збірка через Node, віддача статики через nginx.

У Docker Compose API проксується через `nginx.conf` (`/api/` → контейнер `api`). Змінна `VITE_API_BASE_URL` задається **на етапі збірки** (`ARG` у Dockerfile).

---

## Типові проблеми

**«Не вдалося завантажити дані з сервера» на `/search`**

- Перевірте, що бекенд запущений (`dotnet run --project WebAPI` у `server/`).
- Перевірте `VITE_API_BASE_URL` у `.env.local`.
- Переконайтеся, що в БД є дані (імпорт `official-data.csv` через Swagger).

**CORS-помилки в консолі браузера**

- У режимі Development бекенд дозволяє `http://localhost:5173` автоматично.
- Для production/Docker потрібно налаштувати `Cors:AllowedOrigins` на бекенді.

**Сторінки `/upload` та `/admin` не відкриваються**

- Потрібна авторизація через кнопку «Увійти» у шапці.

---

## Пов’язана документація

- [README бекенду](../server/README.md) — API, PostgreSQL, Swagger
- [docker-compose.yml](../docker-compose.yml) — запуск усієї системи в контейнерах

