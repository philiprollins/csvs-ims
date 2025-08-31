# Inventory Management System API Specification

This document provides a comprehensive list of all API endpoints required for the front-end to operate the Inventory Management System.

## Response Format

All API responses follow this standard format:

### Success Response

```json
{
  "data": {
    // Response data specific to the endpoint
  },
  "meta": {
    // Pagination information (if applicable)
    "page": 1,
    "pageSize": 20,
    "totalItems": 100,
    "totalPages": 5
  }
}
```

### Error Response

```json
{
  "error": {
    "code": "error_code",
    "message": "Description of the error",
    "details": [
      // Validation error details (if applicable)
    ]
  }
}
```

## Error Codes

- `server_error` - Internal server error
- `validation_error` - Request data failed validation
- `not_found` - Requested resource not found
- `duplicate_sku` - A part or product with the same SKU already exists
- `insufficient_inventory` - Not enough parts available for the operation
- `invalid_state` - Operation not allowed in current state

---

## Parts Management

### 1. Get All Parts

**Endpoint:** `GET /parts`

**Query Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 20) - Number of items per page

**Response Example:**
```json
{
  "data": {
    "items": [
      {
        "sku": "PART001",
        "name": "CPU Intel i7",
        "quantity": 25,
        "sourceName": "Intel",
        "sourceUri": "https://intel.com/i7"
      }
    ]
  },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 2. Get Part by SKU

**Endpoint:** `GET /parts/{sku}`

**Path Parameters:**
- `sku` - The unique SKU (Stock Keeping Unit) of the part

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 25,
    "sourceName": "Intel",
    "sourceUri": "https://intel.com/i7",
    "transactions": [
      {
        "type": "ACQUIRED",
        "quantity": 30,
        "justification": "Initial stock purchase",
        "timestamp": "2025-04-20T10:00:00.000Z"
      },
      {
        "type": "CONSUMED", 
        "quantity": -5,
        "justification": "Used in PROD001 assembly",
        "timestamp": "2025-04-20T14:30:00.000Z"
      }
    ]
  }
}
```

### 3. Create Part

**Endpoint:** `POST /parts`

**Request Body:**
```json
{
  "sku": "PART001",
  "name": "CPU Intel i7"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 0,
    "sourceName": null,
    "sourceUri": null
  }
}
```

### 4. Acquire Parts (Add Inventory)

**Endpoint:** `POST /parts/{sku}/acquire`

**Path Parameters:**
- `sku` - The unique SKU of the part

**Request Body:**
```json
{
  "quantity": 50,
  "justification": "Supplier shipment received"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 75,
    "adjustment": {
      "type": "ACQUIRED",
      "quantity": 50,
      "justification": "Supplier shipment received",
      "timestamp": "2025-04-20T00:00:00.000Z"
    }
  }
}
```

### 5. Consume Parts (Remove Inventory)

**Endpoint:** `POST /parts/{sku}/consume`

**Path Parameters:**
- `sku` - The unique SKU of the part

**Request Body:**
```json
{
  "quantity": 5,
  "justification": "Used in product assembly"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 70,
    "adjustment": {
      "type": "CONSUMED",
      "quantity": 5,
      "justification": "Used in product assembly",
      "timestamp": "2025-04-20T00:00:00.000Z"
    }
  }
}
```

### 6. Recount Parts (Set Exact Inventory)

**Endpoint:** `POST /parts/{sku}/recount`

**Path Parameters:**
- `sku` - The unique SKU of the part

**Request Body:**
```json
{
  "quantity": 68,
  "justification": "Physical inventory count - found discrepancy"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 68,
    "adjustment": {
      "type": "RECOUNTED",
      "quantity": 68,
      "justification": "Physical inventory count - found discrepancy",
      "timestamp": "2025-04-20T00:00:00.000Z"
    }
  }
}
```

### 7. Update Part Source

**Endpoint:** `PUT /parts/{sku}/source`

**Path Parameters:**
- `sku` - The unique SKU of the part

**Request Body:**
```json
{
  "sourceName": "Intel Corporation",
  "sourceUri": "https://intel.com/products/i7"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PART001",
    "name": "CPU Intel i7",
    "quantity": 68,
    "sourceName": "Intel Corporation",
    "sourceUri": "https://intel.com/products/i7"
  }
}
```

---

## Products Management

### 1. Get All Products

**Endpoint:** `GET /products`

**Query Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 20) - Number of items per page

