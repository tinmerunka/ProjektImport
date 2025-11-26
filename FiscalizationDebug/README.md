# ?? Fiscalization Debug Files - User Guide

## ?? New Organized Structure

All fiscalization debug files are now organized by **invoice number** and **fiscalization method** for easy navigation:

```
FiscalizationDebug/
??? FINA/
?   ??? 81-907013-2025045/
?   ?   ??? 01-Request-Signed.xml          ? Signed XML (before SOAP envelope)
?   ?   ??? 02-Request-SOAP-Envelope.xml   ? Complete SOAP request sent to FINA
?   ?   ??? 03-Response-200.xml            ? FINA server response (200 = success)
?   ?   ??? metadata.json                   ? File descriptions and timestamp
?   ?
?   ??? 81-907014-2025046/
?       ??? 01-Request-Signed.xml
?       ??? 02-Request-SOAP-Envelope.xml
?       ??? 03-Response-500.xml            ? Error response (500 = server error)
?       ??? metadata.json
?
??? MojeRacun/
    ??? 81-907013-2025045/
    ?   ??? 01-Request-UBL.xml             ? UBL 2.1 XML invoice
    ?   ??? 02-Response-200.json           ? mojE-Raèun API response (JSON)
    ?   ??? metadata.json                   ? File descriptions and timestamp
    ?
    ??? 81-907015-2025047/
        ??? 01-Request-UBL.xml
        ??? 02-Response-400.json           ? Validation error (400 = bad request)
        ??? metadata.json
```

## ?? File Naming Convention

### FINA Fiscalization
1. **`01-Request-Signed.xml`** - Digitally signed XML request (before adding SOAP envelope)
2. **`02-Request-SOAP-Envelope.xml`** - Complete SOAP envelope sent to FINA CIS server
3. **`03-Response-{StatusCode}.xml`** - FINA server response
   - `200` = Success (contains JIR code)
   - `500` = Server error
   - `400` = Validation error

### mojE-Raèun Fiscalization
1. **`01-Request-UBL.xml`** - UBL 2.1 format XML invoice
2. **`02-Response-{StatusCode}.json`** - mojE-Raèun API JSON response
   - `200` = Success (contains ElectronicId)
   - `400` = Validation error
   - `500` = Server error

## ?? Metadata File

Each invoice folder contains a `metadata.json` with:
```json
{
  "InvoiceNumber": "81-907013-2025045",
  "Timestamp": "2025-01-26 14:30:45",
  "Type": "FINA Fiscalization",
  "Files": [
    {
      "Order": 1,
      "File": "01-Request-Signed.xml",
      "Description": "Signed XML request (before SOAP envelope)"
    },
    ...
  ]
}
```

## ?? How to Find Your Files

### By Invoice Number
Navigate to: `FiscalizationDebug/{Method}/{InvoiceNumber}/`

**Example:**
- Invoice: `81-907013-2025045`
- FINA files: `FiscalizationDebug/FINA/81-907013-2025045/`
- mojE-Raèun files: `FiscalizationDebug/MojeRacun/81-907013-2025045/`

### By Response Status
Check the filename suffix:
- `03-Response-200.xml` ? Success
- `03-Response-400.xml` ?? Validation Error
- `03-Response-500.xml` ? Server Error

## ?? Benefits

? **Organized by Invoice** - All related files grouped together  
? **Sequential Numbering** - Clear order: Request ? Response  
? **Status Code in Name** - Instantly see success/error  
? **Metadata** - File descriptions and timestamps  
? **No Long Timestamps** - Clean, readable folder names  

## ?? Old vs New Comparison

### ? Old Structure (Messy)
```
FiscalizationDebug/
??? Fina_SIGNED_81-907013-2025045_20250126143045123.xml
??? Fina_SOAP_81-907013-2025045_20250126143045456.xml
??? Fina_SIGNED_81-907013-2025045_RESPONSE_200_20250126143046789.xml
??? MojeRacun_UBL_Direct_81-907013-2025045_20250126143045123.xml
??? MojeRacun_Response_81-907013-2025045_20250126143046789_Status200.json
??? ...150 more files mixed together...
```

### ? New Structure (Clean)
```
FiscalizationDebug/
??? FINA/
?   ??? 81-907013-2025045/
?       ??? 01-Request-Signed.xml
?       ??? 02-Request-SOAP-Envelope.xml
?       ??? 03-Response-200.xml
?       ??? metadata.json
??? MojeRacun/
    ??? 81-907013-2025045/
        ??? 01-Request-UBL.xml
        ??? 02-Response-200.json
        ??? metadata.json
```

---

**?? Note:** This structure is automatically created when you fiscalize invoices. No manual setup required!
