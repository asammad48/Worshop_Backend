# 05 — Nginx (React static + /api reverse proxy)

sudo cp templates/nginx/myapp.conf /etc/nginx/sites-available/myapp.conf
sudo nano /etc/nginx/sites-available/myapp.conf
sudo ln -sf /etc/nginx/sites-available/myapp.conf /etc/nginx/sites-enabled/myapp.conf && sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
sudo apt install -y certbot python3-certbot-nginx && sudo certbot --nginx -d YOUR_DOMAIN -d www.YOUR_DOMAIN && sudo certbot renew --dry-run
sudo tail -n 200 /var/log/nginx/error.log
