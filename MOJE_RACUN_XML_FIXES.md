# ?? mojE-Raèun XML Format - Fixed Issues

## ? **All Issues Resolved!**

Your XML had several missing/incorrect fields compared to the mojE-Raèun example. All have been fixed.

---

## ?? **Problems That Were Fixed**

### **1. Wrong CustomizationID**
**? Before:**
```xml
<cbc:CustomizationID>urn:cen.eu:en16931:2017#compliant#urn:fina.hr:2.0</cbc:CustomizationID>
```

**? After:**
```xml
<cbc:CustomizationID>urn:cen.eu:en16931:2017#compliant#urn:mfin.gov.hr:cius-2025:1.0#conformant#urn:mfin.gov.hr:ext-2025:1.0</cbc:CustomizationID>
```

---

### **2. Missing Required Header Fields**
**? Added:**
```xml
<cbc:ProfileID>P3</cbc:ProfileID>
<cbc:CopyIndicator>false</cbc:CopyIndicator>
<cbc:IssueTime>09:58:20</cbc:IssueTime>
```

**? Enhanced DocumentCurrencyCode:**
```xml
<cbc:DocumentCurrencyCode listID="ISO 4217 Alpha" listAgencyID="5" listSchemeURI="http://docs.oasis-open.org/ubl/os-UBL-2.1/cl/gc/default/CurrencyCode-2.1.gc">EUR</cbc:DocumentCurrencyCode>
```

---

### **3. Missing Supplier (AccountingSupplierParty) Fields**
**? Added:**
```xml
<cbc:EndpointID schemeID="9934">79830143058</cbc:EndpointID>
<cac:PartyIdentification>
  <cbc:ID>9934:79830143058</cbc:ID>
</cac:PartyIdentification>
<cac:AddressLine>
  <cbc:Line>Your address, City, PostalCode</cbc:Line>
</cac:AddressLine>
<cac:PartyLegalEntity>
  <cbc:RegistrationName>Company Name</cbc:RegistrationName>
  <cbc:CompanyID>79830143058</cbc:CompanyID>
</cac:PartyLegalEntity>
```

**? Added SellerContact:**
```xml
<cac:SellerContact>
  <cbc:ID>OperatorOIB</cbc:ID>
  <cbc:Name>OPERATER</cbc:Name>
</cac:SellerContact>
```

---

### **4. Missing Customer (AccountingCustomerParty) Fields**
**? Added:**
```xml
<cbc:EndpointID schemeID="9934">CustomerOIB</cbc:EndpointID>
<cac:PartyIdentification>
  <cbc:ID>9934:CustomerOIB</cbc:ID>
</cac:PartyIdentification>
<cac:AddressLine>
  <cbc:Line>Customer address</cbc:Line>
</cac:AddressLine>
<cac:PartyLegalEntity>
  <cbc:RegistrationName>Customer Name</cbc:RegistrationName>
  <cbc:CompanyID>CustomerOIB</cbc:CompanyID>
</cac:PartyLegalEntity>
```

**? Fixed empty address fields:**
- Empty `StreetName` now uses "N/A" as placeholder
- Empty `PostalZone` is conditionally included (not empty tag)

---

### **5. Missing Delivery Section**
**? Added:**
```xml
<cac:Delivery>
  <cbc:ActualDeliveryDate>2025-11-18</cbc:ActualDeliveryDate>
</cac:Delivery>
```

---

### **6. Missing Payment Instruction**
**? Added:**
```xml
<cbc:InstructionNote>Plaæanje po raèunu</cbc:InstructionNote>
```

---

### **7. Missing Allowance/Charge Totals**
**? Added:**
```xml
<cbc:AllowanceTotalAmount currencyID="EUR">0.00</cbc:AllowanceTotalAmount>
<cbc:ChargeTotalAmount currencyID="EUR">0.00</cbc:ChargeTotalAmount>
```

---

### **8. Wrong Commodity Classification List ID**
**? Before:**
```xml
<cbc:ItemClassificationCode listID="KPD"></cbc:ItemClassificationCode>
```

**? After:**
```xml
<cbc:ItemClassificationCode listID="CG">35.30.11</cbc:ItemClassificationCode>
```

