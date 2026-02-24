import asyncio
import aiohttp
import os
import sys
from uuid import uuid5, NAMESPACE_DNS

def get_deterministic_uuid(name: str) -> str:
    return str(uuid5(NAMESPACE_DNS, name))

async def post_bulk(session, endpoint, data, targets="Both"):
    url = f"{os.getenv('SERVER_URL', 'http://localhost:3001')}/api/DataSeeder/{endpoint}?targets={targets}"
    print(f"POST {endpoint} ({len(data)} items) → {targets}")
    try:
        timeout = aiohttp.ClientTimeout(total=60)
        async with session.post(url, json=data, timeout=timeout) as resp:
            result = await resp.json()
            print(f"Server: {result.get('message', 'Success')}")
            return result
    except Exception as e:
        print(f"Error on {endpoint}: {e}")
        raise

# --- GÉNÉRATION DÉTERMINISTE ---

def generate_deterministic_users() -> list[dict]:
    """Génère U1 à U30"""
    return [
        {
            "id": get_deterministic_uuid(f"user_{i}"),
            "userName": f"User_{i}",
            "email": f"user{i}@example.com"
        }
        for i in range(1, 31)
    ]

def generate_deterministic_articles() -> list[dict]:
    """Génère P1 à P8 avec rôles spécifiques"""
    names = [
        "Produit Viral",    # P1
        "Produit Populaire", # P2
        "Produit Classique", # P3
        "Divers 4", "Divers 5", "Divers 6", "Divers 7", "Divers 8" # P4-P8
    ]
    return [
        {
            "id": get_deterministic_uuid(f"product_{i}"),
            "name": name,
            "price": round(15.5 * i, 2)
        }
        for i, name in enumerate(names, 1)
    ]

def generate_deterministic_graph(users: list[dict]) -> list[dict]:
    follows = []
    
    c1_idx = range(1, 5)   # U2,U3,U4,U5
    c2_idx = range(5, 10)  # U6-U10
    c3_idx = range(10, 20) 
    c4_idx = range(20, 30) 

    # U2,U3,U4,U5 SUIVENT U1 → FollowersCount(U1) = 4
    for i in c1_idx:  # U2-U5
        follows.append({
            "followerId": users[i]["id"],    # U2-U5
            "followingId": users[0]["id"]    # → U1
        })

    # U6-U10 SUIVENT C1 (U2-U5)
    for i in c2_idx:
        for j in c1_idx:
            follows.append({
                "followerId": users[i]["id"], 
                "followingId": users[j]["id"]
            })

    # U11-U20 SUIVENT C2 (U6-U10)
    for i in c3_idx:
        for j in c2_idx:
            follows.append({
                "followerId": users[i]["id"], 
                "followingId": users[j]["id"]
            })

    # U21-U30 SUIVENT C3 (U11-U20)
    for i in c4_idx:
        for j in c3_idx:
            follows.append({
                "followerId": users[i]["id"], 
                "followingId": users[j]["id"]
            })

    return follows

def generate_deterministic_orders(users, articles) -> list[dict]:
    """Logique métier des achats par groupe"""
    orders = []
    p1, p2, p3 = articles[0], articles[1], articles[2]

    # 1. Followers de U1 (U1..U5) ont tous acheté P1
    for i in range(0, 5):
        orders.append({
            "id": get_deterministic_uuid(f"order_c1_p1_{i}"),
            "userId": users[i]["id"], "articleId": p1["id"],
            "quantity": 1, "totalPrice": p1["price"]
        })

    # 2. Utilisateurs U6 à U10 ont acheté P1 et P2
    for i in range(5, 10):
        # Achat P1
        orders.append({
            "id": get_deterministic_uuid(f"order_u6_10_p1_{i}"),
            "userId": users[i]["id"], "articleId": p1["id"],
            "quantity": 1, "totalPrice": p1["price"]
        })
        # Achat P2
        orders.append({
            "id": get_deterministic_uuid(f"order_u6_10_p2_{i}"),
            "userId": users[i]["id"], "articleId": p2["id"],
            "quantity": 1, "totalPrice": p2["price"]
        })

    # 3. Utilisateurs U11 à U15 ont acheté P3 exclusivement
    for i in range(10, 15):
        orders.append({
            "id": get_deterministic_uuid(f"order_u11_15_p3_{i}"),
            "userId": users[i]["id"], "articleId": p3["id"],
            "quantity": 1, "totalPrice": p3["price"]
        })

    return orders

async def main():
    print("Seeding déterministe en cours...")
    users = generate_deterministic_users()
    articles = generate_deterministic_articles()
    follows = generate_deterministic_graph(users)
    orders = generate_deterministic_orders(users, articles)
    
    async with aiohttp.ClientSession(headers={'Content-Type': 'application/json'}) as session:
        try:
            await post_bulk(session, "articles", articles, "Both")
            await post_bulk(session, "users", users, "Both")
            await post_bulk(session, "social-graph", follows, "Both")
            await post_bulk(session, "orders", orders, "Both")
            print("\nSetup terminé avec succès.")
        except Exception as e:
            print(f"Erreur: {e}")

if __name__ == "__main__":
    asyncio.run(main())