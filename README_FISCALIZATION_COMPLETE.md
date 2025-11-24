# ?? Utility Invoice Fiscalization System - Complete Documentation

## ?? Executive Summary

This system implements **dual fiscalization** for utility invoices in Croatia, supporting both traditional FINA CIS (1.0) and the new mojE-Raèun UBL 2.1 e-invoicing system. The project enables automated CSV import of utility invoices and seamless integration with the Croatian Tax Administration's fiscalization systems.

---

## ?? Table of Contents

1. [What Was Built - Complete Feature List](#what-was-built)
2. [?? CRITICAL: Frontend Implementation Priorities](#-critical-frontend-implementation-priorities)
3. [Backend Architecture Overview](#backend-architecture-overview)
4. [Database Schema Changes](#database-schema-changes)
5. [API Endpoints Reference](#api-endpoints-reference)
6. [Frontend Implementation Roadmap](#frontend-implementation-roadmap)
7. [Testing Environment Setup](#testing-environment-setup)
8. [?? Production Deployment Requirements](#?-production-deployment-requirements)
9. [Known Issues & Current Limitations](#known-issues--current-limitations)
10. [Troubleshooting Guide](#troubleshooting-guide)

---

## ??? What Was Built

### Core Features Implemented

#### 1. **CSV Import System**
- **Purpose**: Import utility invoices from external CSV files
- **Location**: `Controllers/UtilityImportController.cs`
- **Features**:
  - Batch import with unique batch ID tracking
  - Automatic customer matching
  - Invoice item parsing (heating energy, hot water, fees)
  - KPD code assignment (Croatian product classification)
  - Error tracking and validation
  - Import history management

#### 2. **mojE-Raèun Integration**
- **Purpose**: Submit invoices to Croatian Tax Administration's new e-invoicing system
- **Location**: `Services/MojeRacunService.cs`
- **Features**:
  - UBL 2.1 XML generation (EN16931 compliant)
  - Croatian CIUS (Core Invoice Usage Specification) implementation
  - Automatic validation against mojE-Raèun business rules
  - Debug XML logging to `wwwroot/FiscalizationDebug/`
  - Connection testing endpoint
  - Status checking for submitted invoices
  - QR code and PDF URL generation

#### 3. **FINA CIS Integration** (Existing)
- **Purpose**: Traditional cash register fiscalization
- **Location**: `Services/FiscalizationService.cs`
- **Features**:
  - ZKI (security code) generation
  - JIR (unique invoice ID) retrieval
  - XML signing with digital certificates
  - SOAP envelope wrapping

#### 4. **Fiscalization Controller**
- **Purpose**: Unified API for all fiscalization operations
- **Location**: `Controllers/UtilityFiscalizationController.cs`
- **Endpoints**:
  - Test mojE-Raèun connection
  - Fiscalize single invoice
  - Check invoice status
  - Retrieve fiscalization details

#### 5. **Company Management**
- **Purpose**: Store mojE-Raèun and FINA credentials per company
- **Location**: `Controllers/UtilityCompanyController.cs`
- **Features**:
  - mojE-Raèun configuration (username, password, software ID)
  - FINA configuration (certificate, operator OIB)
  - Environment selection (demo/production)
  - Enable/disable fiscalization methods

#### 6. **Invoice Management**
- **Purpose**: CRUD operations for utility invoices
- **Location**: `Controllers/UtilityInvoicesController.cs`
- **Features**:
  - List invoices with filtering
  - View invoice details
  - Track fiscalization status
  - Download invoice data

---

## ?? CRITICAL: Frontend Implementation Priorities

### **PHASE 1: FOUNDATION (Week 1) - MUST DO FIRST! ??**

Without these components, **nothing will work**. These are the absolute minimum to enable fiscalization.

#### Priority 1A: Company Settings Page (DAY 1-2) ?????

**Why First?**: Without mojE-Raèun credentials configured, you cannot fiscalize anything. This is the entry point.

**What You Need to Build**:

```
Component: CompanySettingsPage.tsx (or .jsx)
Location: src/pages/settings/CompanySettings

Required Fields to Display:
??? Basic Info (Read-only display)
?   ??? Company Name
?   ??? OIB
?   ??? Address
?
??? mojE-Raèun Configuration Section ?? CRITICAL
?   ??? Toggle: Enable mojE-Raèun (boolean)
?   ??? Dropdown: Environment (demo/production)
?   ??? Input: Username (MojeRacunClientId)
?   ??? Password: Password (MojeRacunClientSecret)
?   ??? Input: Software ID (MojeRacunApiKey)
?   ??? Button: "Test Connection" ?
?
??? Actions
    ??? Button: "Save Settings"
    ??? Button: "Cancel"

Required API Calls:
??? GET /api/utility/company (load settings)
??? PUT /api/utility/company (save settings)
??? POST /api/utility/fiscalization/test-connection (test)
```

**Critical Test Connection Flow**:
```
1. User fills in credentials
2. User clicks "Test Connection"
3. Frontend calls POST /api/utility/fiscalization/test-connection
4. Backend tries to ping mojE-Raèun
5. Success: Show green checkmark "? Connected successfully"
6. Failure: Show red error "? Connection failed: [reason]"
```

**Current Test Credentials for Demo**:
```
Environment: demo
Username: 8711
Password: test123
Software ID: Test-001
Base URL: https://demo.moj-eracun.hr/apis/v2
```

---

#### Priority 1B: Invoice List Page Enhancement (DAY 3-4) ????

**Why Second?**: Users need to see which invoices need fiscalization and their current status.

**What You Need to Add to Existing Invoice List**:

```
New Columns to Add:
??? Fiscalization Status Badge
?   ??? "Not Fiscalized" (gray) - default
?   ??? "? Fiscalized" (green) - success
?   ??? "? Error" (red) - failed
?   ??? "? Processing" (yellow) - pending
?
??? Fiscalization Method (if fiscalized)
?   ??? "FINA" - traditional
?   ??? "mojE-Raèun" - new system
?
??? Actions Column
    ??? Button: "Fiscalize" (if not fiscalized)
    ??? Icon: "View Details" (if fiscalized)

Filtering Options to Add:
??? Filter by Fiscalization Status
?   ??? All
?   ??? Not Fiscalized
?   ??? Fiscalized
?   ??? Errors
?
??? Filter by Method
    ??? FINA
    ??? mojE-Raèun

Required API Enhancement:
??? GET /api/utility/invoices
??? Add fiscalization fields to response
```

**Status Badge Color Logic**:
```
FiscalizationStatus values:
- "not_required" ? Gray badge "Not Fiscalized"
- "fiscalized" ? Green badge "? Fiscalized"
- "error" ? Red badge "? Error" (with tooltip showing error)
- "pending" ? Yellow badge "? Processing"
```

---

#### Priority 1C: Single Invoice Fiscalization (DAY 5) ????

**Why Third?**: Enable users to fiscalize invoices one at a time for testing.

**What You Need to Build**:

```
Modal/Dialog: FiscalizeInvoiceDialog
Trigger: "Fiscalize" button in invoice list

Dialog Content:
??? Invoice Summary
?   ??? Invoice Number
?   ??? Customer Name
?   ??? Total Amount
?   ??? Issue Date
?
??? Fiscalization Options (if both methods enabled)
?   ??? Radio: Select method (FINA / mojE-Raèun)
?
??? Actions
?   ??? Button: "Fiscalize Now" (primary)
?   ??? Button: "Cancel"
?
??? Progress Indicator
    ??? Spinner during API call
    ??? Success/Error message

API Call:
POST /api/utility/fiscalization/fiscalize/{invoiceId}

Response Handling:
??? Success:
?   ??? Show success message
?   ??? Display ElectronicId (mojE-Raèun ID)
?   ??? Show QR Code link
?   ??? Show PDF download link
?   ??? Refresh invoice list
?
??? Failure:
    ??? Show error message
    ??? Display validation errors
    ??? Keep dialog open for retry
```

---

### **PHASE 2: ENHANCEMENTS (Week 2) ??**

These features improve usability but are not blocking for basic functionality.

#### Priority 2A: Invoice Detail Page

**Add Fiscalization Section**:
```
Component Location: InvoiceDetailPage (existing)

New Section: "Fiscalization Information"
??? If Not Fiscalized:
?   ??? Status badge: "Not Fiscalized"
?   ??? Button: "Fiscalize This Invoice"
?   ??? (If error) Display error message
?
??? If Fiscalized:
    ??? Badge: "? Fiscalized via [METHOD]"
    ??? Fiscalized Date/Time
    ?
    ??? For mojE-Raèun:
    ?   ??? mojE-Raèun ID: [ElectronicId]
    ?   ??? Status: [Obraðen/Poslan/Dostavljen]
    ?   ??? Button: "View QR Code" ? Opens QR URL
    ?   ??? Button: "Download PDF" ? Opens PDF URL
    ?
    ??? For FINA:
        ??? JIR: [unique ID]
        ??? ZKI: [security code]
```

---

#### Priority 2B: Batch Fiscalization

**Purpose**: Fiscalize multiple invoices at once

```
Component: BatchFiscalizationPanel

Features:
??? Checkbox selection in invoice list
??? "Fiscalize Selected" button (top of list)
??? Progress modal showing:
?   ??? Progress bar (X of Y invoices)
?   ??? Current invoice being processed
?   ??? Success count (green)
?   ??? Failed count (red)
?   ??? Button: "Stop" (cancel remaining)
?
??? Results summary:
    ??? "? 45 invoices fiscalized successfully"
    ??? "? 3 invoices failed"
    ??? Button: "View Failed Invoices"

Implementation Note:
- Process invoices sequentially (not parallel)
- Wait 500ms between requests (rate limiting)
- Handle errors gracefully per invoice
- Update UI after each invoice completes
```

---

#### Priority 2C: Dashboard Widgets

**Purpose**: Quick overview of fiscalization status

```
Dashboard Widgets to Add:

1. Fiscalization Stats Card
   ??? Today: X fiscalized
   ??? This Week: X fiscalized
   ??? This Month: X fiscalized
   ??? Pending: X not fiscalized

2. Recent Fiscalizations List
   ??? Last 10 fiscalized invoices
   ??? Invoice number
   ??? Customer name
   ??? Status badge
   ??? Link to invoice detail

3. Errors Alert Card (if any)
   ??? "?? X invoices failed fiscalization"
   ??? List of failed invoice numbers
   ??? Button: "View Details"
```

---

### **PHASE 3: ADVANCED FEATURES (Week 3+) ??**

These are "nice to have" features that can wait until core functionality is solid.

#### Optional Feature A: Real-time Status Updates
- Poll mojE-Raèun API every 10 seconds for invoice status
- Update badge from "Obraðen" ? "Poslan" ? "Dostavljen"
- Show notification when status changes

#### Optional Feature B: Bulk Import + Auto-Fiscalize
- After CSV import, show option "Fiscalize All Imported"
- Automatically process entire batch
- Show progress and results

#### Optional Feature C: Fiscalization Reports
- Generate reports of fiscalized invoices
- Export to Excel/PDF
- Filter by date range, status, method

#### Optional Feature D: QR Code Preview
- Embed QR code image directly in invoice detail
- Allow download as PNG
- Print-friendly invoice view with QR code

---

## ??? Backend Architecture Overview

### Services Layer

#### 1. **MojeRacunService.cs**
**Purpose**: Core integration with mojE-Raèun API

**Key Methods**:
```csharp
// Test connection to mojE-Raèun
Task<MojeRacunResponse> TestConnectionAsync(CompanyProfile company)

// Submit invoice and get back ElectronicId
Task<MojeRacunResponse> SubmitInvoiceAsync(UtilityInvoice invoice, CompanyProfile company)

// Generate UBL 2.1 XML (EN16931 + Croatian CIUS)
string GenerateUblXml(UtilityInvoice invoice, CompanyProfile company)

// Check status of submitted invoice
Task<MojeRacunResponse> CheckInvoiceStatusAsync(string invoiceId, CompanyProfile company)
```

**UBL XML Generation Details**:
- Root element: `<Invoice>` (UBL 2.1 namespace)
- CustomizationID: Croatian CIUS 2025
- ProfileID: P3 (business-to-business)
- All dates: ISO 8601 format (YYYY-MM-DD)
- Currency: EUR (fixed)
- Tax scheme: VAT (PDV in Croatian)

**Critical Business Rules Implemented**:
```
BR-07: Invoice must contain buyer name
HR-BR-S-01: Standard rate invoices require buyer VAT ID
HR-BR-10: Buyer electronic address required
HR-BR-33: No empty XML elements allowed
```

**Current Test Configuration**:
- A1 Hrvatska (OIB: 29524210204) used as default buyer when customer has no OIB
- This is for TESTING ONLY - see Production Requirements section

---

#### 2. **FiscalizationService.cs**
**Purpose**: FINA CIS integration (existing system)

**Key Methods**:
```csharp
// Generate ZKI security code
string GenerateZki(Invoice invoice, CompanyProfile company)

// Submit to FINA and get JIR
Task<FiscalizationResponse> FiscalizeInvoiceAsync(Invoice invoice, CompanyProfile company)
```

---

#### 3. **KpdCodeService.cs**
**Purpose**: Assign Croatian product classification codes

**Default KPD Codes Used**:
```
35.30.11 - Steam and hot water supply
```

---

### Controllers Layer

#### 1. **UtilityFiscalizationController.cs**
**Base Route**: `/api/utility/fiscalization`

**Endpoints**:
```
POST /test-connection
  Request: { companyId: number }
  Response: { success, status, message, rawResponse }

POST /fiscalize/{invoiceId}
  Response: { success, invoiceId, status, qrCodeUrl, pdfUrl, ... }

GET /status/{invoiceId}
  Response: { success, status, message, rawResponse }
```

---

#### 2. **UtilityCompanyController.cs**
**Base Route**: `/api/utility/company`

**Endpoints**:
```
GET /
  Returns company profile with all settings

PUT /
  Updates company settings (including mojE-Raèun credentials)
```

---

#### 3. **UtilityInvoicesController.cs**
**Base Route**: `/api/utility/invoices`

**Endpoints**:
```
GET /
  Returns paginated list of invoices with fiscalization status

GET /{id}
  Returns invoice details including fiscalization info

GET /by-batch/{batchId}
  Returns all invoices from a specific import batch
```

---

#### 4. **UtilityImportController.cs**
**Base Route**: `/api/utility/import`

**Endpoints**:
```
POST /upload
  Accepts CSV file, imports invoices, returns batch ID

GET /batches
  Returns list of import batches with statistics

GET /batch/{batchId}
  Returns batch details and all invoices in batch
```

---

## ?? Database Schema Changes

### New Tables

#### **ImportBatches**
Tracks each CSV import operation.

```sql
CREATE TABLE ImportBatches (
    Id INT PRIMARY KEY IDENTITY,
    BatchId NVARCHAR(36) NOT NULL UNIQUE,  -- GUID
    FileName NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    ImportedAt DATETIME2 NOT NULL,
    ImportedBy NVARCHAR(100),
    TotalRecords INT NOT NULL,
    SuccessfulRecords INT NOT NULL,
    FailedRecords INT NOT NULL,
    SkippedRecords INT NOT NULL,
    ImportDurationMs BIGINT NOT NULL,
    Status NVARCHAR(MAX) NOT NULL,  -- success, partial, failed
    ErrorLog NVARCHAR(MAX)
)
```

---

#### **UtilityInvoices**
Stores imported utility invoices with fiscalization tracking.

```sql
CREATE TABLE UtilityInvoices (
    Id INT PRIMARY KEY IDENTITY,
    
    -- Invoice Basic Info
    InvoiceNumber NVARCHAR(50) NOT NULL,
    IssueDate DATETIME2 NOT NULL,
    DueDate DATETIME2 NOT NULL,
    ValidityDate DATETIME2,
    Period NVARCHAR(50) NOT NULL,
    
    -- Customer Info
    CustomerCode NVARCHAR(20) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    CustomerOib NVARCHAR(20) NOT NULL,  -- May be empty in source data
    CustomerAddress NVARCHAR(200) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    PostalCode NVARCHAR(10) NOT NULL,
    Building NVARCHAR(200) NOT NULL,
    
    -- Financial Info
    SubTotal DECIMAL(18,2) NOT NULL,
    VatAmount DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    
    -- Payment Info
    BankAccount NVARCHAR(50) NOT NULL,
    Model NVARCHAR(10) NOT NULL,
    
    -- Services
    ServiceTypeHot NVARCHAR(100) NOT NULL,
    ServiceTypeHeating NVARCHAR(100) NOT NULL,
    
    -- Metadata
    DebtText NVARCHAR(500),
    ConsumptionText NVARCHAR(1000),
    ImportBatchId NVARCHAR(36) NOT NULL,  -- FK to ImportBatches.BatchId
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2,
    
    -- ==========================================
    -- FISCALIZATION FIELDS (CRITICAL!)
    -- ==========================================
    
    -- Common Fiscalization
    FiscalizationStatus NVARCHAR(50) NOT NULL DEFAULT 'not_required',
        -- Values: not_required, fiscalized, error, too_old
    
    FiscalizationMethod NVARCHAR(20),
        -- Values: fina, moje-racun
    
    FiscalizedAt DATETIME2,
    FiscalizationError NVARCHAR(1000),
    
    -- FINA Fields
    Jir NVARCHAR(100),  -- FINA unique invoice ID
    Zki NVARCHAR(100),  -- FINA security code
    
    -- mojE-Raèun Fields
    MojeRacunInvoiceId NVARCHAR(100),  -- ElectronicId from mojE-Raèun
    MojeRacunQrCodeUrl NVARCHAR(500),
    MojeRacunPdfUrl NVARCHAR(500),
    MojeRacunSubmittedAt DATETIME2,
    MojeRacunStatus NVARCHAR(50),
        -- Values: Obraðen, Poslan, Dostavljen, Odbijen
    
    -- Indexes
    INDEX IX_InvoiceNumber (InvoiceNumber),
    INDEX IX_FiscalizationStatus (FiscalizationStatus),
    INDEX IX_ImportBatchId (ImportBatchId),
    INDEX IX_CreatedAt (CreatedAt)
)
```

---

#### **UtilityInvoiceItems**
Line items for each invoice.

```sql
CREATE TABLE UtilityInvoiceItems (
    Id INT PRIMARY KEY IDENTITY,
    UtilityInvoiceId INT NOT NULL,
    ItemOrder INT NOT NULL,
    Description NVARCHAR(200) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,  -- KWH, kom, etc.
    UnitPrice DECIMAL(18,5) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    TaxRate DECIMAL(5,2) NOT NULL,  -- Usually 5.00 for utilities
    TaxCategoryCode NVARCHAR(2) NOT NULL,  -- "S" for standard
    KpdCode NVARCHAR(20) NOT NULL,  -- Croatian product classification
    
    FOREIGN KEY (UtilityInvoiceId) REFERENCES UtilityInvoices(Id) ON DELETE CASCADE
)
```

---

#### **UtilityConsumptionData**
Consumption parameters (for display purposes).

```sql
CREATE TABLE UtilityConsumptionData (
    Id INT PRIMARY KEY IDENTITY,
    UtilityInvoiceId INT NOT NULL,
    ParameterOrder INT NOT NULL,
    ParameterName NVARCHAR(100) NOT NULL,
    ParameterValue DECIMAL(18,3),
    
    FOREIGN KEY (UtilityInvoiceId) REFERENCES UtilityInvoices(Id) ON DELETE CASCADE
)
```

---

### Modified Tables

#### **CompanyProfiles**
Added mojE-Raèun configuration fields.

```sql
ALTER TABLE CompanyProfiles ADD
    -- mojE-Raèun Configuration
    MojeRacunEnabled BIT NOT NULL DEFAULT 0,
    MojeRacunEnvironment NVARCHAR(20),  -- demo, production
    MojeRacunClientId NVARCHAR(200),    -- Username
    MojeRacunClientSecret NVARCHAR(500), -- Password
    MojeRacunApiKey NVARCHAR(500);      -- Software ID
```

**?? IMPORTANT**: Store sensitive credentials securely. Consider encryption in production.

---

## ?? API Endpoints Reference

### Complete Endpoint Map

```
Authentication
??? POST /api/auth/login
??? POST /api/auth/register

Company Management
??? GET /api/utility/company
??? PUT /api/utility/company

Import Operations
??? POST /api/utility/import/upload
??? GET /api/utility/import/batches
??? GET /api/utility/import/batch/{batchId}

Invoice Operations
??? GET /api/utility/invoices
??? GET /api/utility/invoices/{id}
??? GET /api/utility/invoices/by-batch/{batchId}
??? DELETE /api/utility/invoices/{id}

Fiscalization Operations ?
??? POST /api/utility/fiscalization/test-connection
??? POST /api/utility/fiscalization/fiscalize/{invoiceId}
??? GET /api/utility/fiscalization/status/{invoiceId}
```

---

### Detailed Endpoint Documentation

#### **POST /api/utility/fiscalization/test-connection**

**Purpose**: Test connectivity to mojE-Raèun API before fiscalizing invoices.

**Request**:
```json
{
  "companyId": 1
}
```

**Response (Success)**:
```json
{
  "success": true,
  "message": "Connection test successful",
  "data": {
    "success": true,
    "status": "connected",
    "message": "Successfully connected to mojE-Raèun (demo environment). Service is operational",
    "rawResponse": "{\"Status\":\"ok\",\"Message\":\"Service is operational\"}"
  }
}
```

**Response (Failure)**:
```json
{
  "success": false,
  "message": "Connection test failed",
  "data": {
    "success": false,
    "status": null,
    "message": "Connection test failed: Unauthorized",
    "rawResponse": "{\"error\":\"Invalid credentials\"}"
  }
}
```

**Frontend Use**:
- Call this BEFORE attempting to fiscalize invoices
- Show result in company settings page
- Green checkmark for success, red X for failure

---

#### **POST /api/utility/fiscalization/fiscalize/{invoiceId}**

**Purpose**: Submit invoice to mojE-Raèun for fiscalization.

**Request**: No body (invoiceId in URL)

**Response (Success)**:
```json
{
  "success": true,
  "message": "Invoice fiscalized successfully",
  "data": {
    "success": true,
    "invoiceId": "3103850",  // ElectronicId from mojE-Raèun
    "status": "obraðen",     // Current status
    "message": "Invoice submitted successfully to mojE-Raèun (Status: Obraðen)",
    "submittedAt": "2025-11-24T13:48:42.5832685+01:00",
    "qrCodeUrl": "https://moj-eracun.hr/invoice/3103850",
    "pdfUrl": "https://moj-eracun.hr/pdf/3103850",
    "rawResponse": "{\"ElectronicId\":3103850,\"DocumentNr\":\"81-907007-2025044\",\"StatusName\":\"Obraðen\",...}"
  }
}
```

**Response (Failure - Validation Error)**:
```json
{
  "success": false,
  "message": "Fiscalization failed: Validation error",
  "data": {
    "success": false,
    "message": "XML nije prošao UBL provjeru valjanosti: [BR-07]-An Invoice shall contain the Buyer name (BT-44)",
    "rawResponse": "{\"File\":{\"Value\":\"Neispravan xml\",\"Messages\":[...]}}"
  }
}
```

**Response (Failure - Recipient Not Found)**:
```json
{
  "success": false,
  "message": "Fiscalization failed",
  "data": {
    "success": false,
    "message": "Nije moguæe naæi primatelja dokumenta",
    "rawResponse": "{\"File\":{\"Value\":\"Nije moguæe naæi primatelja dokumenta\",\"Messages\":[...]}}"
  }
}
```

**Status Values Explained**:
- `Obraðen` - Processed (invoice accepted by system)
- `Poslan` - Sent (delivered to recipient's mailbox)
- `Dostavljen` - Delivered (recipient opened/viewed)
- `Odbijen` - Rejected (recipient rejected the invoice)

**Frontend Use**:
- Show loading spinner during API call
- On success: Show success message + QR code + PDF links
- On failure: Show error message with details
- Refresh invoice list to show updated status

---

#### **GET /api/utility/fiscalization/status/{invoiceId}**

**Purpose**: Check current status of previously fiscalized invoice.

**Request**: No body (invoiceId in URL)

**Response**:
```json
{
  "success": true,
  "message": "Invoice status retrieved",
  "data": {
    "success": true,
    "status": "poslan",
    "message": "Status retrieved successfully",
    "rawResponse": "{\"StatusId\":30,\"StatusName\":\"Poslan\",...}"
  }
}
```

**Frontend Use**:
- Poll this endpoint every 10 seconds for real-time updates
- Update status badge when status changes
- Stop polling when status reaches "Dostavljen" or "Odbijen"

---

#### **GET /api/utility/invoices**

**Purpose**: Get paginated list of invoices with fiscalization status.

**Query Parameters**:
```
?pageNumber=1
&pageSize=20
&fiscalizationStatus=fiscalized
&searchTerm=A1
&sortBy=issueDate
&sortDesc=true
```

**Response**:
```json
{
  "success": true,
  "message": "Invoices retrieved successfully",
  "data": {
    "items": [
      {
        "id": 123,
        "invoiceNumber": "81-907007-2025044",
        "customerName": "A1 HRVATSKA D.O.O.",
        "customerOib": "29524210204",
        "issueDate": "2025-11-18T00:00:00",
        "dueDate": "2025-11-19T00:00:00",
        "totalAmount": 75.38,
        "fiscalizationStatus": "fiscalized",
        "fiscalizationMethod": "moje-racun",
        "fiscalizedAt": "2025-11-24T13:48:42",
        "mojeRacunInvoiceId": "3103850",
        "mojeRacunStatus": "Obraðen",
        "mojeRacunQrCodeUrl": "https://moj-eracun.hr/invoice/3103850",
        "mojeRacunPdfUrl": "https://moj-eracun.hr/pdf/3103850"
      }
    ],
    "totalCount": 1500,
    "pageNumber": 1,
    "pageSize": 20,
    "totalPages": 75
  }
}
```

**Frontend Use**:
- Display in data table with pagination
- Add fiscalization status badge column
- Add action buttons (Fiscalize/View Details)
- Implement filtering and searching

---

#### **GET /api/utility/invoices/{id}**

**Purpose**: Get detailed invoice data including all fiscalization info.

**Response**:
```json
{
  "success": true,
  "message": "Invoice retrieved successfully",
  "data": {
    "id": 123,
    "invoiceNumber": "81-907007-2025044",
    "issueDate": "2025-11-18T00:00:00",
    "dueDate": "2025-11-19T00:00:00",
    "customerCode": "903692",
    "customerName": "A1 HRVATSKA D.O.O.",
    "customerOib": "29524210204",
    "customerAddress": "VRTNI PUT 1",
    "city": "ZAGREB",
    "postalCode": "10000",
    "subTotal": 71.79,
    "vatAmount": 3.59,
    "totalAmount": 75.38,
    "fiscalizationStatus": "fiscalized",
    "fiscalizationMethod": "moje-racun",
    "fiscalizedAt": "2025-11-24T13:48:42",
    "mojeRacunInvoiceId": "3103850",
    "mojeRacunStatus": "Obraðen",
    "mojeRacunQrCodeUrl": "https://moj-eracun.hr/invoice/3103850",
    "mojeRacunPdfUrl": "https://moj-eracun.hr/pdf/3103850",
    "items": [
      {
        "id": 1,
        "description": "Energija za proizvodnju - topla voda",
        "quantity": 461.830,
        "unit": "KWH",
        "unitPrice": 0.048,
        "amount": 22.17,
        "taxRate": 5.00,
        "kpdCode": "35.30.11"
      }
    ]
  }
}
```

**Frontend Use**:
- Display in invoice detail page
- Show fiscalization section with all details
- Provide QR code and PDF download buttons

---

## ?? Production Deployment Requirements

### CRITICAL CHANGES REQUIRED FOR PRODUCTION ??

#### 1. **Remove A1 Hrvatska Fallback** (HIGHEST PRIORITY)

**Current Behavior**:
- System uses A1 Hrvatska (OIB: 29524210204) as default buyer when customer has no OIB
- This is for TESTING ONLY

**What to Change**:
```
File: Services/MojeRacunService.cs
Lines: ~420-430 (in GenerateUblXml method)

FIND:
bool hasCustomerOib = !string.IsNullOrEmpty(invoice.CustomerOib) && invoice.CustomerOib.Length == 11;
string effectiveCustomerOib = hasCustomerOib ? invoice.CustomerOib : "29524210204";
string effectiveCustomerName = hasCustomerOib ? invoice.CustomerName : "A1 HRVATSKA D.O.O.";

REPLACE WITH:
bool hasCustomerOib = !string.IsNullOrEmpty(invoice.CustomerOib) && invoice.CustomerOib.Length == 11;

if (!hasCustomerOib)
{
    throw new InvalidOperationException(
        $"Customer '{invoice.CustomerName}' (code: {invoice.CustomerCode}) has no OIB. " +
        "All customers must have valid OIBs for production fiscalization. " +
        "Please update customer data before fiscalizing."
    );
}

string effectiveCustomerOib = invoice.CustomerOib;
string effectiveCustomerName = invoice.CustomerName;
```

**Why Required**:
- Using A1's OIB for real customer invoices is ILLEGAL
- All Croatian citizens/companies have OIBs - they must be collected
- mojE-Raèun validates that recipient OIB matches registered company

**Action Items**:
- [ ] Update CSV source data to include customer OIBs
- [ ] Add OIB validation during import
- [ ] Reject imports with missing OIBs
- [ ] Contact data source provider to add OIB column

---

#### 2. **Get Production mojE-Raèun Credentials**

**Current Demo Credentials**:
```
Environment: demo
Username: 8711
Password: test123
Software ID: Test-001
Base URL: https://demo.moj-eracun.hr/apis/v2
```

**Production Setup**:
```
1. Register your company at: https://moj-eracun.hr
2. Request API access: podrska@moj-eracun.hr
3. Provide:
   - Company name
   - OIB
   - Contact person
   - Technical contact email
   - Software name/version
4. Receive production credentials:
   - Username
   - Password
   - Software ID
5. Update in Company Settings:
   - Switch Environment to "production"
   - Enter production credentials
   - Test connection before going live
```

**Production Base URL**:
```
https://api.moj-eracun.hr/apis/v2
```

---

#### 3. **Secure Credential Storage**

**Current State**:
- Credentials stored in plain text in database
- `MojeRacunClientSecret` (password) is NOT encrypted

**Required Changes**:
```
Option A: Use .NET Data Protection
- Encrypt MojeRacunClientSecret before storing
- Decrypt when reading from database
- Add IDataProtectionProvider to services

Option B: Use Azure Key Vault (Recommended)
- Store credentials in Azure Key Vault
- Reference by key vault URI in database
- Application reads from Key Vault at runtime

Option C: Environment Variables
- Store in server environment variables
- Do NOT commit to source control
- Load via IConfiguration
```

**Action Items**:
- [ ] Implement encryption for sensitive fields
- [ ] Add encryption migration
- [ ] Update CompanyController to encrypt/decrypt
- [ ] Test credential retrieval after encryption

---

#### 4. **Disable Debug XML Logging**

**Current Behavior**:
- All generated XML saved to `wwwroot/FiscalizationDebug/`
- API responses saved to same folder

**What to Change**:
```
File: Services/MojeRacunService.cs

FIND:
SaveXmlForDebug(invoice.InvoiceNumber, ublXml, "UBL_Direct");

REPLACE WITH:
if (_environment.IsDevelopment())
{
    SaveXmlForDebug(invoice.InvoiceNumber, ublXml, "UBL_Direct");
}

FIND:
SaveResponseForDebug(invoice.InvoiceNumber, responseBody, (int)response.StatusCode);

REPLACE WITH:
if (_environment.IsDevelopment())
{
    SaveResponseForDebug(invoice.InvoiceNumber, responseBody, (int)response.StatusCode);
}
```

**Why Required**:
- Disk space: XML files accumulate rapidly in production
- Performance: File I/O on every fiscalization
- Security: XML contains sensitive customer data

**Alternative**:
- Add configuration flag `EnableDebugLogging` in `appsettings.json`
- Set to `false` in production

---

#### 5. **Update Logging Levels**

**Current State**:
- Many `LogInformation` and `LogWarning` calls
- Verbose logging for debugging

**Recommended Production Logging**:
```json
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "InventoryManagementAPI.Services.MojeRacunService": "Error",
      "InventoryManagementAPI.Services.FiscalizationService": "Error"
    }
  }
}
```

**Keep Logging For**:
- Errors (always log)
- Fiscalization success (with invoice ID)
- Failed validations (with details)

**Remove/Reduce Logging For**:
- XML generation steps
- Diagnostic warnings about test mode
- Debug information about OIB handling

---

#### 6. **Add Rate Limiting**

**Current State**:
- No rate limiting on fiscalization endpoint
- Users could overwhelm mojE-Raèun API

**Required**:
```
Add rate limiting middleware:
- Max 10 fiscalizations per minute per user
- Max 100 fiscalizations per hour per company
- Return 429 Too Many Requests if exceeded

Implementation:
Install-Package AspNetCoreRateLimit

Configure in Program.cs:
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(
    builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
```

---

#### 7. **Add Retry Logic**

**Current State**:
- Single attempt to submit to mojE-Raèun
- Network failures cause immediate error

**Required**:
```
Install-Package Polly

Add retry policy:
- Retry 3 times on network errors
- Exponential backoff (1s, 2s, 4s)
- Only retry transient errors (timeout, connection reset)
- Do NOT retry validation errors (400 Bad Request)
```

---

#### 8. **Database Indexes**

**Current State**:
- Basic indexes exist
- May be slow on large datasets

**Add Indexes**:
```sql
-- Improve invoice list filtering
CREATE INDEX IX_UtilityInvoices_FiscalizationStatus_IssueDate
ON UtilityInvoices(FiscalizationStatus, IssueDate DESC);

-- Improve search by customer
CREATE INDEX IX_UtilityInvoices_CustomerOib
ON UtilityInvoices(CustomerOib);

-- Improve batch queries
CREATE INDEX IX_UtilityInvoices_ImportBatchId_CreatedAt
ON UtilityInvoices(ImportBatchId, CreatedAt DESC);

-- Improve mojE-Raèun ID lookups
CREATE INDEX IX_UtilityInvoices_MojeRacunInvoiceId
ON UtilityInvoices(MojeRacunInvoiceId)
WHERE MojeRacunInvoiceId IS NOT NULL;
```

---

#### 9. **Add Health Checks**

**Purpose**: Monitor system health and mojE-Raèun connectivity

```
Add health check endpoints:
GET /health
GET /health/ready
GET /health/live

Checks:
- Database connectivity
- mojE-Raèun API availability (via /Ping)
- Disk space for debug logs
- Last successful fiscalization timestamp
```

---

#### 10. **Implement Audit Logging**

**Current State**:
- No audit trail for fiscalization actions

**Required**:
```
Create AuditLog table:
- Who fiscalized which invoice
- When
- Result (success/failure)
- Error details if failed

Log events:
- Company settings changed
- mojE-Raèun credentials updated
- Batch import completed
- Invoice fiscalized
- Fiscalization error occurred
```

---

## ?? Known Issues & Current Limitations

### Issue 1: Customer OIB Requirement ??

**Problem**:
```
Error: "Nije moguæe naæi primatelja dokumenta"
(Cannot find document recipient)
```

**Cause**:
- Customer OIB is missing in source CSV data
- System uses A1 Hrvatska as fallback (testing only)
- mojE-Raèun cannot find recipient in AMS (Address Management System)

**Current Workaround**:
- System automatically uses OIB: 29524210204 (A1 Hrvatska)
- A1 is registered in AMS, so invoice is accepted

**Production Solution**:
- MUST collect real customer OIBs
- Update CSV format to include OIB column
- Validate OIB format (11 digits) during import
- Reject invoices with missing/invalid OIBs

---

### Issue 2: Date Handling

**Problem**:
- CSV dates may be in future (e.g., 2025-11-18)
- mojE-Raèun may reject future dates

**Current Solution**:
```csharp
// In GenerateUblXml:
var issueDate = invoice.IssueDate > DateTime.Now 
    ? DateTime.Now 
    : invoice.IssueDate;
```

**Recommendation**:
- Fix dates during CSV import instead
- Validate dates are not in future
- Log warnings for suspicious dates

---

### Issue 3: Customer Address Quality

**Problem**:
- Many customers have incomplete addresses
- City/PostalCode may be missing
- mojE-Raèun requires valid postal address

**Current Solution**:
```csharp
// Fallback to defaults:
var customerCity = !string.IsNullOrWhiteSpace(invoice.City)
    ? invoice.City
    : "Koprivnica";  // Default

var customerPostalCode = !string.IsNullOrWhiteSpace(invoice.PostalCode)
    ? invoice.PostalCode
    : "48000";  // Default
```

**Production Solution**:
- Improve data quality at source
- Add address validation during import
- Allow manual correction before fiscalization

---

### Issue 4: Tax Rate Assumptions

**Problem**:
- All invoices assumed to be 5% VAT (reduced rate)
- No support for 13% or 25% rates
- No support for tax-exempt items

**Current Code**:
```csharp
var taxRate = 5.00m;  // Hardcoded
var taxCategory = "S";  // Standard rated
```

**Recommendation**:
- Add tax rate column to CSV
- Support multiple tax categories (S, Z, E, AE, K, G, O)
- Validate tax rates against Croatian law

---

### Issue 5: No Batch Status Tracking

**Problem**:
- Cannot fiscalize entire batch at once from UI
- No batch-level fiscalization status
- No retry for failed invoices in batch

**Missing Features**:
- Batch fiscalization endpoint
- Batch progress tracking
- Automatic retry on transient errors

**Workaround**:
- Fiscalize invoices one by one manually
- Track status individually

---

### Issue 6: Real Customer Names Lost

**Problem**:
- When using A1 fallback, real customer name is lost
- XML shows "A1 HRVATSKA D.O.O." instead of actual customer
- Cannot track who the real recipient was

**Production Fix**:
- Do NOT use fallback OIB
- Force real OIB collection
- Add custom field for display name vs legal name

---

## ?? Testing Guide

### Testing Checklist

#### Phase 1: Company Setup Testing

```
? Navigate to Company Settings page
? Fill in mojE-Raèun credentials:
  Username: 8711
  Password: test123
  Software ID: Test-001
  Environment: demo
? Click "Test Connection"
? Verify: "? Connected successfully" message
? Click "Save Settings"
? Verify: Settings persisted after page refresh
```

---

#### Phase 2: CSV Import Testing

```
? Prepare test CSV with 5-10 invoices
? Navigate to Import page
? Upload CSV file
? Verify: Import batch created
? Check: All invoices visible in list
? Verify: No import errors
? Check: Invoice items correctly parsed
? Verify: Customer data extracted correctly
```

---

#### Phase 3: Single Invoice Fiscalization

```
? Select one invoice (not fiscalized)
? Click "Fiscalize" button
? Wait for API response
? Verify: Success message shown
? Check: ElectronicId returned (e.g., 3103850)
? Verify: Status badge changed to "? Fiscalized"
? Check: QR Code URL provided
? Check: PDF URL provided
? Open QR Code URL in browser
? Verify: QR code displays correctly
? Open PDF URL in browser
? Verify: PDF downloads successfully
```

---

#### Phase 4: Invoice Detail Testing

```
? Click on fiscalized invoice
? Open invoice detail page
? Verify: Fiscalization section visible
? Check: mojE-Raèun ID shown
? Check: Status shown (Obraðen)
? Check: Fiscalized timestamp shown
? Click "View QR Code" button
? Verify: QR code opens in new tab
? Click "Download PDF" button
? Verify: PDF downloads
```

---

#### Phase 5: Error Handling Testing

```
? Disable mojE-Raèun (set enabled = false)
? Try to fiscalize invoice
? Verify: Error message shown
? Re-enable mojE-Raèun
? Enter WRONG password
? Click "Test Connection"
? Verify: Connection fails with error
? Restore correct password
? Test connection again
? Verify: Connection succeeds
```

---

#### Phase 6: Batch Testing

```
? Import batch of 20+ invoices
? Select multiple invoices (if batch feature exists)
? Click "Fiscalize Selected"
? Watch progress bar
? Verify: Invoices fiscalized sequentially
? Check: Success/failure counts
? Verify: Failed invoices show errors
? Check: All successful invoices have ElectronicIds
```

---

### Testing Data

**Test OIBs (Registered in Demo AMS)**:
```
29524210204 - A1 Hrvatska d.o.o.
36522275328 - Hrvatski Telekom d.d.
48920148964 - Konzum d.d.
```

**Test Invoice Numbers**:
```
Format: XX-YYYYYY-ZZZZNNN
XX = Business unit (e.g., 81)
YYYYYY = Sequential number (e.g., 907007)
ZZZZ = Year (e.g., 2025)
NNN = Suffix (e.g., 044)
```

---

### Debug Tools

#### 1. Check Generated XML

**Location**: `wwwroot/FiscalizationDebug/`

**Files**:
```
MojeRacun_UBL_Direct_{invoiceNumber}_{timestamp}.xml
```

**What to Check**:
- Customer OIB (should NOT be all zeros in production)
- Customer name (check if real or A1 fallback)
- Invoice items present
- Tax rates correct
- Total amounts match

---

#### 2. Check API Responses

**Location**: `wwwroot/FiscalizationDebug/`

**Files**:
```
MojeRacun_Response_{invoiceNumber}_{timestamp}_Status{code}.json
```

**What to Check**:
- Status code (200 = OK, 400 = validation error, 401 = auth error)
- ElectronicId present in success responses
- Error messages in failure responses
- Trace IDs for mojE-Raèun support

---

#### 3. Validate XML Online

**Tool**: https://validate-demo.moj-eracun.hr/Validation/ValidateUBL

**Steps**:
1. Copy XML from `FiscalizationDebug` folder
2. Save as `.xml` file
3. Upload to validator
4. Select: "Kompatibilnost za slanje eRaèuna prema EN 16931"
5. Click "Validate"
6. Review validation errors (if any)

---

## ?? Troubleshooting Guide

### Error: "Connection test failed: Unauthorized"

**Possible Causes**:
- Wrong username or password
- Credentials not saved in database
- Using production credentials against demo API

**Solution**:
```
1. Check company settings in database:
   SELECT MojeRacunClientId, MojeRacunEnvironment 
   FROM CompanyProfiles WHERE Id = 1;

2. Verify environment matches credentials:
   Demo: https://demo.moj-eracun.hr
   Prod: https://api.moj-eracun.hr

3. Re-enter credentials in UI
4. Test connection again
```

---

### Error: "Nije moguæe naæi primatelja dokumenta"

**Translation**: "Cannot find document recipient"

**Cause**:
- Customer OIB not registered in mojE-Raèun AMS
- Using test/fake OIB

**Solution**:
```
1. Check invoice XML - find customer OIB
2. Verify OIB is real company/person
3. Check if recipient registered at moj-eracun.hr
4. For testing: Use known registered OIBs (see Testing Data)
5. For production: Collect real customer OIBs
```

---

### Error: "[BR-07] Invoice shall contain the Buyer name"

**Cause**:
- Customer name missing from XML
- XML structure incorrect

**Solution**:
```
1. Check generated XML in FiscalizationDebug folder
2. Find <cac:AccountingCustomerParty> section
3. Verify <cac:PartyName><cbc:Name> exists
4. Check name is not empty

Code location: MojeRacunService.cs, line ~420
Should have:
sb.AppendLine("    <cac:PartyName>");
sb.AppendLine($"    <cbc:Name>{XmlEscape(effectiveCustomerName)}</cbc:Name>");
sb.AppendLine("    </cac:PartyName>");
```

---

### Error: "[HR-BR-S-01] Standard rated invoice requires buyer VAT ID"

**Cause**:
- Using tax category "S" (standard rated)
- Buyer has no VAT ID (PartyTaxScheme missing)

**Solution**:
```
1. Check invoice XML - find <cac:PartyTaxScheme>
2. Verify <cbc:CompanyID>HR{OIB}</cbc:CompanyID> exists
3. If missing: Customer OIB is empty
4. Either: Collect real OIB OR use different tax category

Code location: MojeRacunService.cs, line ~480
Should include:
<cac:PartyTaxScheme>
  <cbc:CompanyID>HR29524210204</cbc:CompanyID>
  <cac:TaxScheme>
    <cbc:ID>VAT</cbc:ID>
  </cac:TaxScheme>
</cac:PartyTaxScheme>
```

---

### Error: "[HR-BR-33] No empty XML elements allowed"

**Cause**:
- XML contains empty elements like `<cbc:EndpointID></cbc:EndpointID>`
- mojE-Raèun rejects empty elements

**Solution**:
```
1. Check generated XML for empty tags
2. Find elements with no content between opening/closing tags
3. Remove empty elements OR provide placeholder value

Common culprits:
- Empty PostalZone
- Empty EndpointID
- Empty PartyIdentification

Current fix in code:
- EndpointID uses "0" if no OIB
- PostalZone uses "48000" if missing
```

---

### Error: "Invoice date is in the future"

**Cause**:
- CSV source data has future dates
- Invoice IssueDate > current date

**Solution**:
```
1. Check invoice IssueDate in database
2. Verify dates in CSV file
3. Code already handles this:
   var issueDate = invoice.IssueDate > DateTime.Now 
       ? DateTime.Now 
       : invoice.IssueDate;

If still occurring:
- Check time zones (server vs database)
- Verify CSV date parsing
```

---

### Error: Network timeout connecting to mojE-Raèun

**Possible Causes**:
- Firewall blocking outbound HTTPS
- VPN/proxy issues
- mojE-Raèun API temporarily down

**Solution**:
```
1. Check firewall allows HTTPS to:
   - demo.moj-eracun.hr
   - api.moj-eracun.hr

2. Test connectivity:
   curl https://demo.moj-eracun.hr/apis/v2/Ping

3. Check proxy settings in code

4. Verify HttpClient timeout:
   client.Timeout = TimeSpan.FromSeconds(60);
```

---

### Issue: XML files filling up disk space

**Cause**:
- Debug logging enabled
- Many fiscalizations over time

**Solution**:
```
1. Disable debug logging in production
2. Add scheduled job to clean old files:
   Delete files older than 30 days

3. Or store in database instead:
   CREATE TABLE FiscalizationLogs (
     Id INT PRIMARY KEY,
     InvoiceId INT,
     GeneratedXml NVARCHAR(MAX),
     ResponseJson NVARCHAR(MAX),
     CreatedAt DATETIME2
   )
```

---

## ?? Additional Resources

### Official Documentation

- **mojE-Raèun Official**: https://moj-eracun.hr
- **Documentation Portal**: https://moj-eracun.hr/dokumentacija
- **API Specification**: https://demo.moj-eracun.hr/swagger
- **UBL 2.1 Standard**: https://docs.oasis-open.org/ubl/UBL-2.1.html
- **EN16931 Specification**: https://ec.europa.eu/digital-building-blocks/wikis/display/DIGITAL/Standards
- **Croatian CIUS**: https://mfin.gov.hr/e-racuni/8942

### Tools

- **XML Validator**: https://validate-demo.moj-eracun.hr/Validation/ValidateUBL
- **XSD Schema Validator**: https://www.freeformatter.com/xml-validator-xsd.html
- **UBL Examples**: https://github.com/ConnectingEurope/eInvoicing-EN16931

### Support

- **mojE-Raèun Support**: podrska@moj-eracun.hr
- **Phone**: +385 1 4591 666
- **Business Hours**: Mon-Fri 8:00-16:00 CET

---

## ?? Summary

### ? What You Have Now

- Working backend API for mojE-Raèun integration
- CSV import system for utility invoices
- Database schema with fiscalization tracking
- Debug tools (XML logging, API responses)
- Test connection endpoint
- Fiscalization endpoint with full UBL 2.1 compliance
- Error handling and validation

### ?? What You Need to Build (Frontend)

**Priority 1 (Week 1)**:
1. Company Settings page with mojE-Raèun configuration
2. Test Connection button
3. Invoice list with fiscalization status badges
4. Fiscalize button per invoice
5. Success/error messages

**Priority 2 (Week 2)**:
6. Invoice detail page with fiscalization section
7. QR code and PDF download buttons
8. Batch fiscalization feature
9. Dashboard widgets

**Priority 3 (Week 3+)**:
10. Real-time status polling
11. Fiscalization reports
12. Advanced filtering

### ?? Critical for Production

1. **Remove A1 OIB fallback** - MUST use real customer OIBs
2. **Get production credentials** from mojE-Raèun
3. **Encrypt sensitive data** (passwords, certificates)
4. **Disable debug logging** to save disk space
5. **Add customer OIB validation** during import
6. **Implement rate limiting** to avoid API throttling
7. **Add retry logic** for network failures
8. **Set up monitoring** and alerts
9. **Create audit logs** for compliance
10. **Test thoroughly** with real data before launch

---

## ?? Next Steps

1. **Start with Company Settings page** - This is blocking everything else
2. **Test connection** to mojE-Raèun demo environment
3. **Build invoice list enhancements** - Show status, add fiscalize button
4. **Implement single invoice fiscalization** - Core user flow
5. **Add invoice detail page** - Show fiscalization results
6. **Collect real customer OIBs** - Prepare for production
7. **Get production credentials** - Register with mojE-Raèun
8. **Production deployment** - Follow checklist above
9. **Monitor and optimize** - Track success rates, fix issues

---

**Good luck with your implementation! ??**

If you encounter issues, check the Troubleshooting section or contact mojE-Raèun support with the Trace ID from error responses.
