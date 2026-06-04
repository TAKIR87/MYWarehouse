# Руководство администратора — WarehouseAPI

## 1. Требования к серверу

| Параметр | Минимум | Рекомендуется |
|---|---|---|
| ОС | Ubuntu 22.04 LTS | Ubuntu 24.04 LTS |
| CPU | 1 ядро | 2 ядра |
| ОЗУ | 512 МБ | 1 ГБ |
| Диск | 5 ГБ | 20 ГБ |
| .NET Runtime | 10.0 | 10.0 |
| PostgreSQL | 15+ | 15+ |

---

## 2. Установка зависимостей

```bash
# .NET 10 Runtime
wget https://dot.net/v1/dotnet-install.sh
bash dotnet-install.sh --runtime aspnetcore --version 10.0

# PostgreSQL 15
sudo apt install -y postgresql-15

# Nginx
sudo apt install -y nginx
```

---

## 3. Настройка базы данных

```bash
sudo -u postgres psql
```

```sql
CREATE DATABASE warehouse_db;
CREATE USER warehouse_user WITH PASSWORD 'STRONG_PASSWORD_HERE';
GRANT ALL PRIVILEGES ON DATABASE warehouse_db TO warehouse_user;
\q
```

---

## 4. Деплой приложения

```bash
# Клонировать репозиторий
git clone <url-репозитория> /opt/warehouse
cd /opt/warehouse/WarehouseAPI

# Собрать
dotnet publish -c Release -o /opt/warehouse/publish

# Задать строку подключения через переменную окружения
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=warehouse_db;Username=warehouse_user;Password=STRONG_PASSWORD_HERE"

# Применить миграции
dotnet ef database update

# Запустить
dotnet /opt/warehouse/publish/WarehouseAPI.dll
```

---

## 5. Настройка systemd (автозапуск)

Создать файл `/etc/systemd/system/warehouseapi.service`:

```ini
[Unit]
Description=WarehouseAPI ASP.NET Core Service
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/warehouse/publish
ExecStart=/usr/bin/dotnet /opt/warehouse/publish/WarehouseAPI.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=warehouseapi
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5023
Environment=ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=warehouse_db;Username=warehouse_user;Password=STRONG_PASSWORD_HERE

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable warehouseapi
sudo systemctl start warehouseapi
sudo systemctl status warehouseapi
```

---

## 6. Настройка Nginx (обратный прокси)

Создать файл `/etc/nginx/sites-available/warehouseapi`:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass         http://localhost:5023;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 90s;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/warehouseapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

---

## 7. Регламент устранения сбоев

### 7.1 — API не отвечает

```bash
# Проверить статус сервиса
sudo systemctl status warehouseapi

# Посмотреть последние логи
sudo journalctl -u warehouseapi -n 50 --no-pager

# Перезапустить
sudo systemctl restart warehouseapi
```

**Частые причины:**
- Порт 5023 занят другим процессом → `sudo lsof -i :5023`
- Нет подключения к БД → проверить строку подключения в Environment
- Кончилась память → `free -h`, перезапустить сервис

---

### 7.2 — База данных упала

```bash
# Проверить статус PostgreSQL
sudo systemctl status postgresql

# Запустить если остановлен
sudo systemctl start postgresql

# Проверить лог PostgreSQL
sudo tail -100 /var/log/postgresql/postgresql-15-main.log

# Проверить подключение
psql -h localhost -U warehouse_user -d warehouse_db -c "SELECT 1;"
```

**После восстановления БД** API сам подхватит соединение при следующем запросе — перезапуск не нужен.

---

### 7.3 — Переполнение диска

```bash
# Проверить занятое место
df -h

# Найти крупные файлы
du -sh /var/log/* | sort -rh | head -20

# Очистить старые логи systemd (оставить последние 7 дней)
sudo journalctl --vacuum-time=7d

# Очистить логи Nginx
sudo truncate -s 0 /var/log/nginx/access.log
sudo truncate -s 0 /var/log/nginx/error.log
```

---

### 7.4 — Откат миграции БД

Если после обновления приложения нужно откатить БД к предыдущей версии:

```bash
cd /opt/warehouse/WarehouseAPI

# Посмотреть список миграций
dotnet ef migrations list

# Откатиться к конкретной миграции (указать имя предыдущей)
dotnet ef database update НазваниеПредыдущейМиграции
```

---

### 7.5 — Резервное копирование БД

```bash
# Создать дамп
pg_dump -U warehouse_user -d warehouse_db -F c -f /backup/warehouse_$(date +%Y%m%d).dump

# Восстановить из дампа
pg_restore -U warehouse_user -d warehouse_db -F c /backup/warehouse_20250115.dump
```

Рекомендуется настроить автоматический бэкап через cron:

```bash
# crontab -e
0 2 * * * pg_dump -U warehouse_user -d warehouse_db -F c -f /backup/warehouse_$(date +\%Y\%m\%d).dump
```
