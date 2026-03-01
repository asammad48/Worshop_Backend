# 03 — PostgreSQL setup

sudo systemctl enable postgresql && sudo systemctl start postgresql
sudo -u postgres psql -c "CREATE DATABASE workshopdb;" && sudo -u postgres psql -c "CREATE USER workshopuser WITH ENCRYPTED PASSWORD 'CHANGE_ME_STRONG';" && sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE workshopdb TO workshopuser;"
sudo -u postgres psql -d workshopdb -c "SELECT now();"
sudo cp scripts/pg_backup.sh /opt/apps/myapp/shared/pg_backup.sh && sudo chmod +x /opt/apps/myapp/shared/pg_backup.sh
( crontab -l 2>/dev/null; echo "15 2 * * * /opt/apps/myapp/shared/pg_backup.sh >> /opt/apps/myapp/backups/pg_backup.log 2>&1" ) | crontab -
