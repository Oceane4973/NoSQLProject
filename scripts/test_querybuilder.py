#!/usr/bin/env python3

import asyncio
import aiohttp
import json
import sys
from datetime import datetime
from typing import Dict, Any

BASE_URL = "http://localhost:3001"
API_ENDPOINT = f"{BASE_URL}/api/querybuilder/execute"

async def health_check(session: aiohttp.ClientSession):
    """Vérifie que le serveur est prêt"""
    async with session.get(f"{BASE_URL}/health") as resp:
        status = "OK" if resp.status == 200 else "NO"
        print(f"{status} Serveur healthy: {resp.status}")
        return resp.status == 200

async def post_query(session: aiohttp.ClientSession, name: str, payload: Dict[str, Any]) -> Dict[str, Any]:
    """POST QueryBuilder + pretty print"""
    print(f"\n{name}")
    print(json.dumps(payload, indent=2))
    
    async with session.post(API_ENDPOINT, json=payload) as response:
        if response.status == 200:
            result = await response.json()
            total_pages = result['totalCount'] // result['pageSize'] + 1 if result['totalCount'] > 0 else 1
            print(f"ServerOK - {result['totalCount']} résultats (page {result['page']}/{total_pages})")
            if result['items']:
                print(f"   Premier: {json.dumps(result['items'][0], indent=2)}")
            else:
                print("   Aucun résultat")
            return result
        else:
            error = await response.text()
            print(f"Server{response.status}: {error[:200]}...")
            return {"error": error}

async def get_real_user_id(session: aiohttp.ClientSession) -> str:
    """Récupère un GUID user réel depuis la DB"""
    payload = {"entity": "Users", "pageSize": 1}
    result = await post_query(session, "Récupère 1er user (pour GUID)", payload)
    if result.get('items'):
        user_id = result['items'][0]['id']
        print(f"👤 User ID réel trouvé: {user_id}")
        return user_id
    return "123e4567-e89b-12d3-a456-426614174000"  # Fallback

async def test_articles(session: aiohttp.ClientSession, user_id: str):
    """Articles des followers niveau 4, tri prix DESC"""
    payload = {
        "entity": "Articles",
        "userId": user_id,
        "followingLevel": 4,
        "orderByField": 2,  # ArticlesOrderBy.Price
        "orderDirection": "Descending", 
        "pageSize": 10
    }
    await post_query(session, "Articles (followers niveau 4, prix DESC)", payload)

async def test_users(session: aiohttp.ClientSession):
    """Users avec >5 followers, tri UserName ASC"""
    payload = {
        "entity": "Users",
        "filters": [{
            "field": 3,  # UsersFields.FollowersCount
            "operator": "GreaterThan",
            "value": 5
        }],
        "orderByField": 1,  # UsersOrderBy.UserName
        "orderDirection": "Ascending",
        "pageSize": 5
    }
    await post_query(session, "Users (>5 followers, UserName ASC)", payload)

async def test_orders(session: aiohttp.ClientSession, user_id: str):
    """Orders >100€ des followers niveau 2"""
    payload = {
        "entity": "Orders",
        "userId": user_id,
        "followingLevel": 2,
        "filters": [{
            "field": 4,  # OrdersFields.TotalPrice
            "operator": "GreaterThan",
            "value": 100.0
        }],
        "orderByField": 4,  # OrdersOrderBy.TotalPrice
        "orderDirection": "Descending",
        "pageSize": 10
    }
    await post_query(session, "Orders (>100€, followers niveau 2, prix DESC)", payload)

async def test_complex(session: aiohttp.ClientSession):
    """Articles complexes: prix >50€ ET nom non-vide"""
    payload = {
        "entity": "Articles",
        "filters": [
            {
                "fieldId": 2,  # ArticlesFields.Price
                "operator": "GreaterThan",
                "value": 50.0
            },
            {
                "fieldId": 1,  # ArticlesFields.Name  
                "operator": "Equals",  # ServerExiste dans FilterOperator
                "value": ""  # Filtre nom non-vide dans ApplyArticlesFilter
            }
        ],
        "orderByField": 1,  # ArticlesOrderBy.Name
        "orderDirection": "Ascending",
        "pageSize": 5
    }
    await post_query(session, "Articles (prix>50€ ET nom)", payload)

async def main():
    print(f"ServerTest QueryBuilder API - {datetime.now().strftime('%H:%M:%S')}")
    print(f"Serveur: {BASE_URL}")
    print("=" * 80)

    timeout = aiohttp.ClientTimeout(total=30)
    async with aiohttp.ClientSession(timeout=timeout) as session:
        # 1. Health check
        if not await health_check(session):
            print("Serveur non disponible !")
            return

        # 2. Récupère un GUID user réel
        user_id = await get_real_user_id(session)

        # 3. Tests complets
        await test_articles(session, user_id)
        await test_users(session)
        await test_orders(session, user_id)
        await test_complex(session)

    print("\n" + "=" * 80)
    print("Test QueryBuilder terminé !")

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nTest interrompu")
        sys.exit(0)
    except Exception as e:
        print(f"\nErreur: {e}")
        sys.exit(1)
