# TP NoSQL – Analyse de comportement d’achat (SQL vs NoSQL)

## Objectif du projet

L’objectif de ce projet est de **modéliser, implémenter et tester en volumétrie un service d’analyse du comportement d’achat d’utilisateurs au sein d’un réseau social**, en comparant :

- Une base de données relationnelle **SQL (PostgreSQL)**
- Une base de données **NoSQL**

Le but principal est de **tester les transactions, les performances d’injection et les performances de recherche entre une base SQL et une base NoSQL**, afin d’identifier :

- Les avantages et inconvénients de chaque solution
- Les différences de modélisation
- Les performances en lecture
- Les performances en écriture (injection de données)
- Le comportement en volumétrie importante

> Ce projet ne vise pas la pertinence métier des résultats, mais uniquement le bon fonctionnement technique et les performances des systèmes.

---

## Environnement technique

- **Backend :** .NET (C#)
- **Base relationnelle :** PostgreSQL
- **Base NoSQL :** Neo4j

---

## Description fonctionnelle

Le système simule un **réseau social** où :

- Les utilisateurs peuvent se suivre (relation orientée "follows")
- Un utilisateur peut avoir entre 0 et 20 followers directs
- Sur plusieurs niveaux, un utilisateur peut être son propre follower (gestion des doublons obligatoire)
- La base peut contenir jusqu’à 1 000 000 utilisateurs

### Achats

- 10 000 références produits
- Chaque utilisateur peut commander entre 0 et 5 produits

Les données sont générées automatiquement de manière aléatoire pour tester la volumétrie.

---

## Requêtes à analyser

Le logiciel doit permettre d’exécuter et mesurer les performances des requêtes suivantes sur les deux bases :

- Liste et nombre des produits commandés par les cercles de followers d’un individu (niveau 1 à n)
- Même requête avec filtre sur un produit spécifique
- Pour une référence donnée : nombre de personnes l’ayant commandée dans un cercle orienté de niveau n
- Analyse des produits "viraux"
- Mesure des temps d’injection
- Mesure des temps de recherche

---

## Installation

1. Installer docker
2. CLoner le repo
3. Renommer `.env.example` en `.env`
4. Modifier au besoin des valeur du fichier `.env` : 
```
# Application Environment
ASPNETCORE_ENVIRONMENT=Development

# Postgres
POSTGRES_DB_USER=postgres
POSTGRES_DB_PASSWORD=<YOUR_PASSWORD>
POSTGRES_DB_NAME=nosqlproject

# Neo4j
NEO4J_PASSWORD=<YOUR_PASSWORD>

# Seeder
SEEDER_USER_COUNT=1000
SEEDER_ARTICLE_COUNT=500
SEEDER_ORDER_COUNT=5000
```
6. Ouvrir un terminal et lancer la commande : 
```bash
docker-compose --profile seeder up -d --build
```
> Le profil `seeder` est obligatoire pour lancer le conteneur `seeder` qui permettra de setup les bases de données.