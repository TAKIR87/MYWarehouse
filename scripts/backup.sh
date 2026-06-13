#!/usr/bin/env bash
# =============================================================================
# backup.sh — резервное копирование БД и медиа-файлов WarehouseAPI
# =============================================================================
# Использование:
#   ./scripts/backup.sh
#
# Требования:
#   - Файл .env в корне репозитория (рядом с папкой scripts/)
#   - Установлен pg_dump (входит в postgresql-client)
#   - Для Docker-варианта: установлен docker
# =============================================================================

set -euo pipefail  # Прерывать при ошибке, неинициализированных переменных

# ─── Пути ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$ROOT_DIR/.env"
BACKUP_DIR="$ROOT_DIR/backups/data"
UPLOADS_DIR="$ROOT_DIR/WarehouseAPI/uploads"    # папка с медиа-файлами
TIMESTAMP="$(date +%Y_%m_%d_%H%M%S)"

# ─── Загружаем переменные из .env ────────────────────────────────────────────
if [ ! -f "$ENV_FILE" ]; then
    echo "[ERROR] Файл .env не найден: $ENV_FILE"
    echo "        Создайте его из шаблона: cp .env.example .env"
    exit 1
fi

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

# ─── Проверяем обязательные переменные ───────────────────────────────────────
REQUIRED_VARS=("POSTGRES_DB" "POSTGRES_USER" "POSTGRES_PASSWORD" "POSTGRES_PORT")
for VAR in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!VAR:-}" ]; then
        echo "[ERROR] Переменная $VAR не задана в .env"
        exit 1
    fi
done

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"

# ─── Создаём папку для бэкапов ────────────────────────────────────────────────
mkdir -p "$BACKUP_DIR"

echo "=============================================="
echo "  WarehouseAPI — Резервное копирование"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================="

# ─── ШАГ 1: Дамп базы данных ─────────────────────────────────────────────────
DB_BACKUP_FILE="$BACKUP_DIR/db_backup_${TIMESTAMP}.sql"

echo ""
echo "[1/2] Создание дампа базы данных '$POSTGRES_DB'..."

# Пробуем сначала локальный pg_dump, потом через Docker
if command -v pg_dump &> /dev/null; then
    PGPASSWORD="$POSTGRES_PASSWORD" pg_dump \
        --host="$POSTGRES_HOST" \
        --port="$POSTGRES_PORT" \
        --username="$POSTGRES_USER" \
        --dbname="$POSTGRES_DB" \
        --format=plain \
        --no-password \
        --verbose \
        --file="$DB_BACKUP_FILE" 2>&1 | grep -E "(dump|table|sequence|WARNING|ERROR)" || true

elif docker ps --format '{{.Names}}' | grep -q "warehouse_db"; then
    docker exec warehouse_db pg_dump \
        --username="$POSTGRES_USER" \
        --dbname="$POSTGRES_DB" \
        --format=plain \
        > "$DB_BACKUP_FILE"
    echo "    Дамп создан через Docker-контейнер warehouse_db"
else
    echo "[ERROR] Не найден pg_dump и нет запущенного контейнера warehouse_db"
    exit 1
fi

DB_SIZE=$(du -sh "$DB_BACKUP_FILE" | cut -f1)
echo "    ✓ Дамп БД создан: db_backup_${TIMESTAMP}.sql ($DB_SIZE)"

# ─── ШАГ 2: Архивирование медиа-файлов ───────────────────────────────────────
MEDIA_BACKUP_FILE="$BACKUP_DIR/media_backup_${TIMESTAMP}.tar.gz"

echo ""
echo "[2/2] Архивирование медиа-файлов..."

if [ -d "$UPLOADS_DIR" ]; then
    tar -czf "$MEDIA_BACKUP_FILE" \
        -C "$(dirname "$UPLOADS_DIR")" \
        "$(basename "$UPLOADS_DIR")" \
        2>/dev/null

    MEDIA_SIZE=$(du -sh "$MEDIA_BACKUP_FILE" | cut -f1)
    echo "    ✓ Медиа-архив создан: media_backup_${TIMESTAMP}.tar.gz ($MEDIA_SIZE)"
else
    echo "    ℹ Папка uploads не найдена ($UPLOADS_DIR) — пропускаем медиа-бэкап"
    # Создаём пустой архив чтобы скрипт восстановления работал консистентно
    mkdir -p "$UPLOADS_DIR"
    tar -czf "$MEDIA_BACKUP_FILE" -C "$(dirname "$UPLOADS_DIR")" "$(basename "$UPLOADS_DIR")"
    echo "    ✓ Создан пустой архив медиа (папка uploads была пустой)"
fi

# ─── ШАГ 3: Удаление старых бэкапов (оставляем последние 5) ─────────────────
echo ""
echo "[3/3] Ротация бэкапов — оставляем последние 5..."

DB_COUNT=$(find "$BACKUP_DIR" -name "db_backup_*.sql" | wc -l)
if [ "$DB_COUNT" -gt 5 ]; then
    find "$BACKUP_DIR" -name "db_backup_*.sql" \
        | sort | head -n $(( DB_COUNT - 5 )) \
        | xargs rm -f
    echo "    ✓ Удалено старых дампов БД: $(( DB_COUNT - 5 ))"
fi

MEDIA_COUNT=$(find "$BACKUP_DIR" -name "media_backup_*.tar.gz" | wc -l)
if [ "$MEDIA_COUNT" -gt 5 ]; then
    find "$BACKUP_DIR" -name "media_backup_*.tar.gz" \
        | sort | head -n $(( MEDIA_COUNT - 5 )) \
        | xargs rm -f
    echo "    ✓ Удалено старых архивов медиа: $(( MEDIA_COUNT - 5 ))"
fi

# ─── Итог ─────────────────────────────────────────────────────────────────────
echo ""
echo "=============================================="
echo "  Резервное копирование завершено успешно"
echo "  Файлы сохранены в: $BACKUP_DIR"
echo ""
echo "  БД:    db_backup_${TIMESTAMP}.sql"
echo "  Медиа: media_backup_${TIMESTAMP}.tar.gz"
echo "=============================================="