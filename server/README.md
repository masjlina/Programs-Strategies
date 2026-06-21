## Структура проєкту

- `WebAPI` — основний проєкт, який запускає веб-сервер API.
- `Application` — бізнес-логіка, сервіси та мапери для роботи з даними.
- `Domain` — сутності БД, контекст бази даних `ApplicationDbContext` та міграції EF Core.
- `Shared` — спільні типи, DTO та допоміжні утиліти.

## Що потрібно для локального запуску

Перед запуском переконайтеся, що на вашому комп'ютері встановлено:
1. **.NET 10 SDK**
2. **PostgreSQL** (база даних)

## Як підготувати базу даних

1. Перейдіть до папки `server/WebAPI`.
2. Налаштуйте підключення до бази даних:
   - Скопіюйте файл `appsettings.Development.example.json` та назвіть копію `appsettings.Development.json`.
   - Вкажіть правильний рядок підключення (`ConnectionString`) до вашої локальної БД PostgreSQL.

Приклад вмісту `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "ConnStr": "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=ProgramsStrategies"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173"
    ]
  },
  "Authentication": {
    "Schemes": {
      "Bearer": {
        "ValidIssuer": "http://localhost:5257",
        "ValidAudiences": [
          "http://localhost:5173"
        ],
        "SigningKeys": [
          {
            "Issuer": "http://localhost:5257",
            "Value": "<base64-key>"
          }
        ]
      }
    }
  }
}
```

### Параметри конфігурації:
- **`ConnectionStrings:ConnStr`** — рядок підключення до вашої локальної бази даних PostgreSQL.
- **`Cors:AllowedOrigins`** — список дозволених адрес клієнтів (сюди потрібно внести адресу вашого React-додатку, наприклад, `http://localhost:5173`).
- **`Authentication:Schemes:Bearer`** — конфігурація для JWT-авторизації:
  - `ValidIssuer` — ідентифікатор видавця токена.
  - `ValidAudiences` — перелік адрес отримувачів токена (повинен збігатися з адресою фронтенд-клієнта).
  - `SigningKeys` — список ключів підпису JWT. Значення `Value` має бути закодованим у Base64 симетричним ключем підпису (довжиною щонайменше 256 біт).


> [!NOTE]  
> База даних мігрується та створюється автоматично при старті сервера завдяки `db.Database.Migrate()` у `Program.cs`. Також при першому запуску автоматично створюється обліковий запис адміністратора із конфігураційного файлу `users.json`.

Якщо ви бажаєте застосувати міграції вручну через CLI, виконайте у папці `server`:
```bash
dotnet ef database update --project Domain --startup-project WebAPI
```
*(Якщо утиліту `dotnet ef` не встановлено, запустіть `dotnet tool install --global dotnet-ef`)*

## Запуск проєкту

Відкрийте термінал у папці `server` та виконайте команди:

### 1. Відновити залежності
```bash
dotnet restore
```

### 2. Запустити сервер
```bash
dotnet run --project WebAPI
```

## Доступ до API та Swagger

Після успішного запуску сервер буде доступний за адресами:
- `http://localhost:5257`
- `https://localhost:7054`

Swagger UI для тестування API знаходиться за адресою:
- **`https://localhost:7054/swagger`** (або `/swagger/index.html`)

---

## Авторизація та імпорт початкових даних (Seeding)

Для захисту операцій створення, редагування та видалення даних на сервері впроваджено JWT-авторизацію. 

### Дані для входу адміністратора за замовчуванням (з файлу `users.json`):
- **Email:** `admin@admin.com`
- **Пароль:** `admin`

### Як виконувати захищені запити у Swagger UI:
1. Запустіть сервер та відкрийте сторінку Swagger.
2. Знайдіть ендпоінт **`POST /api/sign-in`**.
3. Натисніть **Try it out** та відправте запит із даними адміністратора:
   ```json
   {
     "email": "admin@admin.com",
     "password": "admin"
   }
   ```
4. У відповіді ви отримаєте об'єкт із полем `accessToken`. Скопіюйте значення токена.
5. Натисніть кнопку **Authorize** (іконка замочка) у правому верхньому кутку сторінки Swagger.
6. У полі введення вкажіть токен у форматі: `Bearer <ваш_токен>` (наприклад: `Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...`).
7. Натисніть **Authorize**, після чого ви зможете успішно викликати будьякі захищені ендпоінти.

### Додавання початкових даних до бази даних (потребує авторизації):
1. **Імпорт громад та населених пунктів:** виконайте запит **`POST /api/UploadFile/official-data`**, прикріпивши файл `official-data.csv` із кореня проєкту.
2. **Імпорт стратегій розвитку:** виконайте запит **`POST /api/uploadFile`**, прикріпивши файл `data.json` із кореня проєкту.
