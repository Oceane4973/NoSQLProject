
# 1. Tout d'un coup (seeder auto après server)
docker-compose --profile seeder up -d --build

# 4. Logs seeder
docker-compose --profile seeder down -v