**Response Example:**
```json
{
  "data": {
    "items": [
      {
        "sku": "PROD001",
        "name": "Basic Desktop Computer",
        "partCount": 5
      }
    ]
  },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 2. Get Product by SKU

**Endpoint:** `GET /products/{sku}`

**Path Parameters:**
- `sku` - The unique SKU of the product

**Response Example:**
```json
{
  "data": {
    "sku": "PROD001",
    "name": "Basic Desktop Computer",
    "parts": [
      {
        "partSku": "PART001",
        "partName": "CPU Intel i7",
        "quantity": 1
      },
      {
        "partSku": "PART002",
        "partName": "16GB RAM",
        "quantity": 2
      }
    ]
  }
}
```

### 3. Create Product

**Endpoint:** `POST /products`

**Request Body:**
```json
{
  "sku": "PROD001",
  "name": "Basic Desktop Computer"
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PROD001",
    "name": "Basic Desktop Computer",
    "parts": []
  }
}
```

### 4. Add Part to Product

**Endpoint:** `POST /products/{sku}/parts`

**Path Parameters:**
- `sku` - The unique SKU of the product

**Request Body:**
```json
{
  "partSku": "PART001",
  "quantity": 1
}
```

**Response Example:**
```json
{
  "data": {
    "sku": "PROD001",
    "name": "Basic Desktop Computer",
    "part": {
      "partSku": "PART001",
      "partName": "CPU Intel i7",
      "quantity": 1
    }
  }
}
```

### 5. Generate Buildable Products Report

**Endpoint:** `POST /products/buildable-report`

**Request Body:**
```json
{
  "productSkus": ["PROD001", "PROD002"]
}
```

**Response Example:**
```json
{
  "data": {
    "products": [
      {
        "sku": "PROD001",
        "name": "Basic Desktop Computer",
        "buildable": 5,
        "missingParts": []
      },
      {
        "sku": "PROD002",
        "name": "Gaming PC",
        "buildable": 2,
        "parts": [
          {
            "partSku": "PART003",
            "partName": "Graphics Card",
            "requiredQuantity": 1,
            "availableQuantity": 0,
            "buildable": 0
          }
        ]
      }
    ]
  }
}
```

---

## Entities Management

### 1. Get All Entities

**Endpoint:** `GET /entities`

**Query Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 20) - Number of items per page
- `status` (optional) - Filter by entity status
- `productSku` (optional) - Filter by product SKU

**Response Example:**
```json
{
  "data": {
    "items": [
      {
        "id": "PPROD001:S001",
        "productSku": "PROD001",
        "productName": "Basic Desktop Computer",
        "serial": "001",
        "status": "CREATED",
        "leakTestPassed": null,
        "eolTestPassed": null,
        "shipmentId": null,
        "returnId": null,
        "createdAt": "2025-04-20T00:00:00.000Z"
      }
    ]
  },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 2. Get Entity by ID

**Endpoint:** `GET /entities/{entityId}`

**Path Parameters:**
- `entityId` - The unique entity ID (format: P{ProductSku}:S{Serial})

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer", 
    "serial": "001",
    "status": "TESTED",
    "leakTestPassed": true,
    "eolTestPassed": true,
    "reworkHistory": [
      {
        "partSku": "PART002",
        "quantity": 1,
        "justification": "Defective RAM module replaced",
        "timestamp": "2025-04-20T15:30:00.000Z"
      }
    ],
    "shipmentId": null,
    "returnId": null,
    "createdAt": "2025-04-20T00:00:00.000Z"
  }
}
```

### 3. Create Entity

**Endpoint:** `POST /entities`

**Request Body:**
```json
{
  "productSku": "PROD001",
  "serial": "001"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer",
    "serial": "001",
    "status": "CREATED",
    "createdAt": "2025-04-20T00:00:00.000Z"
  }
}
```

### 4. Record Leak Test

**Endpoint:** `POST /entities/{entityId}/leak-test`

**Path Parameters:**
- `entityId` - The unique entity ID

**Request Body:**
```json
{
  "passed": true
}
```

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "leakTestPassed": true,
    "timestamp": "2025-04-20T12:00:00.000Z"
  }
}
```

### 5. Record EOL Test

**Endpoint:** `POST /entities/{entityId}/eol-test`

**Path Parameters:**
- `entityId` - The unique entity ID

