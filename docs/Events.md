# Inventory Management System - Event Definitions

This document defines all domain events in the Inventory Management System for event sourcing implementation.

## Base Event Structure

All events inherit from the base `Event` record:

```csharp
public record Event(string AggregateId)
{
    public DateTime DateTime { get; init; } = DateTime.UtcNow;
    public int Version { get; init; }
}
```

**Properties:**
- `AggregateId` - Unique identifier for the aggregate root
- `DateTime` - Timestamp when the event occurred (defaults to UTC now)
- `Version` - Event version for optimistic concurrency control

---

## Part Events

Parts represent the base units of inventory - individual components or materials.

### PartDefinedEvent
```csharp
public record PartDefinedEvent(string Sku, string Name) : Event(Sku);
```
**Description:** Fired when a new part type is defined in the system. This establishes a new part with zero initial quantity.

**Parameters:**
- `Sku` - Unique stock keeping unit identifier for the part
- `Name` - Human-readable name/description of the part

---

### PartAcquiredEvent
```csharp
public record PartAcquiredEvent(string Sku, int Quantity, string Justification) : Event(Sku);
```
**Description:** Fired when parts are added to inventory, increasing the available quantity. Used for purchases, deliveries, or other inventory increases.

**Parameters:**
- `Sku` - Part identifier
- `Quantity` - Number of parts being added (positive integer)
- `Justification` - Business reason for the acquisition (e.g., "Supplier delivery", "Emergency purchase")

---

### PartConsumedEvent
```csharp
public record PartConsumedEvent(string Sku, int Quantity, string Justification) : Event(Sku);
```
**Description:** Fired when parts are removed from inventory, decreasing the available quantity. Used when parts are used in production or otherwise consumed.

**Parameters:**
- `Sku` - Part identifier
- `Quantity` - Number of parts being consumed (positive integer)
- `Justification` - Business reason for the consumption (e.g., "Used in PROD001 assembly", "Damaged during handling")

---

### PartRecountedEvent
```csharp
public record PartRecountedEvent(string Sku, int Quantity, string Justification) : Event(Sku);
```
**Description:** Fired when a physical inventory count is performed and the system quantity is adjusted to match reality. Sets the absolute quantity regardless of previous value.

**Parameters:**
- `Sku` - Part identifier
- `Quantity` - Actual counted quantity (absolute value, not delta)
- `Justification` - Reason for the recount (e.g., "Monthly inventory audit", "Discrepancy investigation")

---

### PartSourceChangedEvent
```csharp
public record PartSourceChangedEvent(string Sku, string SourceUri, string SourceName) : Event(Sku);
```
**Description:** Fired when the supplier or source information for a part is updated. Used to track where parts can be purchased or obtained.

**Parameters:**
- `Sku` - Part identifier
- `SourceUri` - URL or reference to the supplier's product page or catalog
- `SourceName` - Name of the supplier or vendor

---

## Product Events

Products are assemblies made up of multiple parts in specific quantities.

### ProductDefinedEvent
```csharp
public record ProductDefinedEvent(string Sku, string Name) : Event(Sku);
```
**Description:** Fired when a new product is defined in the system. Creates an empty product that can have parts added to its bill of materials.

**Parameters:**
- `Sku` - Unique product identifier
- `Name` - Human-readable name/description of the product

---

### PartAddedToProductEvent
```csharp
public record PartAddedToProductEvent(string ProductSku, string PartSku, int Quantity) : Event(ProductSku);
```
**Description:** Fired when a part is added to a product's bill of materials. Defines how many of each part are required to build one unit of the product.

**Parameters:**
- `ProductSku` - Product identifier (used as aggregate ID)
- `PartSku` - Part being added to the product
- `Quantity` - Number of this part required per product unit

---

## Entity Events

Entities are physical instantiations of products - actual manufactured items with serial numbers.

### EntityCreatedEvent
```csharp
public record EntityCreatedEvent(string ProductSku, string Serial) : Event($"P{ProductSku}:S{Serial}");
```
**Description:** Fired when a physical entity is created from a product design. This consumes the required parts from inventory and creates a trackable item.

**Parameters:**
- `ProductSku` - The product design this entity is based on
- `Serial` - Unique serial number for this entity
- **Aggregate ID:** Formatted as `P{ProductSku}:S{Serial}` (e.g., "PPROD001:S12345")

---

### EntityLeakTestedEvent
```csharp
public record EntityLeakTestedEvent(string EntityId, bool Passed) : Event(EntityId);
```
**Description:** Fired when a leak test is performed on an entity. Records whether the entity passed or failed the leak test.

