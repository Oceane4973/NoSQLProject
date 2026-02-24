import requests
from uuid import uuid5, NAMESPACE_DNS

# Configuration
URL = "http://localhost:3001/api/QueryBuilder/execute?targets=Both"
HEADERS = {'Content-Type': 'application/json'}

def get_id(name: str):
    return str(uuid5(NAMESPACE_DNS, name))

def log_test_header(role, formula):
    print(f"\n{'='*60}")
    print(f"ROLE : {role}")
    print(f"FORMULE : {formula}")
    print(f"{'='*60}")

def validate_results(test_name, role, formula, payload, expected_count, expected_values=None):
    log_test_header(role, formula)
    print(f"TEST : {test_name}")
    
    response = requests.post(URL, json=payload, headers=HEADERS)
    if response.status_code != 200:
        print(f"Erreur API : {response.status_code}")
        return

    results = response.json()

    pg_res, neo_res = results[0], results[1]

    counts_ok = (pg_res['totalCount'] == neo_res['totalCount'] == expected_count)
    status = "OK" if counts_ok else "NO"
    print(f"{status} Count - Attendu: {expected_count} | PG: {pg_res['totalCount']} | Neo: {neo_res['totalCount']}")

    if expected_values and pg_res['items'] and neo_res['items']:
        sample_pg = pg_res['items'][0]
        sample_neo = neo_res['items'][0]
        
        if isinstance(sample_neo, dict) and 'user' in sample_neo:
            sample_neo = sample_neo['user']
        
        for key, expected_val in expected_values.items():
            def get_val(data, k):
                k_lower = k.lower()
                for d_key, d_val in data.items():
                    if d_key.lower() == k_lower:
                        return d_val
                return None

            pg_val = get_val(sample_pg, key)
            neo_val = get_val(sample_neo, key)
            
            if str(pg_val) == str(expected_val) and str(neo_val) == str(expected_val):
                print(f"   Valeur '{key}' correcte : {expected_val}")
            else:
                print(f"   Erreur '{key}' - Attendu: {expected_val} | PG: {pg_val} | Neo: {neo_val}")

# --- BATTERIE DE TESTS ---

# 1. Validation de la Population
validate_results(
    "Vérification de la base Utilisateurs",
    "Vérifier que les 30 utilisateurs (U1-U30) sont créés.",
    "|U| = 30",
    {"entity": "Users", "pageSize": 50},
    expected_count=30
)

# 2. Test de Popularité de P1 (Produit Viral)
# Formule : C1 achète P1 (5) + U6-U10 achètent P1 (5) = 10
validate_results(
    "Popularité de P1 (Produit Viral)",
    "P1 est acheté par C1 et par le groupe U6-U10.",
    "Count(Orders WHERE ArticleId = P1) = |C1| + |{U6..U10}| = 5 + 5 = 10",
    {
        "entity": "Orders",
        "filters": [{"fieldId": 2, "operator": "Equals", "value": get_id("product_1")}]
    },
    expected_count=10
)

# 3. Test de Popularité de P2 (Produit Populaire)
# Formule : U6-U10 achètent P2 (5)
validate_results(
    "Ventes de P2 (Produit Populaire)",
    "P2 est acheté exclusivement par le groupe U6 à U10.",
    "Count(Orders WHERE ArticleId = P2) = |{U6..U10}| = 5",
    {
        "entity": "Orders",
        "filters": [{"fieldId": 2, "operator": "Equals", "value": get_id("product_2")}]
    },
    expected_count=5
)

# 4. Test du Produit Classique P3 (Achats en profondeur)
# Formule : U11-U15 achètent P3 (5)
validate_results(
    "Ventes de P3 (Acheteurs profonds)",
    "P3 est acheté par les utilisateurs du cercle C2 (U11 à U15).",
    "Count(Orders WHERE ArticleId = P3) = |{U11..U15}| = 5",
    {
        "entity": "Orders",
        "filters": [{"fieldId": 2, "operator": "Equals", "value": get_id("product_3")}]
    },
    expected_count=5
)

# 5. Vérification des produits non vendus (P4-P8)
validate_results(
    "Produits invendus (P4-P8)",
    "Vérifier qu'un produit du groupe 'Divers' n'a aucune vente.",
    "Count(Orders WHERE ArticleId = P4) = 0",
    {
        "entity": "Orders",
        "filters": [{"fieldId": 2, "operator": "Equals", "value": get_id("product_4")}]
    },
    expected_count=0
)

# 6. Test de structure Sociale : Followers de U1 (C1)
# U1 est suivit par U1, U2, U3, U4, U5
validate_results(
    "Cercle C1 de U1",
    "U1 est suivi par C1 (U2-U5)",
    "FollowersCount(U1) = 4",  
    {
        "entity": "Users", 
        "filters": [
            {
                "fieldId": 0, 
                "operator": "Equals", 
                "value": get_id("user_1")
            }]
    },
    expected_count=1,
    expected_values={"FollowersCount": 4}
)

# 7. Test de Recommandation Sociale (FollowingLevel)
# Articles achetés par ceux qui suivent U1 (C2 = U1-U10)
validate_results(
    "Recommandation Niveau 1 (C1)",
    "Articles(U2 -> Level 1) inclut P1",
    "Articles(U2 -> Level 1)",
    {
        "entity": "Articles", 
        "userId": get_id("user_2"),  # U2 ∈ C1
        "followingLevel": 1
    },
    expected_count=1,
    expected_values={"name": "Produit Viral"}
)