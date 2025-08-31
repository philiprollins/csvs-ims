# Event Sourcing Projections and Read Models

This document defines all projections and their corresponding read models for the Inventory Management System. Each projection listens to specific events and maintains denormalized views optimized for querying.

## Parts Management

### PartSummaryProjection
*Maintains current inventory levels and source information for all parts*

```csharp
public class PartSummaryProjection :
    IEventHandler<PartDefinedEvent>,           // Creates new part record
    IEventHandler<PartAcquiredEvent>,          // Increases quantity
    IEventHandler<PartConsumedEvent>,          // Decreases quantity
    IEventHandler<PartRecountedEvent>,         // Sets exact quantity
    IEventHandler<PartSourceChangedEvent>      // Updates supplier information
{ }

public class PartSummaryReadModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 0;
    public string SourceUri { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
}
```

### PartTransactionProjection
*Tracks all inventory movements for audit trail and transaction history*

```csharp
public class PartTransactionProjection :
    IEventHandler<PartAcquiredEvent>,          // Records inventory addition
    IEventHandler<PartConsumedEvent>,          // Records inventory removal
    IEventHandler<PartRecountedEvent>          // Records inventory adjustment
{ }

public class PartTransactionReadModel
{
    public int Id { get; set; }
    public string PartSku { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // ACQUIRED, CONSUMED, RECOUNTED
    public int Quantity { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

---

## Products Management

### ProductSummaryProjection
*Lightweight view for product listings showing basic information*

```csharp
public class ProductSummaryProjection :
    IEventHandler<ProductDefinedEvent>,        // Creates product record
    IEventHandler<PartAddedToProductEvent>     // Increments part count
{ }

public class ProductSummaryReadModel
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int PartCount { get; set; } = 0;
}
```

### ProductCompositionProjection
*Detailed view showing complete bill of materials for products*

```csharp
public class ProductCompositionProjection :
    IEventHandler<ProductDefinedEvent>,        // Creates product record
    IEventHandler<PartAddedToProductEvent>     // Adds part to BOM
{ }

public class ProductCompositionReadModel
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public List<ProductPartReadModel> Parts { get; set; } = new();
}

public class ProductPartReadModel
{
    public string PartSku { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
```

---

## Entities Management

### EntitySummaryProjection
*Tracks current state, test results, and location of all manufactured entities*

```csharp
public class EntitySummaryProjection :
    IEventHandler<EntityCreatedEvent>,         // Creates entity record
    IEventHandler<EntityLeakTestedEvent>,      // Updates leak test result
    IEventHandler<EntityEolTestedEvent>,       // Updates EOL test result
    IEventHandler<EntityReworkedEvent>,        // Updates rework status
    IEventHandler<EntityScrappedEvent>,        // Marks as scrapped
    IEventHandler<EntityShippedEvent>,         // Updates shipment info
    IEventHandler<EntityReturnedEvent>         // Updates return info
{ }

public class EntitySummaryReadModel
{
    public string Id { get; set; } = string.Empty; // P{ProductSku}:S{Serial}
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // CREATED, TESTED, SHIPPED, RETURNED, SCRAPPED
    public bool LeakTestPassed { get; set; } = false;
    public bool EolTestPassed { get; set; } = false;
    public string ShipmentId { get; set; } = string.Empty;
    public string ReturnId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### EntityReworkHistoryProjection
*Maintains detailed audit trail of all rework operations performed on entities*

```csharp
public class EntityReworkHistoryProjection :
    IEventHandler<EntityReworkedEvent>         // Records rework operation
{ }

public class EntityReworkHistoryReadModel
{
    public int Id { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
```

---

## Shipments Management

### ShipmentSummaryProjection
*Lightweight view for shipment listings showing status and basic information*

```csharp
public class ShipmentSummaryProjection :
    IEventHandler<ShipmentCreatedEvent>,       // Creates shipment record
    IEventHandler<EntityAddedToShipmentEvent>, // Increments entity count
    IEventHandler<ShipmentDispatchedEvent>,    // Updates status to dispatched
    IEventHandler<EntityShippedEvent>          // Links entities to shipments
{ }

public class ShipmentSummaryReadModel
{
    public string Id { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // PENDING, DISPATCHED
    public int EntityCount { get; set; } = 0;
    public string AdHoc { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### ShipmentDetailProjection
*Detailed view showing all entities included in a shipment*

```csharp
public class ShipmentDetailProjection :
    IEventHandler<ShipmentCreatedEvent>,       // Creates shipment record
    IEventHandler<EntityAddedToShipmentEvent>, // Adds entity to shipment
    IEventHandler<ShipmentDispatchedEvent>,    // Updates dispatch info
    IEventHandler<EntityShippedEvent>          // Links entities to shipments
{ }

public class ShipmentDetailReadModel
{
    public string Id { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ShipmentEntityReadModel> Entities { get; set; } = new();
    public string AdHoc { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ShipmentEntityReadModel
{
    public string EntityId { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
```

---

## Returns Management

### ReturnSummaryProjection
*Lightweight view for return listings showing basic information*

```csharp
public class ReturnSummaryProjection :
    IEventHandler<ReturnCreatedEvent>,         // Creates return record
    IEventHandler<EntityAddedToReturnEvent>,   // Increments entity count
    IEventHandler<EntityReturnedEvent>         // Links entities to returns
{ }

public class ReturnSummaryReadModel
{
    public string Id { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int EntityCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
}
```

### ReturnDetailProjection
*Detailed view showing all entities included in a return*

```csharp
public class ReturnDetailProjection :
    IEventHandler<ReturnCreatedEvent>,         // Creates return record
    IEventHandler<EntityAddedToReturnEvent>,   // Adds entity to return
    IEventHandler<EntityReturnedEvent>         // Links entities to returns
{ }

public class ReturnDetailReadModel
{
    public string Id { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public List<ReturnEntityReadModel> Entities { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class ReturnEntityReadModel
{
    public string EntityId { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
```

---

## Cross-Aggregate Reports

### BuildableProductsProjection
*Calculates how many of each product can be built based on current part inventory*

```csharp
public class BuildableProductsProjection :
    IEventHandler<PartDefinedEvent>,           // Tracks part availability
    IEventHandler<PartAcquiredEvent>,          // Updates part quantities
    IEventHandler<PartConsumedEvent>,          // Updates part quantities
    IEventHandler<PartRecountedEvent>,         // Updates part quantities
    IEventHandler<ProductDefinedEvent>,        // Tracks products
    IEventHandler<PartAddedToProductEvent>     // Updates product requirements
{ }

public class BuildableProductReadModel
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int BuildableCount { get; set; }
    public List<BuildablePartReadModel> Parts { get; set; } = new();
}

public class BuildablePartReadModel
{
    public string PartSku { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public int RequiredQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int Buildable { get; set; }
}
```

### EntityStatusReportProjection
*Provides dashboard-style reporting on entity status across all products*

```csharp
public class EntityStatusReportProjection :
    IEventHandler<EntityCreatedEvent>,         // Tracks entity creation
    IEventHandler<EntityScrappedEvent>,        // Updates status counts
    IEventHandler<EntityShippedEvent>,         // Updates status counts
    IEventHandler<EntityReturnedEvent>         // Updates status counts
{ }

public class EntityStatusReportReadModel
{
    public int TotalEntities { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public List<ProductEntityBreakdownReadModel> ProductBreakdown { get; set; } = new();
}

public class ProductEntityBreakdownReadModel
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalEntities { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = new();
}
```