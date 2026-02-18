# 1. Démarrer stack complète SANS seeder
docker compose up -d server postgres-db

# 2. Lancer le seeder une fois
docker compose --profile seeder up seeder

# 3. Tout d'un coup (seeder auto après server)
docker compose --profile seeder up -d

# 4. Logs seeder
docker compose logs seeder