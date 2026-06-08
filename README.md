# WarehouseAPI

REST API бэкенд для системы складского учёта. Разработан в рамках учебной практики как серверная часть десктопного приложения на WPF (C#).

## Содержание

- [Технологии](#технологии)
- [Архитектура](#архитектура)
- [Структура проекта](#структура-проекта)
- [База данных](#база-данных)
- [API — эндпоинты](#api--эндпоинты)
- [Запуск проекта](#запуск-проекта)
- [Переменные окружения](#переменные-окружения)
- [Формат ответа при ошибке](#формат-ответа-при-ошибке)

---

## Технологии

| Технология | Версия | Назначение |
|---|---|---|
| .NET | 10.0 | Платформа |
| ASP.NET Core Web API | 10.0 | HTTP-сервер |
| Entity Framework Core | 10.0 | ORM |
| Npgsql EF Core PostgreSQL | 10.0 | Провайдер PostgreSQL |
| PostgreSQL | 15+ | База данных |
| Swashbuckle (Swagger) | 10.1 | Документация API |

---

## Архитектура

Проект построен по слоистой архитектуре с разделением ответственности:

```
HTTP-запрос
    │
    ▼
ExceptionHandlingMiddleware   ← перехватывает все необработанные исключения
    │
    ▼
ValidationFilter              ← проверяет DTO через Data Annotations
    │
    ▼
Controller                    ← принимает запрос, возвращает HTTP-ответ
    │
    ▼
Service                       ← бизнес-логика, транзакции
    │
    ▼
Repository                    ← доступ к базе данных через EF Core
    │
    ▼
AppDbContext → PostgreSQL
```

**Принципы:**
- Каждый слой взаимодействует только со слоем ниже через интерфейс
- Все зависимости регистрируются через DI (`AddScoped`)
- Контроллеры не содержат бизнес-логики и `try/catch` — исключения обрабатываются глобально

---

## Структура проекта

```
WarehouseAPI/
├── Controllers/                  # HTTP-эндпоинты
│   ├── ProductsController.cs
│   ├── StockController.cs
│   ├── OperationsController.cs
│   ├── CounterpartiesController.cs
│   └── AnalyticsController.cs
│
├── Services/                     # Бизнес-логика
│   ├── Interfaces/
│   └── *.cs
│
├── Repositories/                 # Доступ к данным
│   ├── Interfaces/
│   └── *.cs
│
├── Models/                       # Сущности EF Core (6 таблиц)
│   ├── Product.cs
│   ├── Stock.cs
│   ├── Operation.cs
│   ├── OperationItem.cs
│   ├── Counterparty.cs
│   └── Contact.cs
│
├── DTOs/                         # Объекты передачи данных
│   ├── Products/
│   ├── Counterparties/
│   ├── Operations/
│   ├── Stock/
│   └── Analytics/
│
├── Data/
│   └── AppDbContext.cs            # DbContext, конфигурация связей
│
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs
│
├── Filters/
│   └── ValidationFilter.cs
│
├── Migrations/                    # EF Core миграции
├── appsettings.json
└── Program.cs
```

---

## База данных

### Схема связей

```
Products ──────────── Stocks
   │                 (1 : 1)
   │
   └──── OperationItems ──── Operations ──── Counterparties
              (N)               (1)               (1)
                                                   │
                                                Contacts
                                                  (N)
```

### Таблицы

#### `Products` — товары
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| Name | varchar(200) | Наименование |
| Article | varchar(50) | Артикул (уникальный) |
| Unit | varchar(20) | Единица измерения (шт, кг, л) |
| Price | decimal | Цена |
| CreatedAt | timestamp | Дата создания |

#### `Stocks` — остатки на складе
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| ProductId | int | FK → Products |
| Quantity | decimal | Текущий остаток |
| UpdatedAt | timestamp | Время последнего изменения |

#### `Operations` — складские операции
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| Type | enum | `Income` / `Sale` / `Transfer` / `WriteOff` |
| Date | timestamp | Дата и время операции |
| Comment | text | Комментарий / причина |
| CounterpartyId | int? | FK → Counterparties (опционально) |

#### `OperationItems` — строки операции
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| OperationId | int | FK → Operations |
| ProductId | int | FK → Products |
| Quantity | decimal | Количество |
| Price | decimal | Цена на момент операции |

#### `Counterparties` — контрагенты
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| Name | varchar(200) | Наименование |
| Type | enum | `Client` / `Supplier` / `Company` |
| Inn | varchar(12) | ИНН (опционально) |
| Address | varchar(300) | Адрес (опционально) |

#### `Contacts` — контакты контрагентов
| Поле | Тип | Описание |
|---|---|---|
| Id | int | Первичный ключ |
| CounterpartyId | int | FK → Counterparties |
| Name | varchar(100) | Имя контакта |
| Phone | varchar | Телефон |
| Email | varchar | Email |

---

## API — эндпоинты

### Товары `/api/products`

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/products` | Список всех товаров с текущими остатками |
| `GET` | `/api/products/{id}` | Товар по ID |
| `POST` | `/api/products` | Создать товар |
| `PUT` | `/api/products/{id}` | Обновить товар |
| `DELETE` | `/api/products/{id}` | Удалить товар (нельзя если есть остаток) |

<details>
<summary>Пример POST /api/products</summary>

**Запрос:**
```json
{
  "name": "Молоко 1л",
  "article": "MLK-001",
  "unit": "шт",
  "price": 89.90
}
```

**Ответ `201 Created`:**
```json
{
  "id": 1,
  "name": "Молоко 1л",
  "article": "MLK-001",
  "unit": "шт",
  "price": 89.90,
  "currentStock": 0,
  "createdAt": "2025-01-15T10:00:00Z"
}
```

**Ошибки:** `400` — артикул уже занят, `400` — ошибка валидации полей
</details>

---

### Остатки `/api/stock`

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/stock` | Остатки по всем товарам |
| `GET` | `/api/stock/{productId}` | Остаток по конкретному товару |

<details>
<summary>Пример GET /api/stock</summary>

**Ответ `200 OK`:**
```json
[
  {
    "productId": 1,
    "productName": "Молоко 1л",
    "productArticle": "MLK-001",
    "unit": "шт",
    "quantity": 150,
    "price": 89.90,
    "totalValue": 13485.00,
    "updatedAt": "2025-01-15T12:00:00Z"
  }
]
```

**Ошибки:** `404` — товар не найден (для `/stock/{productId}`)
</details>

---

### Складские операции `/api/operations`

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/operations/history` | История операций с фильтрами |
| `GET` | `/api/operations/{id}` | Операция по ID |
| `POST` | `/api/operations/income` | Приход товара |
| `POST` | `/api/operations/sale` | Реализация (продажа) |
| `POST` | `/api/operations/transfer` | Перемещение |
| `POST` | `/api/operations/writeoff` | Списание |

**Фильтры для `GET /api/operations/history`:**

| Параметр | Тип | Описание |
|---|---|---|
| `from` | DateTime | Начало периода |
| `to` | DateTime | Конец периода |
| `type` | enum | `Income` / `Sale` / `Transfer` / `WriteOff` |
| `counterpartyId` | int | Фильтр по контрагенту |
| `productId` | int | Фильтр по товару |
| `page` | int | Страница (по умолчанию 1) |
| `pageSize` | int | Размер страницы (по умолчанию 20) |

<details>
<summary>Пример POST /api/operations/income</summary>

**Запрос:**
```json
{
  "counterpartyId": 1,
  "comment": "Плановая поставка",
  "items": [
    { "productId": 3, "quantity": 100, "price": 45.00 },
    { "productId": 7, "quantity": 50,  "price": 120.00 }
  ]
}
```

**Ответ `201 Created`:**
```json
{
  "id": 42,
  "type": 0,
  "typeLabel": "Приход",
  "date": "2025-01-15T10:00:00Z",
  "comment": "Плановая поставка",
  "counterpartyId": 1,
  "counterpartyName": "ООО Поставщик",
  "items": [
    { "productId": 3, "productName": "Товар А", "productArticle": "ART-003", "quantity": 100, "price": 45.00 }
  ],
  "totalAmount": 10500.00
}
```

**Ошибки:** `400` — недостаточно остатка (для sale/transfer/writeoff), `400` — пустой список товаров
</details>

> ⚠️ Все операции выполняются в транзакции. При недостаточном остатке для `Sale`, `Transfer`, `WriteOff` — возвращается `400 Bad Request`, изменений в БД не происходит.

---

### Контрагенты `/api/counterparties`

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/api/counterparties` | Все контрагенты |
| `GET` | `/api/counterparties?type=Client` | Фильтрация по типу |
| `GET` | `/api/counterparties/clients` | Только клиенты |
| `GET` | `/api/counterparties/suppliers` | Только поставщики |
| `GET` | `/api/counterparties/companies` | Только компании |
| `GET` | `/api/counterparties/{id}` | Контрагент по ID |
| `POST` | `/api/counterparties` | Создать контрагента с контактами |
| `PUT` | `/api/counterparties/{id}` | Обновить контрагента и контакты |
| `DELETE` | `/api/counterparties/{id}` | Удалить контрагента |

<details>
<summary>Пример POST /api/counterparties</summary>

**Запрос:**
```json
{
  "name": "ООО Ромашка",
  "type": 1,
  "inn": "7701234567",
  "address": "г. Москва, ул. Ленина, 1",
  "contacts": [
    { "name": "Иван Иванов", "phone": "+79001234567", "email": "ivan@romashka.ru" }
  ]
}
```

**Ответ `201 Created`:**
```json
{
  "id": 5,
  "name": "ООО Ромашка",
  "type": 1,
  "typeLabel": "Поставщик",
  "inn": "7701234567",
  "address": "г. Москва, ул. Ленина, 1",
  "contacts": [
    { "id": 3, "name": "Иван Иванов", "phone": "+79001234567", "email": "ivan@romashka.ru" }
  ]
}
```

**Ошибки:** `404` — контрагент не найден, `400` — ошибка валидации
</details>

---

### Аналитика `/api/analytics`

| Метод | Путь | Параметры | Описание |
|---|---|---|---|
| `GET` | `/api/analytics/top-products` | `from`, `to`, `limit` (1–100) | Топ продаваемых товаров |
| `GET` | `/api/analytics/turnover` | `from`, `to` | Обороты за период |
| `GET` | `/api/analytics/low-stock` | `minQuantity` (по умолч. 5) | Остатки ниже минимума |

<details>
<summary>Пример GET /api/analytics/turnover</summary>

**Запрос:** `GET /api/analytics/turnover?from=2025-01-01&to=2025-01-31`

**Ответ `200 OK`:**
```json
{
  "from": "2025-01-01T00:00:00Z",
  "to": "2025-01-31T00:00:00Z",
  "incomeAmount": 150000.00,
  "saleAmount": 98000.00,
  "writeOffAmount": 2000.00,
  "profit": -52000.00,
  "byDay": [
    { "date": "2025-01-15", "incomeAmount": 45000.00, "saleAmount": 12000.00 }
  ]
}
```

**Ошибки:** `400` — дата начала позже даты конца
</details>

---

### Служебные эндпоинты

| Метод | Путь | Описание |
|---|---|---|
| `GET` | `/` | Информация о сервисе |
| `GET` | `/health` | Проверка доступности (`"ok"`) |
| `GET` | `/swagger` | Swagger UI (только в Development) |

---

## Запуск проекта

### Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/) **или** [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

### Вариант 1 — Обычный запуск (без Docker)

**1. Клонировать репозиторий:**
```bash
git clone <url-репозитория>
cd MYWarehouse-maintenance
```

**2. Создать базу данных в PostgreSQL:**
```sql
CREATE DATABASE warehouse_db;
```

**3. Настроить строку подключения через User Secrets (безопасно — не попадает в git):**
```bash
cd WarehouseAPI
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=warehouse_db;Username=postgres;Password=ВАШ_ПАРОЛЬ"
```

**4. Применить миграции:**
```bash
dotnet ef database update
```

**5. Запустить API:**
```bash
dotnet run
```

API доступен по адресам:
- `http://localhost:5023`
- `https://localhost:7150`
- Swagger UI: `http://localhost:5023/swagger`

---

### Вариант 2 — Запуск через Docker Compose

**Требования:** Docker Desktop установлен и запущен.

**1. Клонировать репозиторий:**
```bash
git clone <url-репозитория>
cd MYWarehouse-maintenance
```

**2. Создать файл `.env` в корне репозитория:**
```bash
cp .env.example .env
```
Открыть `.env` и заполнить своими значениями (см. раздел [Переменные окружения](#переменные-окружения)).

**3. Запустить все сервисы одной командой:**
```bash
docker-compose up -d
```

Docker автоматически:
- Поднимет контейнер PostgreSQL
- Соберёт образ WarehouseAPI
- Применит миграции при старте
- Запустит API

**4. Проверить что всё работает:**
```bash
curl http://localhost:5023/health
# Ответ: "ok"
```

**5. Открыть Swagger UI:**
```
http://localhost:5023/swagger
```

**Остановить все контейнеры:**
```bash
docker-compose down
```

**Остановить и удалить данные БД:**
```bash
docker-compose down -v
```

---

## Переменные окружения

Все настройки задаются через файл `.env` (для Docker) или через User Secrets / переменные окружения (для обычного запуска).

**Файл `.env.example`** — шаблон, хранится в репозитории. Скопируйте его в `.env` и заполните своими значениями. Сам `.env` в git не попадает.

| Переменная | Пример значения | Описание |
|---|---|---|
| `POSTGRES_DB` | `warehouse_db` | Имя базы данных |
| `POSTGRES_USER` | `postgres_user` | Пользователь PostgreSQL |
| `POSTGRES_PASSWORD` | `YOUR_STRONG_PASSWORD` | Пароль PostgreSQL |
| `POSTGRES_PORT` | `5432` | Порт PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Режим ASP.NET Core (`Development` включает Swagger) |
| `ASPNETCORE_URLS` | `http://+:5023` | Адрес и порт HTTP-сервера |
| `ConnectionStrings__DefaultConnection` | `Host=db;Port=5432;...` | Полная строка подключения к PostgreSQL |

> ⚠️ Никогда не коммитьте файл `.env` в репозиторий. Он добавлен в `.gitignore`.

---

## Формат ответа при ошибке

Все ошибки возвращаются в едином формате:

```json
{
  "status": 400,
  "message": "Недостаточно товара ID=3: на складе 5, запрошено 10",
  "path": "/api/operations/sale",
  "traceId": "0HN7K2J1L4Q3A:00000001"
}
```

| HTTP-код | Причина |
|---|---|
| `400` | Ошибка валидации DTO или бизнес-правил |
| `404` | Запрошенный ресурс не найден |
| `500` | Внутренняя ошибка сервера |