#!/usr/bin/env bash
# =============================================================================
# schedule_backup.sh — настройка автоматического бэкапа через cron
# =============================================================================
# Запустить один раз: ./scripts/schedule_backup.sh
# После этого бэкап будет создаваться каждый день в 02:00
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_SCRIPT="$SCRIPT_DIR/backup.sh"
LOG_FILE="$(dirname "$SCRIPT_DIR")/logs/backup_cron.log"

# Строка для crontab: каждый день в 02:00
CRON_JOB="0 2 * * * $BACKUP_SCRIPT >> $LOG_FILE 2>&1"

# Добавляем в crontab только если ещё нет
if crontab -l 2>/dev/null | grep -qF "$BACKUP_SCRIPT"; then
    echo "✓ Задание cron уже настроено:"
    crontab -l | grep "$BACKUP_SCRIPT"
else
    (crontab -l 2>/dev/null; echo "$CRON_JOB") | crontab -
    echo "✓ Автобэкап добавлен в cron (ежедневно в 02:00)"
    echo "  Команда: $CRON_JOB"
fi

echo ""
echo "Проверить текущий crontab: crontab -l"
echo "Удалить задание: crontab -e  (удалить строку вручную)"