**Parameters:**
- `EntityId` - Entity identifier (P{ProductSku}:S{Serial} format)
- `Passed` - True if the entity passed the leak test, false if it failed

---

### EntityEolTestedEvent
```csharp
public record EntityEolTestedEvent(string EntityId, bool Passed) : Event(EntityId);
```
**Description:** Fired when an End-of-Line (EOL) test is performed on an entity. Records whether the entity passed or failed the final production test.

**Parameters:**
- `EntityId` - Entity identifier
- `Passed` - True if the entity passed the EOL test, false if it failed

---

### EntityReworkedEvent
```csharp
public record EntityReworkedEvent(string EntityId, string PartSku, int Quantity, string Justification) : Event(EntityId);
```
**Description:** Fired when an entity requires rework - replacement or addition of parts after initial assembly. This consumes additional parts from inventory.

**Parameters:**
- `EntityId` - Entity being reworked
- `PartSku` - Part being added/replaced during rework
- `Quantity` - Number of parts used in the rework
- `Justification` - Reason for the rework (e.g., "Failed leak test - replaced gasket", "Customer requested upgrade")

---

### EntityScrappedEvent
```csharp
public record EntityScrappedEvent(string EntityId, string Justification) : Event(EntityId);
```
**Description:** Fired when an entity is determined to be beyond repair and is scrapped. The entity becomes unusable and cannot be shipped.

**Parameters:**
- `EntityId` - Entity being scrapped
- `Justification` - Reason for scrapping (e.g., "Multiple test failures", "Irreparable damage")

---

### EntityShippedEvent
```csharp
public record EntityShippedEvent(string EntityId, string ShipmentId) : Event(EntityId);
```
**Description:** Fired when an entity is added to a shipment and physically shipped to a customer. The entity leaves the facility's inventory.

**Parameters:**
- `EntityId` - Entity being shipped
- `ShipmentId` - Shipment this entity is part of

---

### EntityReturnedEvent
```csharp
public record EntityReturnedEvent(string EntityId, string ReturnId) : Event(EntityId);
```
**Description:** Fired when a previously shipped entity is returned by a customer and re-enters the facility's inventory.

**Parameters:**
- `EntityId` - Entity being returned
- `ReturnId` - Return batch this entity is part of

---

## Shipment Events

Shipments group entities of the same product for delivery to customers.

### ShipmentCreatedEvent
```csharp
public record ShipmentCreatedEvent(string ShipmentId, string ProductSku, string Destination) : Event(ShipmentId);
```
**Description:** Fired when a new shipment is created. Establishes an empty shipment that can have entities added to it. All entities in a shipment must be of the same product type.

**Parameters:**
- `ShipmentId` - Unique shipment identifier
- `ProductSku` - Product type for this shipment (all entities must match)
- `Destination` - Customer or location where the shipment will be delivered

---

### EntityAddedToShipmentEvent
```csharp
public record EntityAddedToShipmentEvent(string ShipmentId, string EntityId) : Event(ShipmentId);
```
**Description:** Fired when an entity is added to an existing shipment. The entity must be of the same product type as the shipment.

**Parameters:**
- `ShipmentId` - Shipment receiving the entity (used as aggregate ID)
- `EntityId` - Entity being added to the shipment

---

### ShipmentDispatchedEvent
```csharp
public record ShipmentDispatchedEvent(string ShipmentId, string AdHoc) : Event(ShipmentId);
```
**Description:** Fired when a shipment is dispatched and leaves the facility. After dispatch, no more entities can be added to the shipment.

**Parameters:**
- `ShipmentId` - Shipment being dispatched
- `AdHoc` - Additional shipping information (tracking numbers, carrier details, special instructions)

---

## Return Events

Returns group entities of the same product that are being returned from customers.

### ReturnCreatedEvent
```csharp
public record ReturnCreatedEvent(string ReturnId, string ProductSku, string Source) : Event(ReturnId);
```
**Description:** Fired when a new return batch is created. Establishes an empty return that can have entities added to it. All entities in a return must be of the same product type.

**Parameters:**
- `ReturnId` - Unique return batch identifier
- `ProductSku` - Product type for this return (all entities must match)
- `Source` - Customer or location where the entities are being returned from

---

### EntityAddedToReturnEvent
```csharp
public record EntityAddedToReturnEvent(string ReturnId, string EntityId) : Event(ReturnId);
```
**Description:** Fired when a returned entity is added to an existing return batch. The entity must be of the same product type as the return batch.

**Parameters:**
- `ReturnId` - Return batch receiving the entity (used as aggregate ID)
- `EntityId` - Entity being added to the return batch