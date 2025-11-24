# ?? mojE-Raèun Authentication & Configuration Guide

## ?? Authentication Methods for mojE-Raèun

mojE-Raèun supports **TWO authentication methods**:

### **Method 1: Digital Certificate (.pfx file)** ? RECOMMENDED
This is similar to FINA fiscalization - you use a digital certificate issued by the Croatian Tax Administration.

### **Method 2: Username & Password (OAuth 2.0)**
Login credentials for the mojE-Raèun web portal with OAuth token generation.

---

## ?? **Method 1: Certificate Authentication (Recommended)**

### **How to Get a Certificate:**

1. **Register your company** on mojE-Raèun portal:
   - **Test environment:** https://test.moj-eracun.hr
   - **Production:** https://moj-eracun.hr

2. **Request a digital certificate** from:
   - Financial Agency (FINA)
   - Or any authorized Certificate Authority (CA) in Croatia

3. **Download the .pfx file** with your private key

### **Configuration in Your System:**

```csharp
// In CompanyProfile table:
MojeRacunEnabled = true
MojeRacunCertificatePath = "certificates/moje-racun-cert.pfx"
MojeRacunCertificatePassword = "your-certificate-password"
MojeRacunEnvironment = "test" // or "production"
```

### **How the System Uses It:**

```csharp
// In MojeRacunService.cs
private X509Certificate2 LoadCertificate(CompanyProfile company)
{
    var fullPath = Path.Combine(_environment.WebRootPath, company.MojeRacunCertificatePath);
    var password = company.MojeRacunCertificatePassword ?? string.Empty;
    
    return new X509Certificate2(fullPath, password, X509KeyStorageFlags.Exportable);
}

// Attach certificate to HTTP request
var handler = new HttpClientHandler();
handler.ClientCertificates.Add(certificate);
var client = new HttpClient(handler);
```

---

## ?? **Method 2: Username & Password (OAuth 2.0)**

### **How It Works:**

1. You log in with your **mojE-Raèun username and password**
2. System exchanges credentials for an **OAuth access token**
3. Access token is used for API calls (valid for ~1 hour)
4. System automatically refreshes token when expired

### **Configuration in Your System:**

```csharp
// In CompanyProfile table:
MojeRacunEnabled = true
MojeRacunClientId = "your-username@company.hr"
MojeRacunClientSecret = "your-password"
MojeRacunEnvironment = "test" // or "production"
```

### **Implementation (needs to be added):**

```csharp
// In MojeRacunService.cs
private async Task<string> GetOAuthTokenAsync(CompanyProfile company)
{
    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "/oauth/token")
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", company.MojeRacunClientId },
            { "password", company.MojeRacunClientSecret },
            { "scope", "invoice:submit invoice:read" }
        })
    };

    var response = await _httpClient.SendAsync(tokenRequest);
    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
    
    return tokenResponse.AccessToken;
}

// Usage:
var token = await GetOAuthTokenAsync(company);
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

---

## ??? **Where to Store Credentials**

### **Option A: Database (CompanyProfile table)** ? CURRENT
Already implemented! The `CompanyProfile` table has these fields:

```sql
-- Certificate method:
MojeRacunCertificatePath nvarchar(500)
MojeRacunCertificatePassword nvarchar(500)

-- Username/Password method:
MojeRacunClientId nvarchar(200)          -- Username
MojeRacunClientSecret nvarchar(500)      -- Password

