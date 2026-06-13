#!/usr/bin/env bash
# =============================================================================
# restore.sh — восстановление WarehouseAPI из резервной копии
# =============================================================================
# Использование:
#   ./scripts/restore.sh                        — восстановить из последнего бэкапа
#   ./scripts/restore.sh db_backup_2026_06_10_120000.sql   — из конкретного файла
# =============================================================================

set -euo pipefail

# ─── Пути ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
ENV_FILE="$ROOT_DIR/.env"
BACKUP_DIR="$ROOT_DIR/backups/data"
UPLOADS_DIR="$ROOT_DIR/WarehouseAPI/uploads"

# ─── Загружаем переменные из .env ────────────────────────────────────────────
if [ ! -f "$ENV_FILE" ]; then
    echo "[ERROR] Файл .env не найден: $ENV_FILE"
    exit 1
fi

set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

POSTGRES_HOST="${POSTGRES_HOST:-localhost}"

echo "=============================================="
echo "  WarehouseAPI — Восстановление из бэкапа"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================="

# ─── Определяем файл дампа БД ────────────────────────────────────────────────
if [ -n "${1:-}" ]; then
    # Передан конкретный файл
    if [ -f "$BACKUP_DIR/$1" ]; then
        DB_BACKUP_FILE="$BACKUP_DIR/$1"
    elif [ -f "$1" ]; then
        DB_BACKUP_FILE="$1"
    else
        echo "[ERROR] Файл бэкапа не найден: $1"
        exit 1
    fi
else
    # Берём последний по дате
    DB_BACKUP_FILE=$(find "$BACKUP_DIR" -name "db_backup_*.sql" | sort | tail -n 1)
    if [ -z "$DB_BACKUP_FILE" ]; then
        echo "[ERROR] Файлы бэкапов не найдены в $BACKUP_DIR"
        echo "        Сначала запустите: ./scripts/backup.sh"
        exit 1
    fi
fi

# Определяем метку времени из имени файла дампа
BACKUP_TIMESTAMP=$(basename "$DB_BACKUP_FILE" | sed 's/db_backup_//;s/\.sql//')
MEDIA_BACKUP_FILE="$BACKUP_DIR/media_backup_${BACKUP_TIMESTAMP}.tar.gz"

echo ""
echo "  Файл БД:    $(basename "$DB_BACKUP_FILE")"
if [ -f "$MEDIA_BACKUP_FILE" ]; then
echo "  Файл медиа: $(basename "$MEDIA_BACKUP_FILE")"
fi
echo ""

# ─── Предупреждение ───────────────────────────────────────────────────────────
echo "⚠  ВНИМАНИЕ: текущая БД '$POSTGRES_DB' будет полностью пересоздана!"
echo "   Все несохранённые данные будут утеряны."
echo ""
read -r -p "   Продолжить восстановление? (yes/no): " CONFIRM
if [ "$CONFIRM" != "yes" ]; then
    echo "   Восстановление отменено."
    exit 0
fi

# ─── ШАГ 1: Пересоздаём базу данных ─────────────────────────────────────────
echo ""
echo "[1/3] Пересоздание базы данных '$POSTGRES_DB'..."

run_psql() {
    local SQL="$1"
    if command -v psql &> /dev/null; then
        PGPASSWORD="$POSTGRES_PASSWORD" psql \
            --host="$POSTGRES_HOST" \
            --port="$POSTGRES_PORT" \
            --username="$POSTGRES_USER" \
            --dbname="postgres" \
            --no-password \
            -c "$SQL" &>/dev/null
    elif docker ps --format '{{.Names}}' | grep -q "warehouse_db"; then
        docker exec warehouse_db psql \
            --username="$POSTGRES_USER" \
            --dbname="postgres" \
            -c "$SQL" &>/dev/null
    else
        echo "[ERROR] Не найден psql и нет контейнера warehouse_db"
        exit 1
    fi
}

# Разрываем все активные соединения с БД
run_psql "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$POSTGRES_DB' AND pid <> pg_backend_pid();"

# Удаляем и пересоздаём БД
run_psql "DROP DATABASE IF EXISTS \"$POSTGRES_DB\";"
run_psql "CREATE DATABASE \"$POSTGRES_DB\" OWNER \"$POSTGRES_USER\";"

echo "    ✓ База данных пересоздана"

# ─── ШАГ 2: Восстанавливаем дамп ─────────────────────────────────────────────
echo ""
echo "[2/3] Восстановление данных из $(basename "$DB_BACKUP_FILE")..."

if command -v psql &> /dev/null; then
    PGPASSWORD="$POSTGRES_PASSWORD" psql \
        --host="$POSTGRES_HOST" \
        --port="$POSTGRES_PORT" \
        --username="$POSTGRES_USER" \
        --dbname="$POSTGRES_DB" \
        --no-password \
        --quiet \
        --file="$DB_BACKUP_FILE"
elif docker ps --format '{{.Names}}' | grep -q "warehouse_db"; then
    docker exec -i warehouse_db psql \
        --username="$POSTGRES_USER" \
        --dbname="$POSTGRES_DB" \
        --quiet \
        < "$DB_BACKUP_FILE"
fi

echo "    ✓ Данные восстановлены из дампа"

# ─── ШАГ 3: Восстанавливаем медиа-файлы ──────────────────────────────────────
echo ""
echo "[3/3] Восстановление медиа-файлов..."

if [ -f "$MEDIA_BACKUP_FILE" ]; then
    # Очищаем текущую папку uploads
    rm -rf "$UPLOADS_DIR"
    mkdir -p "$(dirname "$UPLOADS_DIR")"

    # Распаковываем архив
    tar -xzf "$MEDIA_BACKUP_FILE" -C "$(dirname "$UPLOADS_DIR")"
    echo "    ✓ Медиа-файлы восстановлены из $(basename "$MEDIA_BACKUP_FILE")"
else
    echo "    ℹ Архив медиа не найден — создаём пустую папку uploads"
    mkdir -p "$UPLOADS_DIR"
fi

# ─── Итог ─────────────────────────────────────────────────────────────────────
echo ""
echo "=============================================="
echo "  Восстановление завершено успешно!"
echo ""
echo "  Запустите API: dotnet run"
echo "  Или через Docker: docker-compose up -d"
echo "=============================================="