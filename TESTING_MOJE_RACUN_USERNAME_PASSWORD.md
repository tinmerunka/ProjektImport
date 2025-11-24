# ?? Quick Start: Testing mojE-Raèun with Demo Credentials

## ? **Implementation Complete - Real API Integration!**

The system now uses the **actual mojE-Raèun API** format and is ready for testing with demo credentials!

---

## ?? **mojE-Raèun API Details**

### **Base URLs:**
- **Demo/Test:** `https://demo.moj-eracun.hr/apis/v2`
- **Production:** `https://api.moj-eracun.hr/apis/v2`

### **Endpoints:**
- **Ping (Test):** `GET /Ping/`
- **Send Invoice:** `POST /OutgoingInvoices/Send`
- **Check Status:** `GET /OutgoingInvoices/Status/{electronicId}`

### **Required Credentials:**
1. **Username** (e.g., `1083`)
2. **Password** (e.g., `test123`)
3. **CompanyId** (your company OIB, e.g., `99999999927`)
4. **SoftwareId** (e.g., `Test-001`)

---

## ?? **Step 1: Configure Your Demo Credentials**

### **API Call:**

```http
PUT /api/utility/UtilityCompany/{companyId}/moje-racun
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "enabled": true,
  "environment": "test",
  "username": "1083",
  "password": "test123",
  "softwareId": "Test-001"
}
```

### **Example with curl:**

```bash
curl -X PUT "https://localhost:7001/api/utility/UtilityCompany/1/moje-racun" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "enabled": true,
    "environment": "test",
    "username": "YOUR_USERNAME",
    "password": "YOUR_PASSWORD",
    "softwareId": "YOUR_SOFTWARE_ID"
  }'
```

### **Expected Response:**

```json
{
  "success": true,
  "message": "mojE-Raèun configuration updated successfully",
  "data": {
    "enabled": true,
    "environment": "test",
    "username": "1083",
    "softwareId": "Test-001",
    "hasPassword": true
  }
}
```

---

## ?? **Step 2: Test Your Connection (Ping)**

Verify the API is accessible before submitting invoices.

### **API Call:**

```http
POST /api/UtilityFiscalization/test-moje-racun
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "companyId": 1
}
```

### **Example with curl:**

```bash
curl -X POST "https://localhost:7001/api/UtilityFiscalization/test-moje-racun" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"companyId": 1}'
```

### **Expected Success Response:**

```json
{
  "success": true,
  "message": "Successfully connected to mojE-Raèun (test environment). Service is up",
  "data": {
    "companyId": 1,
    "companyName": "Your Company",
    "environment": "test",
    "username": "1083",
    "connectionStatus": "connected",
    "testedAt": "2025-11-24T12:00:00Z"
  }
}
```

---

## ?? **Step 3: Check Available Methods**

Verify mojE-Raèun is configured and ready.

### **API Call:**

```http
GET /api/UtilityFiscalization/methods?companyId=1
Authorization: Bearer YOUR_JWT_TOKEN
```

### **Expected Response:**

```json
{
  "success": true,
  "message": "Found 2 available fiscalization method(s)",
  "data": {
    "companyId": 1,
    "companyName": "Your Company",
    "availableMethods": [
      {
        "id": "fina",
        "name": "Fiskalizacija 1.0 (FINA/CIS)",
        "available": true,
        "configured": true
      },
      {
        "id": "moje-racun",
        "name": "mojE-Raèun 2.0",
        "available": true,
        "configured": true,
        "environment": "test",
        "authMethod": "username/password"
      }
    ]
  }
}
```

---

## ?? **Step 4: Import Test CSV & Fiscalize Invoice**

### **4a. Import Your CSV File**

```http
POST /api/utility/import/upload
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: multipart/form-data

file: your-utility-invoices.csv
```

### **4b. Get Imported Invoice ID**

```http
GET /api/UtilityInvoices/imported-invoices
Authorization: Bearer YOUR_JWT_TOKEN
```

Response will include invoice IDs with `fiscalizationStatus: "not_required"`.

### **4c. Fiscalize with mojE-Raèun**

```http
POST /api/UtilityFiscalization/{invoiceId}/fiscalize-moje-racun
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "companyId": 1
}
```

### **Example:**

```bash
curl -X POST "https://localhost:7001/api/UtilityFiscalization/123/fiscalize-moje-racun" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"companyId": 1}'
```

### **Expected Success Response:**

```json
{
  "success": true,
  "message": "Raèun uspješno poslan u mojE-Raèun sustav (Status: Sent)",
  "data": {
    "invoiceId": 123,
    "method": "moje-racun",
    "fiscalizationStatus": "fiscalized",
    "mojeRacunInvoiceId": "394167",
    "qrCodeUrl": "https://moj-eracun.hr/invoice/394167",
    "pdfUrl": "https://moj-eracun.hr/pdf/394167",
    "status": "sent",
    "submittedAt": "2025-11-24T12:00:00Z"
  }
}
```

---

## ?? **What Happens Behind the Scenes**

### **1. XML Generation**
System generates UBL 2.1 XML with your invoice data:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">
  <cbc:ID>Your-Invoice-Number</cbc:ID>
  <cbc:IssueDate>2025-11-24</cbc:IssueDate>
  <!-- ... full UBL 2.1 structure with KPD codes, 5% VAT, etc. ... -->