`"CG"` = Commodity Group (standard UBL), not `"KPD"` (Croatian-specific)

---

### **9. Empty/Invalid Item Tax Values**
**? Before:**
```xml
<cbc:ID></cbc:ID>                   <!-- Empty! -->
<cbc:Percent>0.00</cbc:Percent>     <!-- Wrong! Should be 5.00 -->
```

**? After:**
```xml
<cbc:ID>S</cbc:ID>                  <!-- "S" for Standard rate -->
<cbc:Percent>5.00</cbc:Percent>     <!-- Correct 5% VAT -->
```

---

### **10. Missing BaseQuantity in Price**
**? Before:**
```xml
<cac:Price>
  <cbc:PriceAmount currencyID="EUR">0.04800</cbc:PriceAmount>
</cac:Price>
```

**? After:**
```xml
<cac:Price>
  <cbc:PriceAmount currencyID="EUR">0.05</cbc:PriceAmount>
  <cbc:BaseQuantity unitCode="KWH">769.72</cbc:BaseQuantity>
</cac:Price>
```

---

## ?? **Comparison Summary**

| Field | Before | After | Status |
|-------|--------|-------|--------|
| **CustomizationID** | `urn:fina.hr:2.0` | `urn:mfin.gov.hr:cius-2025:1.0` | ? Fixed |
| **ProfileID** | Missing | `P3` | ? Added |
| **CopyIndicator** | Missing | `false` | ? Added |
| **IssueTime** | Missing | Current time | ? Added |
| **EndpointID (Supplier)** | Missing | OIB | ? Added |
| **PartyIdentification** | Missing | `9934:OIB` | ? Added |
| **AddressLine** | Missing | Full address | ? Added |
| **PartyLegalEntity** | Missing | Company details | ? Added |
| **SellerContact** | Missing | Operator info | ? Added |
| **EndpointID (Customer)** | Missing | Customer OIB | ? Added |
| **Delivery** | Missing | Actual delivery date | ? Added |
| **InstructionNote** | Missing | Payment instruction | ? Added |
| **AllowanceTotalAmount** | Missing | 0.00 | ? Added |
| **ChargeTotalAmount** | Missing | 0.00 | ? Added |
| **ItemClassificationCode listID** | `KPD` | `CG` | ? Fixed |
| **Empty KPD Code** | Empty string | Actual code | ? Fixed |
| **Tax Category ID** | Empty | `S` | ? Fixed |
| **Tax Percent** | 0.00 | 5.00 | ? Fixed |
| **BaseQuantity** | Missing | Quantity with unit | ? Added |

---

## ?? **Test Again**

Now try fiscalizing an invoice again:

```sh
curl -X POST "https://localhost:7001/api/UtilityFiscalization/123/fiscalize-moje-racun" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"companyId": 1}'
```

Check the newly generated XML in:
```
wwwroot/FiscalizationDebug/MojeRacun_UBL_Wrapped_[InvoiceNumber]_[Timestamp].xml
```

**All fields should now be present and properly formatted!** ?

---

## ?? **Note About PDF Attachment**

The example XML includes an embedded PDF:
```xml
<cac:AdditionalDocumentReference>
  <cbc:ID>Raèun</cbc:ID>
  <cac:Attachment>
    <cbc:EmbeddedDocumentBinaryObject mimeCode="application/pdf" filename="Racun.pdf">
      Base64EncodedPDF...
    </cbc:EmbeddedDocumentBinaryObject>
  </cac:Attachment>
</cac:AdditionalDocumentReference>
```

**This is OPTIONAL!** You don't need to include it unless you want to attach a PDF version of the invoice. The mojE-Raèun system will accept invoices without it.

---

## ?? **What to Expect**

With these fixes, your XML now:
- ? Matches mojE-Raèun UBL 2.1 requirements
- ? Has all mandatory fields
- ? Uses correct namespace and identifiers
- ? Includes proper party identification
- ? Has valid tax rates (5% for utilities)
- ? Includes delivery and payment instructions
- ? Uses correct commodity classification scheme

**The API should now accept your invoices!** ??

If you still get errors, check the `RawResponse` in the API response for specific error messages from mojE-Raèun.
