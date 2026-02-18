import asyncio
import aiohttp
import json
import os
import random
from faker import Faker
from faker_commerce import Provider
import signal
import sys
from uuid import uuid4

fake = Faker('fr_FR')
fake.add_provider(Provider)

async def post_bulk(session, endpoint, data, targets="Both"):
    """POST bulk avec targets=Postgres/Neo4j/Both"""
    url = f"{os.getenv('SERVER_URL', 'http://localhost:3001')}/api/DataSeeder/{endpoint}?targets={targets}"
    print(f"POST {endpoint} ({len(data)} items) → {targets}")
    
    try:
        timeout = aiohttp.ClientTimeout(total=60)  # 60s par requête
        
        async with session.post(url, json=data, timeout=timeout) as resp:
            result = await resp.json()
            print(f"Server{result['message']}")
            return result
    except asyncio.TimeoutError:
        print(f"Timeout sur {endpoint}")
        raise
    except Exception as e:
        print(f"Server{endpoint}: {e}")
        raise

def generate_articles(count: int) -> list[dict]:
    """Articles avec prix réalistes"""
    return [
        {
            "id": str(uuid4()),
            "name": fake.ecommerce_name(),
            "price": round(random.uniform(5, 500), 2)
        }
        for _ in range(count)
    ]

def generate_users(count: int) -> list[dict]:
    """Users réalistes"""
    return [
        {
            "id": str(uuid4()),
            "userName": fake.user_name(),
            "email": fake.email()
        }
        for _ in range(count)
    ]

def generate_orders(users: list[dict], articles: list[dict], count: int) -> list[dict]:
    """Orders AVEC TotalPrice = Quantity × Article.Price Server"""
    orders = []
    for _ in range(count):
        user = random.choice(users)
        article = random.choice(articles)
        quantity = random.randint(1, 5)
        
        # ServerCALCUL TotalPrice automatique !
        total_price = round(quantity * article["price"], 2)
        
        order = {
            "id": str(uuid4()),
            "userId": user["id"],
            "articleId": article["id"],
            "quantity": quantity,
            "totalPrice": total_price  # ServerParfait pour OrderDto !
        }
        orders.append(order)
    return orders

def generate_social_graph(users: list[dict], edges_per_user: int = 5) -> list[dict]:
    """Social graph réaliste"""
    follows = []
    for user in users:
        candidates = [u for u in users if u["id"] != user["id"]]
        for _ in range(random.randint(1, min(edges_per_user, len(candidates)))):
            if candidates:
                target = random.choice(candidates)
                follows.append({
                    "followerId": user["id"], 
                    "followingId": target["id"]
                })
    return follows

async def main():
    print("ServerStarting HYBRIDE Postgres+Neo4j Data Seeder...")
    
    # Configurable via ENV
    user_count = int(os.getenv('USER_COUNT', '1000'))
    article_count = int(os.getenv('ARTICLE_COUNT', '500'))
    order_count = int(os.getenv('ORDER_COUNT', '5000'))
    
    print(f"ServerGenerating: {user_count} users, {article_count} articles, {order_count} orders")
    
    # Génération
    articles = generate_articles(article_count)
    users = generate_users(user_count)
    orders = generate_orders(users, articles, order_count)
    follows = generate_social_graph(users)
    
    print(f"ServerData ready: {len(follows)} follows")
    
    connector = aiohttp.TCPConnector(limit=100, limit_per_host=30)
    timeout = aiohttp.ClientTimeout(total=None)  # Pas de timeout global
    
    async with aiohttp.ClientSession(
        connector=connector, 
        timeout=timeout,
        headers={'Content-Type': 'application/json'}
    ) as session:
        try:
            # Ordre important: Articles → Users → Social → Orders
            await post_bulk(session, "articles", articles, "Both")
            await post_bulk(session, "users", users, "Both")
            await post_bulk(session, "social-graph", follows, "Both")
            await post_bulk(session, "orders", orders, "Both")
        except Exception as e:
            print(f"SEEDER FAILED: {e}")
            raise

    print("\nServerHYBRIDE SEEDING COMPLETED SUCCESSFULLY!")
    print(f"   → Postgres: {user_count} users + {order_count} orders")
    print(f"   → Neo4j: {user_count} users + {len(follows)} follows + {order_count} BOUGHT")

async def test_small(session):
    """Test rapide 10 records"""
    users = generate_users(10)
    articles = generate_articles(5)
    orders = generate_orders(users, articles, 20)
    follows = generate_social_graph(users, 2)
    
    async with aiohttp.ClientSession() as session:
        await post_bulk(session, "articles", articles, "Both")
        await post_bulk(session, "users", users, "Both")
        await post_bulk(session, "orders", orders, "Both")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSeeder interrupted")
        sys.exit(0)
    except Exception as e:
        print(f"Seeder failed: {e}")
        sys.exit(1)