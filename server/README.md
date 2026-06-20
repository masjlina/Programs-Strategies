
## Структура проєкта

- `WebAPI` - основний проєкт, який запускає сервер
- `Application` - логіка роботи з даними
- `Domain` - сутності, підключення до бази даних і міграції
- `Shared` - спільні типи та допоміжні класи

## Що потрібно, щоб запустити проєкт локально

Перед запуском на комп'ютері мають бути встановлені:

1. `.NET 10 SDK`
2. `PostgreSQL`

## Як підготувати базу даних
1. Перейти до WebAPI
2. Додати налаштування підключення до файлу:
- дублювати файл `appsettings.Development.example.json`
- змінити назву на `appsettings.Development.json`

Приклад вмісту:

```json
{
  "ConnectionStrings": {
    "ConnStr": "Host=localhost;Port=5432;Database=programs_strategies;Username=postgres;Password=postgres"
  }
}
```

## Запуск проєкта

Відкрити термінал у корені проєкту та виконати команди по черзі.

### 1. Завантажити залежності

```bash
dotnet restore
```

### 2. Налаштувати структуру бази даних

```bash
dotnet ef database update --project Domain --startup-project WebAPI
```

Якщо команда `dotnet ef` не працює, спочатку встановіть її:

```bash
dotnet tool install --global dotnet-ef
```

### 3. Запустити сервер

```bash
dotnet run --project WebAPI
```

## Доступ до сервера

Після запуску проєкт буде доступний тут:

- `http://localhost:5257`
- `https://localhost:7054`

Swagger для перевірки API:

- `https://localhost:7054/swagger`

## Додати дані до БД
1. Запустити сервер
2. Відкрити сторінку `https://localhost:7054/swagger`
3. Виконати запит "/api/UploadFile/official-data" прікріпивши туди "official-data.csv" з кореня проєкту для заповнення бази даних громадами.
4. Виконати запит "/api/uploadFile" прікріпивши туди "data.json" з кореня проєкту для додавання стратегії громади.