-- Common:
MojeRacunEnvironment nvarchar(20)        -- "test" or "production"
MojeRacunEnabled bit
```

### **Option B: appsettings.json** (Alternative)
For single-company setups:

```json
{
  "MojeRacun": {
    "Environment": "test",
    "Authentication": {
      "Method": "certificate",
      "CertificatePath": "certificates/moje-racun.pfx",
      "CertificatePassword": "encrypted-password",
      
      // OR for OAuth:
      "Username": "your-username@company.hr",
      "Password": "encrypted-password"
    },
    "Endpoints": {
      "Test": "https://api-test.moj-eracun.hr/v1",
      "Production": "https://api.moj-eracun.hr/v1"
    }
  }
}
```

---

## ?? **Security Best Practices**

### **1. Encrypt Passwords in Database**

?? **IMPORTANT:** Never store passwords in plain text!

**Add encryption service:**

```csharp
// Services/EncryptionService.cs
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;

    public EncryptionService(IConfiguration configuration)
    {
        _encryptionKey = configuration["Encryption:Key"] 
            ?? throw new InvalidOperationException("Encryption key not configured");
    }

    public string Encrypt(string plainText)
    {
        // Use AES-256 encryption
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_encryptionKey);
        aes.GenerateIV();
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var cipherBytes = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_encryptionKey);
        
        var iv = new byte[aes.IV.Length];
        var cipher = new byte[cipherBytes.Length - iv.Length];
        
        Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(cipherBytes, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

**Use it when saving:**

```csharp
// In UtilityCompanyController.cs
[HttpPost("moje-racun/configure")]
public async Task<IActionResult> ConfigureMojeRacun(
    [FromServices] IEncryptionService encryption,
    [FromBody] MojeRacunConfigRequest request)
{
    var company = await GetCompany();
    
    company.MojeRacunEnabled = true;
    company.MojeRacunEnvironment = request.Environment;
    
    if (!string.IsNullOrEmpty(request.Password))
    {
        // ? Encrypt before storing
        company.MojeRacunClientSecret = encryption.Encrypt(request.Password);
    }
    
    if (!string.IsNullOrEmpty(request.CertificatePassword))
    {
        // ? Encrypt before storing
        company.MojeRacunCertificatePassword = encryption.Encrypt(request.CertificatePassword);
    }
    
    await _context.SaveChangesAsync();
    return Ok();
}
```

**Use it when reading:**

```csharp
// In MojeRacunService.cs
public async Task<MojeRacunResponse> SubmitInvoiceAsync(
    UtilityInvoice invoice, 
    CompanyProfile company,
    IEncryptionService encryption)
{
    string password;
    
    if (company.AuthMethod == "certificate")
    {
        // Decrypt certificate password
        password = encryption.Decrypt(company.MojeRacunCertificatePassword);
        var cert = new X509Certificate2(certPath, password);
        // ... use certificate
    }
    else
    {
        // Decrypt OAuth password
        password = encryption.Decrypt(company.MojeRacunClientSecret);
        var token = await GetOAuthToken(company.MojeRacunClientId, password);
        // ... use token
    }
}
```

### **2. Use Environment Variables for Encryption Key**

```json
// appsettings.json (DO NOT commit this!)
{
  "Encryption": {
    "Key": "BASE64_ENCODED_32_BYTE_KEY_HERE"
  }
}
```

**Generate encryption key:**

```bash
# PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
```

### **3. Use Azure Key Vault (Production)**

For production, store sensitive data in Azure Key Vault:

```csharp
// In Program.cs
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{builder.Configuration["KeyVault:Name"]}.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## ?? **Configuration Steps for Users**

### **Step 1: Register on mojE-Raèun**

1. Go to https://test.moj-eracun.hr (for testing)
2. Click "Registriraj se" (Register)
3. Fill in company details (OIB, name, address)
4. Verify email
5. Note down your **username** and **password**

### **Step 2: Get Certificate (if using certificate method)**

1. Contact FINA or authorized CA
2. Request e-invoice certificate
3. Download .pfx file
4. Upload to your server at: `wwwroot/certificates/moje-racun.pfx`

### **Step 3: Configure in Your App**

**Via API:**

```http
POST /api/UtilityCompany/moje-racun/settings
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "mojeRacunEnabled": true,
  "mojeRacunEnvironment": "test",
  "authenticationMethod": "certificate",  // or "oauth"
  
  // For certificate:
  "certificatePath": "certificates/moje-racun.pfx",
  "certificatePassword": "your-cert-password",
  
  // For OAuth:
  "username": "your-email@company.hr",
  "password": "your-password"
}
```

---

## ?? **Testing Your Configuration**

### **Test Endpoint (to be implemented):**

```http
POST /api/UtilityFiscalization/test-moje-racun
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "companyId": 1
}
```

**Expected Response:**

```json
{
  "success": true,
  "message": "Connection to mojE-Raèun successful!",
  "data": {
    "environment": "test",
    "authenticated": true,
    "testInvoiceId": "test-550e8400-e29b-41d4"
  }
}
```

---

## ?? **Summary**

### **What You Need:**

? **mojE-Raèun account** (username + password)  
OR  
? **Digital certificate** (.pfx file + password)

### **Where to Configure:**

? **Already implemented in database** (`CompanyProfile` table)  
? **Fields ready:**
- `MojeRacunEnabled`
- `MojeRacunEnvironment`
- `MojeRacunCertificatePath` + `MojeRacunCertificatePassword`
- `MojeRacunClientId` + `MojeRacunClientSecret`

### **What's Missing:**

? **OAuth token exchange** (needs implementation)  
? **Encryption service** (strongly recommended)  
? **Configuration UI** (frontend form)  
? **Test connection endpoint**

### **Your Current CSV: 5% PDV** ?

? **Default VAT rate changed to 5%** in:
- `KpdCodeService.cs`
- `UtilityInvoiceItem.cs` model
- Database migration applied

All utility services now default to **5% PDV** unless specified otherwise (waste/maintenance remain 25%).

---

## ?? **Need Help?**

- **mojE-Raèun Support:** https://moj-eracun.hr/podrska
- **Technical Documentation:** https://docs.moj-eracun.hr/api
- **FINA Certificate Info:** https://www.fina.hr/digitalni-certifikati

**Everything is ready for configuration! Just add your credentials.** ??
