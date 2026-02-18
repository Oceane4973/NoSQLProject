import asyncio
import aiohttp
import json
import os
import random
from faker import Faker
from faker_commerce import Provider
import signal
import sys

fake = Faker('fr_FR')
fake.add_provider(Provider)

async def post_bulk(session, endpoint, data):
    url = f"{os.getenv('SERVER_URL', 'http://localhost:3001')}/api/DataSeeder/{endpoint}"
    print(f"POST {url} ({len(data)} items)")
    
    try:
        async with session.post(url, json=data) as resp:
            result = await resp.json()
            print(f"{endpoint}: {result}")
            return result
    except Exception as e:
        print(f"{endpoint}: {e}")
        raise

def generate_articles(count: int) -> list[dict]:
    return [
        {
            "id": str(fake.uuid4()),
            "name": fake.ecommerce_name(),
            "price": round(random.uniform(5, 500), 2)
        }
        for _ in range(count)
    ]

def generate_users(count: int) -> list[dict]:
    return [
        {
            "id": str(fake.uuid4()),
            "userName": fake.user_name(),
            "email": fake.email()
        }
        for _ in range(count)
    ]

def generate_orders(users: list[dict], articles: list[dict], count: int) -> list[dict]:
    return [
        {
            "id": str(fake.uuid4()),
            "userId": random.choice(users)["id"],
            "articleId": random.choice(articles)["id"],
            "quantity": random.randint(1, 5)
        }
        for _ in range(count)
    ]

def generate_social_graph(users: list[dict], edges_per_user: int = 5) -> list[dict]:
    follows = []
    for user in users:
        candidates = [u for u in users if u["id"] != user["id"]]
        for _ in range(random.randint(1, min(edges_per_user, len(candidates)))):
            if candidates:
                target = random.choice(candidates)
                follows.append({"followerId": user["id"], "followingId": target["id"]})
    return follows

async def main():
    print("Starting data seeder...")
    
    user_count = int(os.getenv('USER_COUNT', '1000'))
    article_count = int(os.getenv('ARTICLE_COUNT', '500'))
    order_count = int(os.getenv('ORDER_COUNT', '5000'))
    
    print(f"Generating data: {user_count} users, {article_count} articles, {order_count} orders")
    
    articles = generate_articles(article_count)
    users = generate_users(user_count)
    orders = generate_orders(users, articles, order_count)
    follows = generate_social_graph(users)
    
    timeout = aiohttp.ClientTimeout(total=300)
    
    async with aiohttp.ClientSession(timeout=timeout) as session:
        await post_bulk(session, "articles", articles)
        await post_bulk(session, "users", users)
        await post_bulk(session, "social-graph", follows)
        await post_bulk(session, "orders", orders)
    
    print("PostgreSQL seeding completed successfully!")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSeeder interrupted")
        sys.exit(0)
    except Exception as e:
        print(f"Seeder failed: {e}")
        sys.exit(1)
