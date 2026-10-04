# Diagramme de classes — Gestion Poulet

Modèle métier d'après `src/Poulet.Domain/Entities.cs` et les relations configurées dans `src/Poulet.Infrastructure/AppDbContext.cs`.
Les propriétés métier principales sont représentées ; les clés étrangères sont exprimées par les associations et les collections par les compositions.
Les attributs `Notes` sont omis pour alléger la lecture.

```mermaid
classDiagram
    direction TB

    class Entity {
        <<abstract>>
        +int Id
    }
    class Supplier {
        +string Name
        +string Phone
    }
    class Client {
        +string Name
        +string Phone
        +string Type
    }
    class Chamber {
        +string Name
        +decimal Capacity
        +bool IsSouk
    }
    class Purchase {
        +DateOnly Date
        +decimal Quantity
        +decimal UnitPrice
        +string ChickenType
        +decimal DepartureWeight
        +decimal ActualWeight
    }
    class Allocation {
        +decimal Quantity
    }
    class Feed {
        +DateOnly Date
        +decimal Quantity
        +decimal Cost
    }
    class Sale {
        +DateOnly Date
        +string Type
        +string ClientName
        +string ChickenType
        +string Mode
        +decimal Quantity
        +decimal UnitPrice
        +int Pieces
        +decimal CrateCost
        +decimal CostOfGoods
    }
    class SaleAllocation {
        +decimal Quantity
    }
    class MondayLine {
        +string ClientName
        +decimal Quantity
        +decimal UnitPrice
        +bool Paid
        +string Mode
    }
    class ChickenPiece {
        +string Number
    }
    class User {
        +string Username
        +string PasswordHash
        +string Role
    }

    Entity <|-- Supplier
    Entity <|-- Client
    Entity <|-- Chamber
    Entity <|-- Purchase
    Entity <|-- Allocation
    Entity <|-- Feed
    Entity <|-- Sale
    Entity <|-- SaleAllocation
    Entity <|-- MondayLine
    Entity <|-- ChickenPiece
    Entity <|-- User

    Supplier "0..1" -- "0..*" Purchase : fournisseur
    Purchase "1" *-- "0..*" Allocation : repartition
    Chamber "0..1" -- "0..*" Allocation : destination
    Client "0..1" -- "0..*" Allocation : destination
    Chamber "1" -- "0..*" Feed : alimentation
    Client "0..1" -- "0..*" Sale : acheteur
    Sale "1" *-- "0..*" SaleAllocation : prelevement
    Chamber "1" -- "0..*" SaleAllocation : origine
    Sale "1" *-- "0..*" MondayLine : lignes du lundi
    MondayLine "1" *-- "0..*" ChickenPiece : pieces numerotees
    Client "0..1" -- "0..*" User : compte associe
```

- `Entity` est la classe abstraite commune : toutes les entités héritent de `Id`.
- `0..1` indique une référence facultative ; `1` une référence obligatoire ; `0..*` zéro ou plusieurs éléments.
- Le losange plein représente une composition : les allocations, lignes et pièces sont supprimées avec leur parent dans la configuration EF Core.
- `Phone`, `ClientName`, `DepartureWeight` et `ActualWeight` peuvent être nuls dans les classes où ils apparaissent ici.

Ce diagramme couvre le domaine métier ; les contrôleurs, commandes, handlers, interfaces de persistance et composants React ne sont pas représentés.