**Request Body:**
```json
{
  "passed": false
}
```

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "eolTestPassed": false,
    "timestamp": "2025-04-20T13:00:00.000Z"
  }
}
```

### 6. Record Entity Rework

**Endpoint:** `POST /entities/{entityId}/rework`

**Path Parameters:**
- `entityId` - The unique entity ID

**Request Body:**
```json
{
  "partSku": "PART002",
  "quantity": 1,
  "justification": "Defective RAM module replaced"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "rework": {
      "partSku": "PART002",
      "quantity": 1,
      "justification": "Defective RAM module replaced",
      "timestamp": "2025-04-20T15:30:00.000Z"
    }
  }
}
```

### 7. Scrap Entity

**Endpoint:** `POST /entities/{entityId}/scrap`

**Path Parameters:**
- `entityId` - The unique entity ID

**Request Body:**
```json
{
  "justification": "Failed multiple tests, irreparable"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "PPROD001:S001",
    "status": "SCRAPPED",
    "justification": "Failed multiple tests, irreparable",
    "timestamp": "2025-04-20T16:00:00.000Z"
  }
}
```

---

## Shipments Management

### 1. Get All Shipments

**Endpoint:** `GET /shipments`

**Query Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 20) - Number of items per page
- `status` (optional) - Filter by shipment status (PENDING, DISPATCHED)

**Response Example:**
```json
{
  "data": {
    "items": [
      {
        "id": "SHIP001",
        "productSku": "PROD001",
        "productName": "Basic Desktop Computer",
        "destination": "ABC Electronics, New York",
        "status": "PENDING",
        "entityCount": 2,
        "createdAt": "2025-04-20T00:00:00.000Z"
      }
    ]
  },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 2. Get Shipment by ID

**Endpoint:** `GET /shipments/{id}`

**Path Parameters:**
- `id` - The unique ID of the shipment

**Response Example:**
```json
{
  "data": {
    "id": "SHIP001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer",
    "destination": "ABC Electronics, New York",
    "status": "PENDING",
    "entities": [
      {
        "entityId": "PPROD001:S001",
        "serial": "001",
        "addedAt": "2025-04-20T10:15:00.000Z"
      },
      {
        "entityId": "PPROD001:S002", 
        "serial": "002",
        "addedAt": "2025-04-20T10:16:00.000Z"
      }
    ],
    "adHoc": null,
    "createdAt": "2025-04-20T00:00:00.000Z",
    "dispatchedAt": null
  }
}
```

### 3. Create Shipment

**Endpoint:** `POST /shipments`

**Request Body:**
```json
{
  "id": "SHIP001",
  "productSku": "PROD001",
  "destination": "ABC Electronics, New York"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "SHIP001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer",
    "destination": "ABC Electronics, New York",
    "status": "PENDING",
    "entities": [],
    "createdAt": "2025-04-20T00:00:00.000Z"
  }
}
```

### 4. Add Entity to Shipment

**Endpoint:** `POST /shipments/{id}/entities`

**Path Parameters:**
- `id` - The unique ID of the shipment

**Request Body:**
```json
{
  "entityId": "PPROD001:S001"
}
```

**Response Example:**
```json
{
  "data": {
    "shipmentId": "SHIP001",
    "entityId": "PPROD001:S001",
    "addedAt": "2025-04-20T10:15:00.000Z"
  }
}
```

### 5. Dispatch Shipment

**Endpoint:** `POST /shipments/{id}/dispatch`

**Path Parameters:**
- `id` - The unique ID of the shipment

**Request Body:**
```json
{
  "adHoc": "1Z999AA1234567890"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "SHIP001",
    "status": "DISPATCHED",
    "adHoc": "1Z999AA1234567890",
    "dispatchedAt": "2025-04-20T14:00:00.000Z"
  }
}
```

---

## Returns Management

### 1. Get All Returns

**Endpoint:** `GET /returns`

**Query Parameters:**
- `page` (optional, default: 1) - Page number for pagination
- `pageSize` (optional, default: 20) - Number of items per page

**Response Example:**
```json
{
  "data": {
    "items": [
      {
        "id": "RET001",
        "productSku": "PROD001",
        "productName": "Basic Desktop Computer",
        "source": "ABC Electronics, New York",
        "entityCount": 1,
        "createdAt": "2025-04-21T00:00:00.000Z"
      }
    ]
  },
  "meta": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1
  }
}
```

### 2. Get Return by ID

**Endpoint:** `GET /returns/{id}`

**Path Parameters:**
- `id` - The unique ID of the return

**Response Example:**
```json
{
  "data": {
    "id": "RET001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer",
    "source": "ABC Electronics, New York", 
    "entities": [
      {
        "entityId": "PPROD001:S001",
        "serial": "001",
        "addedAt": "2025-04-21T09:30:00.000Z"
      }
    ],
    "createdAt": "2025-04-21T00:00:00.000Z"
  }
}
```

### 3. Create Return

**Endpoint:** `POST /returns`

**Request Body:**
```json
{
  "id": "RET001",
  "productSku": "PROD001",
  "source": "ABC Electronics, New York"
}
```

**Response Example:**
```json
{
  "data": {
    "id": "RET001",
    "productSku": "PROD001",
    "productName": "Basic Desktop Computer",
    "source": "ABC Electronics, New York",
    "entities": [],
    "createdAt": "2025-04-21T00:00:00.000Z"
  }
}
```

### 4. Add Entity to Return

**Endpoint:** `POST /returns/{id}/entities`

**Path Parameters:**
- `id` - The unique ID of the return

**Request Body:**
```json
{
  "entityId": "PPROD001:S001"
}
```

**Response Example:**
```json
{
  "data": {
    "returnId": "RET001",
    "entityId": "PPROD001:S001", 
    "addedAt": "2025-04-21T09:30:00.000Z"
  }
}
```

---

## Error Handling

All endpoints will return appropriate HTTP status codes:

- `200 OK` - Request was successful
- `201 Created` - Resource was successfully created
- `400 Bad Request` - Request validation failed
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource already exists (e.g., duplicate SKU)
- `422 Unprocessable Entity` - Invalid state for operation
- `500 Internal Server Error` - Server-side error occurred