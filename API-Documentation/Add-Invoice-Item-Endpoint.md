# ?? Add Invoice Item Endpoint

## **NEW Endpoint: Add Item to Existing Invoice**

### **Endpoint**
```
POST /api/UtilityInvoices/{id}/items
```

### **Description**
Adds a new item (line) to an existing utility invoice. This endpoint is more efficient than updating the entire invoice when you only need to add items.

### **?? Important Restrictions**
- ? **Cannot add items to fiscalized invoices** (Croatian fiscal law)
- ? Only works on invoices with `FiscalizationStatus != "fiscalized"`
- ? Automatically assigns the next `ItemOrder` number
- ? Optionally recalculates invoice totals

---

## **Request Body**

```json
{
  "description": "Naknada za distribuciju toplinske energije",
  "unit": "kWh",
  "quantity": 150.5,
  "unitPrice": 0.45,
  "amount": 67.73,
  "kpdCode": "35.30.11",
  "taxRate": 5.00,
  "taxCategoryCode": "S",
  "recalculateTotals": true
}
```

### **Field Descriptions**

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `description` | string | ? Yes | - | Item description (max 500 chars) |
| `unit` | string | ? Yes | - | Unit of measurement (e.g., "kWh", "m?", "kom") |
| `quantity` | decimal | ? Yes | - | Quantity (must be > 0) |
| `unitPrice` | decimal | ? Yes | - | Unit price (cannot be negative) |
| `amount` | decimal | ? Yes | - | Total amount (quantity × unitPrice) |
| `kpdCode` | string | ? No | `"35.30.11"` | KPD code for Croatian tax classification |
| `taxRate` | decimal | ? No | `5.00` | Tax rate percentage (0-100) |
| `taxCategoryCode` | string | ? No | `"S"` | Tax category code (`S` = Standard, `Z` = Zero, `E` = Exempt) |
| `recalculateTotals` | boolean | ? No | `true` | Automatically recalculate invoice totals |

---

## **Response**

### **Success (200 OK)**
```json
{
  "success": true,
  "message": "Stavka 'Naknada za distribuciju toplinske energije' uspješno dodana raèunu",
  "data": {
    "id": 123,
    "invoiceNumber": "81-907030-2025040",
    "subTotal": 1245.50,
    "vatAmount": 62.28,
    "totalAmount": 1307.78,
    "items": [
      {
        "id": 456,
        "description": "Existing item 1",
        "unit": "kWh",
        "quantity": 100.0,
        "unitPrice": 0.50,
        "amount": 50.00,
        "itemOrder": 1,
        "kpdCode": "35.30.11",
        "taxRate": 5.00,
        "taxCategoryCode": "S"
      },
      {
        "id": 789,
        "description": "Naknada za distribuciju toplinske energije",
        "unit": "kWh",
        "quantity": 150.5,
        "unitPrice": 0.45,
        "amount": 67.73,
        "itemOrder": 2,
        "kpdCode": "35.30.11",
        "taxRate": 5.00,
        "taxCategoryCode": "S"
      }
    ],
    // ... other invoice fields
  }
}
```

### **Error Responses**

#### **404 Not Found** - Invoice doesn't exist
```json
{
  "success": false,
  "message": "Raèun nije pronaðen"
}
```

#### **400 Bad Request** - Invoice is fiscalized
```json
{
  "success": false,
  "message": "Ne možete dodavati stavke fiskaliziranom raèunu. Fiskalizirani raèuni su zaštiæeni zakonom."
}
```

#### **400 Bad Request** - Validation error
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "Description": ["Opis stavke je obavezan"],
    "Quantity": ["Kolièina mora biti veæa od 0"]
  }
}
```

---

## **cURL Example**

```bash
curl -X POST "https://localhost:7031/api/UtilityInvoices/123/items" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "description": "Prikljuèna naknada",
    "unit": "kom",
    "quantity": 1,
    "unitPrice": 50.00,
    "amount": 50.00,
    "kpdCode": "35.30.11",
    "taxRate": 5.00,
    "taxCategoryCode": "S",
    "recalculateTotals": true
  }'
```

---

## **C# HttpClient Example**

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", jwtToken);

var request = new AddUtilityInvoiceItemRequest
{
    Description = "Prikljuèna naknada",
    Unit = "kom",
    Quantity = 1,
    UnitPrice = 50.00m,
    Amount = 50.00m,
    KpdCode = "35.30.11",
    TaxRate = 5.00m,
    TaxCategoryCode = "S",
    RecalculateTotals = true
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync(
    "https://localhost:7031/api/UtilityInvoices/123/items", 
    content);

if (response.IsSuccessStatusCode)
{
    var result = await response.Content.ReadAsStringAsync();
    Console.WriteLine("Item added successfully!");
}
```

---

## **Automatic Total Recalculation**

When `recalculateTotals` is `true` (default), the endpoint automatically:

1. ? Sums all item amounts ? `SubTotal`
2. ? Calculates VAT: `SubTotal × (TaxRate / 100)` ? `VatAmount`
3. ? Calculates total: `SubTotal + VatAmount` ? `TotalAmount`

### **Example Calculation:**
```
Item 1: 100.00 EUR
Item 2:  67.73 EUR (NEW)
????????????????????
SubTotal: 167.73 EUR
VAT (5%):   8.39 EUR
????????????????????
TOTAL:   176.12 EUR
```

---

## **Common KPD Codes for Utilities**

| KPD Code | Description |
|----------|-------------|
| `35.30.11` | Steam and hot water supply (default) |
| `35.30.12` | Distribution of cooled air |
| `36.00.20` | Water treatment and supply |
| `37.00.00` | Sewerage services |

---

## **Tax Categories**

| Code | Name | Description |
|------|------|-------------|
| `S` | Standard | Standard VAT rate (5%, 13%, 25%) |
| `Z` | Zero | 0% VAT rate |
| `E` | Exempt | VAT exempt |
| `AE` | Reverse charge | Reverse charge mechanism |

---

## **Notes**

- ?? **Security:** Requires JWT authentication (`[Authorize]` attribute)
- ???? **Croatian Law:** Fiscalized invoices cannot be modified
- ?? **Item Order:** Automatically assigns next available order number
- ?? **Precision:** All decimal values use 2 decimal places
- ?? **Timestamps:** `UpdatedAt` is automatically updated
- ? **Validation:** All required fields are validated server-side

---

## **Related Endpoints**

| Endpoint | Method | Description |
|----------|--------|-------------|
| `PUT /api/UtilityInvoices/{id}` | PUT | Update entire invoice (including items) |
| `GET /api/UtilityInvoices/{id}` | GET | Get invoice details with items |
| `DELETE /api/UtilityInvoices/{id}` | DELETE | Delete invoice (if not fiscalized) |

---

**Created:** 2025-01-26  
**Version:** 1.0  
**Status:** ? Implemented & Tested