</Invoice>
```

### **2. XML Wrapping**
XML is wrapped in `OutgoingInvoicesData` envelope:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<OutgoingInvoicesData>
  <Invoice xmlns="...">
    <!-- UBL content here -->
  </Invoice>
</OutgoingInvoicesData>
```

### **3. API Request**
Sends JSON request to mojE-Raèun:
```json
{
  "Username": "1083",
  "Password": "test123",
  "CompanyId": "99999999927",
  "CompanyBu": "",
  "SoftwareId": "Test-001",
  "File": "<?xml version=\"1.0\"?>...full XML..."
}
```

### **4. mojE-Raèun Response**
Receives invoice details:
```json
{
  "ElectronicId": 394167,
  "DocumentNr": "20156256",
  "StatusId": 30,
  "StatusName": "Sent",
  "Created": "2025-11-24T12:00:00+02:00",
  "Sent": "2025-11-24T12:00:05+02:00"
}
```

### **5. Database Update**
Stores all data in `UtilityInvoices` table:
- `FiscalizationMethod` = "moje-racun"
- `FiscalizationStatus` = "fiscalized"
- `MojeRacunInvoiceId` = "394167"
- `MojeRacunStatus` = "sent"

---

## ??? **API Endpoints Summary**

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/utility/UtilityCompany/{id}/moje-racun` | PUT | Configure credentials |
| `/api/utility/UtilityCompany/{id}/moje-racun` | GET | Check configuration |
| `/api/UtilityFiscalization/test-moje-racun` | POST | Test connection (Ping) |
| `/api/UtilityFiscalization/methods` | GET | List available methods |
| `/api/UtilityFiscalization/{id}/fiscalize-moje-racun` | POST | Fiscalize invoice |

---

## ?? **Demo API Information**

### **Demo Endpoint:**
```
https://demo.moj-eracun.hr/apis/v2
```

### **Test Ping:**
```bash
curl https://demo.moj-eracun.hr/apis/v2/Ping/
```

Expected response:
```json
{
  "Status": "ok",
  "Message": "Service is up"
}
```

---

## ?? **Troubleshooting**

### **Problem: "SoftwareId is not configured"**

**Solution:** Make sure you included `softwareId` in your configuration:
```json
{
  "username": "1083",
  "password": "test123",
  "softwareId": "Test-001"  // ? Make sure this is included!
}
```

### **Problem: "401 Unauthorized"**

**Possible causes:**
- Wrong username/password
- Credentials don't match environment (test vs production)

**Solution:**
1. Double-check your demo credentials
2. Ensure environment is set to "test"
3. Try the Ping endpoint first

### **Problem: "XML validation error"**

**Possible causes:**
- Missing required fields in invoice
- Invalid OIB format
- Currency issues

**Solution:**
1. Check generated XML in `wwwroot/FiscalizationDebug` folder
2. Verify all required invoice fields are populated
3. Ensure customer OIB is 11 digits (if provided)

### **Problem: "No response from mojE-Raèun"**

**Solution:**
1. Check if demo.moj-eracun.hr is accessible
2. Verify your network allows HTTPS outbound connections
3. Check application logs for HTTP errors

---

## ?? **Debug Files**

After each fiscalization attempt, XML files are saved to:
```
wwwroot/FiscalizationDebug/MojeRacun_UBL_Wrapped_[InvoiceNumber]_[Timestamp].xml
```

Review these files to:
- Verify XML structure
- Check KPD codes are correct
- Confirm 5% VAT rate is applied
- Debug any API errors

---

## ?? **Complete Testing Workflow**

1. ? Configure demo credentials (username, password, softwareId)
2. ? Test connection with Ping endpoint
3. ? Import CSV file with utility invoices
4. ? Verify invoices have 5% PDV and KPD codes assigned
5. ? Fiscalize one test invoice with mojE-Raèun
6. ? Check database for mojE-Raèun fields populated
7. ? Review generated XML in FiscalizationDebug folder
8. ? Verify invoice shows in your mojE-Raèun demo portal

---

## ?? **Important Notes**

1. **Demo Environment:**
   - Use `environment: "test"` with demo credentials
   - Demo data won't appear in production portal
   - Demo API may have rate limits

2. **Security:**
   - Credentials stored in PLAIN TEXT in database
   - **MUST implement encryption for production!**
   - See `MOJE_RACUN_AUTHENTICATION_GUIDE.md` for encryption guide

3. **VAT Rate:**
   - System defaults to **5% PDV** for utilities
   - Waste/maintenance services use 25%
   - Can be customized per item if needed

4. **Company OIB:**
   - Used as `CompanyId` in API request
   - Must match your mojE-Raèun registration
   - Format: 11-digit number

---

## ?? **Support Resources**

- **mojE-Raèun Demo Portal:** https://demo.moj-eracun.hr
- **Production Portal:** https://moj-eracun.hr
- **API Documentation:** https://docs.moj-eracun.hr/api (if available)
- **Technical Support:** Contact mojE-Raèun support for API access

---

## ?? **Next Steps After Successful Testing**

1. Test with multiple invoices
2. Verify different service types (heating, electricity, water)
3. Test error handling (invalid data, network errors)
4. **Implement password encryption** (critical for production!)
5. Request production credentials
6. Switch `environment` to "production"
7. Build React frontend for dual method selection

---

**You're all set with real API integration! Just add your demo credentials and test.** ??

**Note:** The demo API uses the exact same structure as production, so once testing is complete, you only need to change the environment and credentials to go live!
