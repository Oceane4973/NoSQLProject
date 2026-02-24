import requests
from uuid import uuid5, NAMESPACE_DNS

# Configuration
URL = "http://localhost:3001/api/QueryBuilder/execute?targets=Both"
HEADERS = {'Content-Type': 'application/json'}

def get_id(name: str):
    return str(uuid5(NAMESPACE_DNS, name))

def validate_results(test_name, payload, expected_count, expected_values=None):
    print(f"\n{test_name}")
    response = requests.post(URL, json=payload, headers=HEADERS)
    
    if response.status_code != 200:
        print(f"Erreur API : {response.status_code}")
        return

    results = response.json()
    pg_res, neo_res = results[0], results[1]

    # 1. Validation des nombres (Postgres vs Neo4j vs Attendu)
    counts_ok = (pg_res['totalCount'] == neo_res['totalCount'] == expected_count)
    status = "OK" if counts_ok else "NO"
    print(f"{status} Count - Attendu: {expected_count} | PG: {pg_res['totalCount']} | Neo: {neo_res['totalCount']}")

    # 2. Validation des valeurs exactes (Données déterministes)
    if expected_values:
        # On vérifie sur le premier item retourné (souvent trié par ID ou Name)
        sample_pg = pg_res['items'][0]
        sample_neo = neo_res['items'][0]
        
        for key, expected_val in expected_values.items():
            # Normalisation des clés (Postgres souvent PascalCase, Neo4j souvent lowercase)
            pg_val = sample_pg.get(key) or sample_pg.get(key.capitalize())
            neo_val = sample_neo.get(key) or sample_neo.get(key.lower())

            if str(pg_val) == str(expected_val) and str(neo_val) == str(expected_val):
                print(f"   Valeur '{key}' correcte : {expected_val}")
            else:
                print(f"   Erreur '{key}' - Attendu: {expected_val} | PG: {pg_val} | Neo: {neo_val}")

# --- BATTERIE DE TESTS ---

# Test A : Vérification du Produit Viral (P1)
validate_results("Vérification Produit Viral (P1)", {
    "entity": "Articles",
    "filters": [{"fieldId": 0, "operator": "Equals", "value": get_id("product_1")}]
}, expected_count=1, expected_values={
    "name": "Produit Viral",
    "price": 15.5 # (15.5 * 1 dans ton seeder)
})

# Test B : Vérification des Achats du cercle C1 (U1-U5)
# Chaque membre de C1 a acheté P1
validate_results("Vérification Achats Groupe C1 sur P1", {
    "entity": "Orders",
    "filters": [
        {"fieldId": 2, "operator": "Equals", "value": get_id("product_1")}, # ArticleId
        {"fieldId": 1, "operator": "Equals", "value": get_id("user_1")}    # UserId
    ]
}, expected_count=1, expected_values={
    "totalPrice": 15.5,
    "quantity": 1
})

# Test C : Cohérence globale des Orders
# 5 (C1) + 10 (U6-U10) + 5 (U11-U15) = 20
validate_results("Volume total des ventes", {
    "entity": "Orders",
    "pageSize": 50
}, expected_count=20)