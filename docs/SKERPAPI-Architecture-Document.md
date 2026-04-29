# Enterprise Internal PKI Manager — 開發手冊 (Developer Manual)

> **文件類型**：開發手冊（Developer Manual）  
> **目標讀者**：後端工程師、API 整合開發者  
> **版本**：v2.0.0  
> **最後更新**：2026-04-30  
> **平台**：.NET 10 / ASP.NET Core 10

---

## 目錄

1. [系統簡介](#1-系統簡介)
2. [系統安裝手冊](#2-系統安裝手冊)
   - 2.1 [前置需求](#21-前置需求)
   - 2.2 [使用 Docker Compose 快速啟動](#22-使用-docker-compose-快速啟動)
   - 2.3 [手動安裝（開發環境）](#23-手動安裝開發環境)
   - 2.4 [資料庫初始化](#24-資料庫初始化)
   - 2.5 [驗證安裝](#25-驗證安裝)
3. [系統組件功能說明](#3-系統組件功能說明)
   - 3.1 [Portal API](#31-portal-api)
   - 3.2 [Gateway API](#32-gateway-api)
   - 3.3 [Collector 代理程式](#33-collector-代理程式)
   - 3.4 [Windows Agent](#34-windows-agent)
   - 3.5 [Portal UI（Blazor）](#35-portal-uidblazorl)
4. [API 詳細說明](#4-api-詳細說明)
   - 4.1 [Portal API 端點](#41-portal-api-端點)
   - 4.2 [Gateway API 端點](#42-gateway-api-端點)
   - 4.3 [Windows Agent API 端點](#43-windows-agent-api-端點)
5. [認證與授權實作](#5-認證與授權實作)
6. [資料模型](#6-資料模型)
7. [設定檔參考](#7-設定檔參考)
8. [單元測試指引（TDD）](#8-單元測試指引tdd)
9. [錯誤處理規範](#9-錯誤處理規範)
10. [常見開發問題](#10-常見開發問題)

---

## 1. 系統簡介

Enterprise Internal PKI Manager（以下簡稱 **Enterprise PKI**）是一套企業內部 X.509 憑證生命週期管理平台，提供以下核心能力：

| 功能模組 | 說明 |
|----------|------|
| 憑證發行 | 透過 ADCS 簽發、追蹤 X.509 憑證 |
| 自動部署 | 將憑證推送至 Windows、Linux、F5 等目標主機 |
| 代理程式管理 | 監控分散式 Collector Agent 的存活狀態 |
| PQC 準備度 | 追蹤後量子密碼學（ML-KEM、ML-DSA）遷移進度 |
| 稽核日誌 | 全面記錄憑證操作，符合企業合規要求 |

### 系統服務清單

| 服務 | 類型 | 預設 Port | 職責 |
|------|------|-----------|------|
| **Portal** | ASP.NET Core Web API | 5069 | 業務邏輯、資料庫、API gateway |
| **Gateway** | ASP.NET Core Web API | 5176 | ADCS 整合邊界、簽發代理 |
| **Collector** | .NET Worker Service | N/A | 憑證探索、部署執行 |
| **WindowsAgent** | ASP.NET Core Minimal API | 8080 | ADCS COM/DCOM 代理 |
| **Portal.UI** | Blazor Server + WASM | 5261 | 管理介面 |

---

## 2. 系統安裝手冊

### 2.1 前置需求

#### 軟體需求

| 軟體 | 最低版本 | 用途 |
|------|----------|------|
| .NET SDK | 10.0 | 建置與執行所有服務 |
| Docker Desktop | 27.x | 容器化執行（可選） |
| Docker Compose | 2.x | 多服務協調啟動 |
| PostgreSQL | 16.x | Portal 主要資料庫 |
| Git | 2.x | 原始碼管理 |

#### 硬體需求（最低）

| 資源 | 開發環境 | 生產環境 |
|------|----------|----------|
| CPU | 2 vCPU | 4 vCPU |
| RAM | 4 GB | 8 GB |
| 磁碟 | 10 GB | 50 GB SSD |
| OS | Windows 10+ / Ubuntu 22.04+ | Windows Server 2022 / Ubuntu 22.04 |

#### Windows Agent 額外需求

- **OS**：Windows Server 2019 / 2022（必須 Windows，因 ADCS COM 介面依賴）
- **ADCS 客戶端工具**：已安裝 `certreq.exe`
- **.NET Runtime**：.NET 10 Windows Runtime

---

### 2.2 使用 Docker Compose 快速啟動

這是最快速的開發環境啟動方式，所有服務（包含 PostgreSQL）會一起啟動。

#### 步驟 1：複製儲存庫

```bash
git clone https://github.com/your-org/enterprise-internal-pki-manager.git
cd enterprise-internal-pki-manager
```

#### 步驟 2：設定環境變數

複製範本並填入實際值：

```bash
cp .env.example .env
```

`.env` 檔案內容：

```env
# Database
POSTGRES_PASSWORD=pki_password_change_me
POSTGRES_DB=pki

# Portal Auth Token (at least 32 chars)
PORTAL_API_AUTH_TOKEN=replace-with-min-32-char-random-string

# Gateway Service Token (shared secret)
GATEWAY_SERVICE_AUTH_TOKEN=replace-with-min-32-char-random-string

# CORS origins for Portal UI
PORTAL_CORS_ORIGINS=http://localhost:5261,https://localhost:7261
```

#### 步驟 3：啟動所有服務

```bash
docker-compose up -d
```

#### 步驟 4：確認服務健康

```bash
docker-compose ps
# 預期所有服務狀態為 running/healthy

curl http://localhost:5069/api/dashboard/stats \
  -H "Authorization: Bearer replace-with-min-32-char-random-string"
# 預期回傳 200 OK 含 JSON
```

---

### 2.3 手動安裝（開發環境）

適合需要單獨除錯各服務的情境。

#### 步驟 1：安裝 .NET 10 SDK

```bash
# Ubuntu/Debian
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0

# macOS
brew install --cask dotnet-sdk

# Windows (winget)
winget install Microsoft.DotNet.SDK.10
```

#### 步驟 2：安裝 PostgreSQL

```bash
# Ubuntu
sudo apt-get install -y postgresql-16
sudo systemctl enable postgresql --now

# 建立資料庫與使用者
sudo -u postgres psql << 'EOF'
CREATE DATABASE pki;
CREATE USER pkiuser WITH ENCRYPTED PASSWORD 'pki_password';
GRANT ALL PRIVILEGES ON DATABASE pki TO pkiuser;
\c pki
GRANT ALL ON SCHEMA public TO pkiuser;
EOF
```

#### 步驟 3：還原 NuGet 套件並建置

```bash
cd src
dotnet restore EnterprisePKI.sln
dotnet build EnterprisePKI.sln -c Release
```

#### 步驟 4：設定 appsettings

編輯每個服務的 `appsettings.Development.json`：

**Portal** (`src/Portal/appsettings.Development.json`)：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=pki;Username=pkiuser;Password=pki_password"
  },
  "Portal": {
    "ApiAuthToken": "your-portal-bearer-token-min-32-chars"
  },
  "Gateway": {
    "Url": "http://localhost:5176",
    "ServiceAuthToken": "your-gateway-service-token-min-32-chars"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5261"]
  }
}
```

**Gateway** (`src/Gateway/appsettings.Development.json`)：

```json
{
  "Gateway": {
    "ServiceAuthToken": "your-gateway-service-token-min-32-chars"
  },
  "AdcsProxy": {
    "Url": "http://windows-agent-host:8080"
  }
}
```

**Collector** (`src/Collector/appsettings.json`)：

```json
{
  "Portal": {
    "Url": "http://localhost:5069",
    "ApiAuthToken": "your-portal-bearer-token-min-32-chars"
  }
}
```

**Portal.UI** (`src/Portal.UI/Portal.UI/appsettings.json`)：

```json
{
  "PortalApi": {
    "BaseUrl": "http://localhost:5069",
    "AuthToken": "your-portal-bearer-token-min-32-chars"
  }
}
```

#### 步驟 5：啟動各服務

在不同終端機依序啟動：

```bash
# Terminal 1: Gateway
cd src/Gateway && dotnet run

# Terminal 2: Portal (等 Gateway 啟動後)
cd src/Portal && dotnet run

# Terminal 3: Portal.UI
cd src/Portal.UI/Portal.UI && dotnet run

# Terminal 4 (可選): Collector
cd src/Collector && dotnet run

# Terminal 5 (Windows 只): Windows Agent
cd src/WindowsAgent && dotnet run
```

---

### 2.4 資料庫初始化

Portal 啟動時會自動執行資料庫 migration（若 Schema 尚未存在）。種子資料透過 SQL 腳本載入：

```bash
# 手動執行初始化腳本（若需要）
psql -h localhost -U pkiuser -d pki -f src/Portal/Schema/init.sql
```

**init.sql 建立的表格**：

| 資料表 | 說明 |
|--------|------|
| `Certificates` | X.509 憑證主表 |
| `Endpoints` | Collector 代理程式登記表 |
| `CertificateDeployments` | 憑證與端點的關聯（已部署紀錄） |
| `DeploymentJobs` | 待執行/執行中/已完成的部署作業 |
| `CertificateRequests` | CSR 簽發申請紀錄 |

---

### 2.5 驗證安裝

執行以下測試確認安裝成功：

```bash
cd src

# 執行 Portal 單元測試
dotnet test Portal.Tests/Portal.Tests.csproj

# 執行 Gateway 單元測試
dotnet test Gateway.Tests/Gateway.Tests.csproj

# 執行 Collector 單元測試
dotnet test Collector.Tests/Collector.Tests.csproj

# 執行整合測試（需要 Docker）
dotnet test Integration.Tests/Integration.Tests.csproj

# 確認完整建置
dotnet build EnterprisePKI.sln
```

**預期結果**：
- Portal.Tests: 12/12 通過
- Gateway.Tests: 21/21 通過
- Collector.Tests: 19/19 通過
- Integration.Tests: 21/21 通過

---

## 3. 系統組件功能說明

### 3.1 Portal API

**路徑**：`src/Portal/`  
**職責**：業務邏輯核心，負責協調所有 PKI 操作。

#### 功能模組

##### 3.1.1 憑證管理（Certificates）

`CertificatesController` 實作完整的 CRUD 與簽發流程：

```
憑證簽發流程：
Client → POST /api/certificates/request
       → Portal 建立 CertificateRequest 記錄（Status=Pending）
       → Portal → POST /api/ca/issue（Gateway）
       → Gateway → ADCS（via Windows Agent）
       → Certificate 回傳 → 儲存至 DB
       → 201 Created（含 Location header）
```

**關鍵程式邏輯**：
- `GatewayService.RequestIssuanceAsync()` 負責 HTTP 呼叫 Gateway，並自動附加 Bearer Token
- 如果 Gateway 返回錯誤，Request 狀態維持 `Pending` 供後續重試
- `RawData`（PFX 二進位）**不會**在 GET 回應中傳回（安全考量）

##### 3.1.2 部署管理（Deployments）

`DeploymentsController` 管理 DeploymentJob 的整個生命週期：

```
部署流程：
1. 管理員建立 DeploymentJob → Status=Pending
2. PFX 資料透過 DeploymentJobSecretMapper 加密後存入 DB
3. Collector 輪詢 GET /api/deployments/jobs/{hostname} 取得待辦作業
4. Collector 執行部署（將 PFX 安裝至目標憑證庫）
5. Collector POST /api/deployments/jobs/{id}/status 更新狀態
6. 若失敗，ErrorMessage 記錄失敗原因
```

**PFX 安全機制**：

| 存取場景 | `DeploymentJobSecretMapper` 方法 | 效果 |
|----------|----------------------------------|------|
| 存入資料庫 | `ForStorage()` | 加密 PfxData + PfxPassword |
| 傳給 Collector | `ForCollector()` | 解密供代理程式使用 |
| 傳給 UI | `ForUiList()` | 清空（不傳送至瀏覽器） |

##### 3.1.3 代理程式管理（Agents）

`AgentsController` 管理所有 Collector 代理程式的登記狀態：

- 列出所有已登記的 Endpoint（含最後心跳時間）
- `Online` 狀態 = `LastHeartbeat > UTC - 5 minutes`
- Collector 定期呼叫 Portal 更新 LastHeartbeat

##### 3.1.4 儀表板統計（Dashboard）

`DashboardController.GetStats()` 聚合以下指標：

| 統計項目 | 計算方式 |
|----------|----------|
| TotalCertificates | `COUNT(*) FROM Certificates` |
| ExpiringSoon | `COUNT(*) WHERE NotAfter BETWEEN NOW() AND NOW()+30days` |
| ActiveAgents | `COUNT(*) WHERE LastHeartbeat > NOW()-5min FROM Endpoints` |
| PqcReadyCertificates | `COUNT(*) WHERE IsPQC=true FROM Certificates` |

##### 3.1.5 安全探針（Security）

`SecurityController.Probe()` 提供認證測試端點，返回 `{Status: "Authorized"}`，供 Collector 和 UI 確認 Token 有效性。

---

#### 3.1.6 中介軟體管道（Middleware Pipeline）

Portal 的請求管道順序（順序重要）：

```
Request 進入
     ↓
[1] CorrelationIdMiddleware     → 設定 X-Correlation-ID
     ↓
[2] SecurityHeadersMiddleware   → 設定 HTTP 安全回應標頭
     ↓
[3] GlobalExceptionHandler      → 捕捉未處理例外，回傳 ApiError
     ↓
[4] HTTPS Redirection           → 強制 HTTPS
     ↓
[5] CORS                        → 允許 UI 跨域
     ↓
[6] RateLimiting                → 限流（60 req/60s 預設）
     ↓
[7] Authentication              → PortalApiBearer Token 驗證
     ↓
[8] Authorization               → [Authorize] 檢查
     ↓
[9] Controllers                 → 業務邏輯執行
```

---

### 3.2 Gateway API

**路徑**：`src/Gateway/`  
**職責**：ADCS 整合邊界，隔離 CA 操作風險。

#### 功能說明

Gateway 是 Portal 與 ADCS 之間的唯一通道，實作以下職責：

1. **驗證請求**：確認來自 Portal 的 Bearer Token 合法
2. **速率限制**：防止 CA 被超載（30 req/60s）
3. **代理轉發**：將 CSR 轉發至 Windows Agent（或本地 mock）
4. **回應標準化**：將 ADCS 回應轉換為統一 Certificate 物件

#### ADCS 代理邏輯（AdcsGatewayService）

```csharp
// 決策邏輯
if (AdcsProxy:Url 已設定) {
    POST {proxy_url}/api/adcs/submit  // 轉發給 Windows Agent
    return 解析後的 Certificate
} else {
    return Mock Certificate  // 開發/測試用
}
```

**輸入 CSR 格式**：PEM 格式（`-----BEGIN CERTIFICATE REQUEST-----`）  
**回傳 Certificate 欄位**：SerialNumber、Thumbprint、CommonName、IssuerDN、NotBefore、NotAfter、CertificateBase64

#### 速率限制（GatewayIssueRequestThrottle）

- **Partition key**：`{Subject}:{X-Client-Id}` 複合鍵
- **限制**：每 partition 30 次 / 60 秒
- **超過**：回傳 `429 Too Many Requests`

---

### 3.3 Collector 代理程式

**路徑**：`src/Collector/`  
**職責**：在目標主機執行憑證探索與部署。

#### 工作循環（CollectorWorker）

Collector 以背景服務形式執行，每 ~60 分鐘完成一個完整循環：

```
[Start]
   │
   ▼
1. WindowsDiscoveryService.DiscoverAsync()
   └─ 掃描 Windows 憑證庫（LocalMachine\My, Root 等）
   └─ 回傳 List<CertificateDiscovery>
   │
   ▼
2. ReportingService.ReportDiscoveryAsync(discoveries)
   └─ POST /api/certificates/discovery → Portal
   │
   ▼
3. ReportingService.GetPendingJobsAsync(hostname)
   └─ GET /api/deployments/jobs/{hostname} → Portal
   │
   ▼
4. For each pending job:
   WindowsDeploymentService.InstallCertificateAsync(job)
   └─ X509CertificateLoader.LoadPkcs12(pfxBytes, password)
   └─ X509Store.Add(certificate)
   │
   ▼
5. ReportingService.UpdateJobStatusAsync(jobId, status)
   └─ POST /api/deployments/jobs/{id}/status → Portal
   │
   ▼
[Wait 60 min] → [Repeat]
```

#### 憑證探索（WindowsDiscoveryService）

```csharp
// 掃描所有 Windows 憑證庫位置
foreach (var storeName in StoreNames) {
    using var store = new X509Store(storeName, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadOnly);
    foreach (var cert in store.Certificates) {
        discoveries.Add(new CertificateDiscovery {
            Thumbprint = cert.Thumbprint,
            CommonName = cert.Subject,
            NotBefore  = cert.NotBefore,
            NotAfter   = cert.NotAfter
        });
    }
}
```

#### 憑證部署（WindowsDeploymentService）

```csharp
// StoreLocation 格式："StoreName/StoreLocationEnum"
// 例："My/LocalMachine" 或 "Root/LocalMachine"
var parts = job.StoreLocation.Split('/');
var storeName = parts[0];         // e.g. "My"
var storeLocation = parts[1];     // e.g. "LocalMachine"

var pfxBytes = Convert.FromBase64String(job.PfxData);
var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, job.PfxPassword);
using var store = new X509Store(storeName, Enum.Parse<StoreLocation>(storeLocation));
store.Open(OpenFlags.ReadWrite);
store.Add(cert);
```

#### CSR 生成（CertificateRequestService）

```csharp
// 使用 RSA-2048 + SHA256 生成 CSR
using var rsa = RSA.Create(2048);
var request = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256,
    RSASignaturePadding.Pkcs1);
var pem = request.CreateSigningRequestPem();
// 透過 ReportingService 提交至 Portal
```

---

### 3.4 Windows Agent

**路徑**：`src/WindowsAgent/`  
**職責**：在 Windows 主機上代理 ADCS COM 介面呼叫。

**重要**：此服務必須在 Windows 上執行（程式啟動時會驗證）：

```csharp
if (!OperatingSystem.IsWindows())
    return BadRequest("This agent only runs on Windows.");
```

#### ADCS 提交端點

```
POST /api/adcs/submit
Body: { "Csr": "<PEM>", "Template": "WebServer", "CaConfig": "CA01\\My-Enterprise-CA" }
Response: { "SerialNumber": "...", "Thumbprint": "...", "CertificateBase64": "..." }
```

**生產實作**（待完成）：使用 `CERTCLILib.CCertRequest` COM 物件：

```csharp
// 計畫實作
var certRequest = new CCertRequest();
var result = certRequest.Submit(
    CR_IN_BASE64 | CR_IN_PKCS10,
    csrPem,
    attributeString,
    caConfig);
```

---

### 3.5 Portal UI（Blazor）

**路徑**：`src/Portal.UI/`  
**技術**：Blazor Server + WebAssembly（InteractiveAuto 渲染模式）

#### 頁面結構

| 頁面 | Route | 功能 |
|------|-------|------|
| Home | `/` | 儀表板（統計卡片、健康指標、快速操作） |
| Certificates | `/certificates` | 憑證清單、搜尋、申請新憑證 |
| Deployments | `/deployments` | 部署作業管理 |
| Agents | `/agents` | 代理程式監控 |

#### API 客戶端（PkiApiClient）

所有 API 呼叫透過 `PkiApiClient` 集中管理：

```csharp
// 範例：取得儀表板統計
var stats = await _pkiApiClient.GetStatsAsync();
// GET /api/dashboard/stats + Authorization: Bearer {token}

// 範例：取得憑證列表
var result = await _pkiApiClient.GetCertificatesAsync(page: 1, pageSize: 20);
// GET /api/certificates?page=1&pageSize=20
```

---

## 4. API 詳細說明

### 4.1 Portal API 端點

**Base URL**: `http://localhost:5069`  
**認證**: `Authorization: Bearer {Portal:ApiAuthToken}`  
**回應格式**: `application/json`

---

#### GET /api/certificates

列出所有受管理憑證（分頁）。

**Query Parameters**:

| 參數 | 型別 | 預設值 | 說明 |
|------|------|--------|------|
| `page` | int | 1 | 頁碼（>= 1） |
| `pageSize` | int | 20 | 每頁筆數（1–200） |

**成功回應** `200 OK`:
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "commonName": "web01.internal.corp",
      "serialNumber": "4A00B3C2",
      "thumbprint": "A1B2C3D4E5F6...",
      "issuerDN": "CN=Enterprise-CA, DC=internal, DC=corp",
      "notBefore": "2025-01-01T00:00:00Z",
      "notAfter": "2026-01-01T00:00:00Z",
      "algorithm": "RSA-2048",
      "keySize": 2048,
      "isPQC": false,
      "status": "Active"
    }
  ],
  "totalCount": 125,
  "page": 1,
  "pageSize": 20
}
```

**注意**: `rawData` 欄位**不在**回應中（安全性考量）。

---

#### GET /api/certificates/{id}

取得單一憑證詳細資訊。

**路徑參數**: `id` (GUID)

**成功回應** `200 OK`: 同上 Certificate 物件

**失敗回應** `404 Not Found`:
```json
{
  "errorCode": "CERT_NOT_FOUND",
  "message": "Certificate {id} was not found."
}
```

---

#### POST /api/certificates

直接建立憑證記錄（用於匯入現有憑證）。

**請求 Body**:
```json
{
  "commonName": "api.internal.corp",
  "serialNumber": "4A00B3C3",
  "thumbprint": "B1C2D3E4F5G6...",
  "issuerDN": "CN=Enterprise-CA",
  "notBefore": "2025-01-01T00:00:00Z",
  "notAfter": "2026-01-01T00:00:00Z",
  "algorithm": "RSA-4096",
  "keySize": 4096,
  "isPQC": false
}
```

**成功回應** `201 Created`:
- Header `Location: /api/certificates/{new-id}`
- Body: Certificate 物件

---

#### POST /api/certificates/discovery

Collector 代理程式回報探索到的憑證。

**請求 Body**:
```json
{
  "agentHostname": "WIN-SERVER-01",
  "discoveries": [
    {
      "thumbprint": "A1B2C3...",
      "commonName": "CN=web01.internal.corp",
      "notBefore": "2025-01-01T00:00:00Z",
      "notAfter": "2026-01-01T00:00:00Z",
      "storeName": "My",
      "storeLocation": "LocalMachine"
    }
  ]
}
```

**成功回應** `200 OK`:
```json
{ "message": "Discovery report processed. 5 certificates updated." }
```

**驗證失敗** `400 Bad Request`:
```json
{
  "errorCode": "VALIDATION_ERROR",
  "message": "AgentHostname is required."
}
```

---

#### POST /api/certificates/request

提交 CSR 憑證簽發申請。

**請求 Body**:
```json
{
  "requester": "admin@internal.corp",
  "csr": "-----BEGIN CERTIFICATE REQUEST-----\nMIIC...\n-----END CERTIFICATE REQUEST-----",
  "templateName": "WebServer"
}
```

**Template 可選值**:
- `WebServer` - 標準 Web 伺服器憑證
- `PQCWebServer` - 後量子密碼學 Web 憑證
- `CodeSigning` - 程式碼簽章憑證
- `ClientAuth` - 用戶端認證憑證

**成功回應** `201 Created`:
- Header `Location: /api/certificates/requests/{id}`
- Body: `{ "id": "...", "status": "Pending" }`

---

#### GET /api/deployments/jobs

列出所有部署作業（分頁）。

**Query Parameters**: `page`, `pageSize`（同上）

**成功回應** `200 OK`:
```json
{
  "items": [
    {
      "id": "...",
      "certificateId": "...",
      "targetHostname": "WIN-SERVER-01",
      "storeLocation": "My/LocalMachine",
      "status": "Pending",
      "errorMessage": null,
      "createdAt": "2026-04-30T10:00:00Z",
      "completedAt": null
    }
  ],
  "totalCount": 15
}
```

**注意**: `pfxData` 和 `pfxPassword` **不會**出現在 UI 回應中。

---

#### GET /api/deployments/jobs/{hostname}

Collector 呼叫此端點取得指定主機的待執行作業。

**路徑參數**: `hostname` (string)

**成功回應** `200 OK`:
```json
[
  {
    "id": "...",
    "targetHostname": "WIN-SERVER-01",
    "storeLocation": "My/LocalMachine",
    "status": "Pending",
    "pfxData": "<Base64-encoded-PFX>",
    "pfxPassword": "pfx-pass"
  }
]
```

**此端點的 pfxData 已解密**（`DeploymentJobSecretMapper.ForCollector()`）。

---

#### POST /api/deployments/jobs/{id}/status

更新部署作業狀態。

**請求 Body**:
```json
{
  "status": "Completed",
  "errorMessage": null
}
```

**Status 可選值**: `Pending` | `InProgress` | `Completed` | `Failed`

---

#### GET /api/agents

列出所有 Collector 代理程式。

**成功回應** `200 OK`:
```json
{
  "items": [
    {
      "id": "...",
      "hostname": "WIN-SERVER-01",
      "ipAddress": "192.168.1.101",
      "type": "Windows",
      "lastHeartbeat": "2026-04-30T09:55:00Z",
      "isOnline": true
    }
  ],
  "totalCount": 5
}
```

**`isOnline` 計算**: `LastHeartbeat > NOW() - 5 minutes`

---

#### GET /api/dashboard/stats

取得儀表板統計數據。

**成功回應** `200 OK`:
```json
{
  "totalCertificates": 125,
  "expiringSoon": 3,
  "activeAgents": 5,
  "pqcReadyCertificates": 12
}
```

---

### 4.2 Gateway API 端點

**Base URL**: `http://localhost:5176`  
**認證**: `Authorization: Bearer {Gateway:ServiceAuthToken}`（必須含 scope: `gateway.issue`）

---

#### POST /api/ca/issue

簽發新憑證。

**請求 Header**:
```
Authorization: Bearer {token}
X-Client-Id: portal-service-01
```

**請求 Body**:
```json
{
  "csr": "-----BEGIN CERTIFICATE REQUEST-----\nMIIC...\n-----END CERTIFICATE REQUEST-----",
  "templateName": "WebServer"
}
```

**成功回應** `200 OK`:
```json
{
  "id": "...",
  "commonName": "web01.internal.corp",
  "serialNumber": "4A00B3C2",
  "thumbprint": "A1B2C3D4E5F6",
  "issuerDN": "CN=Enterprise-CA",
  "notBefore": "2026-04-30T00:00:00Z",
  "notAfter": "2027-04-30T00:00:00Z",
  "algorithm": "RSA-2048",
  "certificateBase64": "<Base64 DER>"
}
```

**速率限制超過** `429 Too Many Requests`:
```json
{
  "errorCode": "RATE_LIMIT_EXCEEDED",
  "message": "Too many certificate issuance requests. Retry after 60 seconds."
}
```

---

#### POST /api/ca/revoke

撤銷憑證。

**請求 Body**:
```json
{
  "serialNumber": "4A00B3C2",
  "reason": "KeyCompromise"
}
```

**Reason 可選值**: `Unspecified` | `KeyCompromise` | `CACompromise` | `AffiliationChanged` | `Superseded` | `CessationOfOperation`

**成功回應** `200 OK`:
```json
{
  "serialNumber": "4A00B3C2",
  "status": "Revoked"
}
```

---

### 4.3 Windows Agent API 端點

**Base URL**: `http://windows-agent-host:8080`  
**備注**: 僅供 Gateway 內部呼叫，不對外公開

---

#### POST /api/adcs/submit

將 CSR 提交至 ADCS。

**請求 Body**:
```json
{
  "csr": "-----BEGIN CERTIFICATE REQUEST-----\nMIIC...\n-----END CERTIFICATE REQUEST-----",
  "template": "WebServer",
  "caConfig": "WIN-CA01\\Enterprise-CA"
}
```

**成功回應** `200 OK`:
```json
{
  "serialNumber": "4A00B3C2",
  "thumbprint": "A1B2C3D4E5F6",
  "commonName": "web01.internal.corp",
  "issuerDN": "CN=Enterprise-CA, DC=internal, DC=corp",
  "notBefore": "2026-04-30T00:00:00Z",
  "notAfter": "2027-04-30T00:00:00Z",
  "certificateBase64": "<Base64 DER>"
}
```

---

## 5. 認證與授權實作

### 5.1 Custom Bearer Handler 架構

本系統採用**靜態共享 Token**（Static Bearer Token）而非 JWT/OAuth，適合封閉網路內部服務通訊：

```csharp
// PortalApiBearerAuthenticationHandler 核心邏輯
protected override Task<AuthenticateResult> HandleAuthenticateAsync()
{
    var authHeader = Request.Headers.Authorization.ToString();
    if (!authHeader.StartsWith("Bearer "))
        return Task.FromResult(AuthenticateResult.Fail("Missing Bearer token"));

    var token = authHeader["Bearer ".Length..].Trim();
    var expectedToken = _config["Portal:ApiAuthToken"];

    if (token != expectedToken)
        return Task.FromResult(AuthenticateResult.Fail("Invalid token"));

    var claims = new[] { new Claim(ClaimTypes.Name, "PortalApiClient") };
    var identity = new ClaimsIdentity(claims, "PortalApiBearer");
    return Task.FromResult(AuthenticateResult.Success(
        new AuthenticationTicket(new ClaimsPrincipal(identity), "PortalApiBearer")));
}
```

### 5.2 Gateway 額外 Scope 驗證

Gateway 除 Token 外還驗證 `scope` claim：

```csharp
// GatewayServiceBearerAuthenticationHandler 額外新增 scope claim
claims.Add(new Claim("scope", "gateway.issue"));

// CaController 使用 GatewayIssuePolicy
[Authorize(Policy = "GatewayIssuePolicy")]
// Policy 定義：requires scope == "gateway.issue"
```

### 5.3 Token 安全注意事項

- Token 長度**至少 32 字元**（建議 64 字元以上）
- 生產環境必須使用環境變數或 Secret Manager，**不可硬寫在程式碼中**
- 定期輪換（建議 90 天）
- Portal ↔ Gateway 使用**不同** token

---

## 6. 資料模型

### 6.1 Certificate（憑證）

```csharp
public class Certificate
{
    public Guid Id { get; set; }
    public string CommonName { get; set; }        // CN=web01.internal.corp
    public string SerialNumber { get; set; }      // HEX 格式 CA 序號
    public string Thumbprint { get; set; }        // SHA-1 指紋（唯一索引）
    public string IssuerDN { get; set; }          // 發行者 DN
    public DateTime NotBefore { get; set; }       // 生效日期（UTC）
    public DateTime NotAfter { get; set; }        // 到期日期（UTC）
    public string Algorithm { get; set; }         // "RSA-2048"/"RSA-4096"/"ML-KEM-768"/"ML-DSA-65"
    public int KeySize { get; set; }              // 金鑰長度（bit）
    public bool IsPQC { get; set; }              // 是否為後量子密碼學憑證
    public byte[]? RawData { get; set; }         // 原始 DER/PFX（敏感，不回傳 API）
    public string Status { get; set; }           // Active / Revoked / Expired
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 6.2 DeploymentJob（部署作業）

```csharp
public class DeploymentJob
{
    public Guid Id { get; set; }
    public Guid CertificateId { get; set; }
    public string TargetHostname { get; set; }    // 目標主機名稱
    public string StoreLocation { get; set; }     // "My/LocalMachine" 或 "/etc/pki/tls/"
    public string Status { get; set; }            // Pending/InProgress/Completed/Failed
    public string? ErrorMessage { get; set; }     // 失敗原因
    public string? PfxData { get; set; }          // Base64 PFX（加密存儲）
    public string? PfxPassword { get; set; }      // PFX 密碼（加密存儲）
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

### 6.3 Agent（代理程式）

```csharp
public class Agent
{
    public Guid Id { get; set; }
    public string Hostname { get; set; }          // 主機名稱
    public string? IPAddress { get; set; }        // IP 位址
    public string Type { get; set; }              // Windows/Linux/F5/JKS/K8s
    public DateTime? LastHeartbeat { get; set; }  // 最後心跳時間（UTC）
    public bool IsOnline                          // 計算屬性
        => LastHeartbeat > DateTime.UtcNow - TimeSpan.FromMinutes(5);
}
```

### 6.4 PaginatedResult\<T\>（分頁結果）

```csharp
public class PaginatedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
```

---

## 7. 設定檔參考

### 7.1 Portal（src/Portal/appsettings.json）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=pki;Username=postgres;Password=pki_password"
  },
  "Portal": {
    "ApiAuthToken": "replace-with-long-random-token",
    "RateLimiting": {
      "PermitLimit": 60,
      "WindowSeconds": 60
    }
  },
  "Gateway": {
    "Url": "http://localhost:5176",
    "ServiceAuthToken": "replace-with-shared-service-token"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5261", "https://localhost:7261"]
  }
}
```

### 7.2 Gateway（src/Gateway/appsettings.json）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Gateway": {
    "ServiceAuthToken": "replace-with-shared-service-token",
    "RateLimiting": {
      "PermitLimit": 30,
      "WindowSeconds": 60
    }
  },
  "AdcsProxy": {
    "Url": "http://windows-agent-host:8080"
  }
}
```

### 7.3 Collector（src/Collector/appsettings.json）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Portal": {
    "Url": "http://localhost:5069",
    "ApiAuthToken": "replace-with-long-random-token"
  }
}
```

### 7.4 Portal.UI（src/Portal.UI/Portal.UI/appsettings.json）

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PortalApi": {
    "BaseUrl": "http://localhost:5069",
    "AuthToken": "replace-with-long-random-token"
  }
}
```

---

## 8. 單元測試指引（TDD）

本專案嚴格遵循 **Red-Green-Refactor** 循環：

### 8.1 測試框架

| 測試類型 | 框架 | 套件 |
|----------|------|------|
| 單元測試 | xUnit + Moq + FluentAssertions | Portal.Tests, Gateway.Tests, Collector.Tests |
| 整合測試 | xUnit + WebApplicationFactory + Testcontainers | Integration.Tests |
| E2E 測試 | NUnit + Microsoft.Playwright.NUnit | E2E.Tests |

### 8.2 TDD 工作流程

```
1. RED：先寫一個最小的失敗測試
   ↓
2. 執行確認測試確實失敗（且為預期原因）
   ↓
3. GREEN：寫最少的生產程式碼使測試通過
   ↓
4. 執行確認測試通過
   ↓
5. REFACTOR：重構（保持綠燈）
   ↓
6. 提交（commit message 含測試名稱）
```

### 8.3 測試範例

#### Gateway 速率限制測試

```csharp
[Fact]
public async Task Issue_RateLimitExceeded_Returns429()
{
    // Arrange
    var throttle = new Mock<IGatewayIssueRequestThrottle>();
    throttle.Setup(t => t.TryAcquire(It.IsAny<string>())).Returns(false);
    var controller = new CaController(_caService.Object, throttle.Object, _logger.Object);

    // Act
    var result = await controller.Issue(new IssueRequest { Csr = "valid-csr", TemplateName = "WebServer" });

    // Assert
    result.Should().BeOfType<ObjectResult>()
        .Which.StatusCode.Should().Be(429);
}
```

#### Collector 部署服務測試

```csharp
[Fact]
public async Task InstallCertificateAsync_EmptyPfxData_ThrowsArgumentException()
{
    // Arrange
    var service = new WindowsDeploymentService(_logger.Object);
    var job = new DeploymentJob { PfxData = "", StoreLocation = "My/LocalMachine" };

    // Act + Assert
    await Assert.ThrowsAsync<ArgumentException>(
        () => service.InstallCertificateAsync(job));
}
```

### 8.4 執行測試

```bash
# 執行指定測試專案
dotnet test src/Portal.Tests/Portal.Tests.csproj --verbosity normal

# 執行所有單元測試
dotnet test src/EnterprisePKI.sln

# 只執行特定測試類別
dotnet test src/Gateway.Tests/ --filter "FullyQualifiedName~CaControllerTests"

# 帶覆蓋率報告
dotnet test src/EnterprisePKI.sln --collect:"XPlat Code Coverage"
```

---

## 9. 錯誤處理規範

### 9.1 統一錯誤格式（ApiError）

所有錯誤回應必須使用統一格式：

```json
{
  "errorCode": "CERT_NOT_FOUND",
  "message": "Certificate 3fa85f64-5717-4562-b3fc-2c963f66afa6 was not found.",
  "correlationId": "abc123"
}
```

### 9.2 HTTP 狀態碼規範

| 場景 | 狀態碼 | errorCode 範例 |
|------|--------|----------------|
| 成功（查詢） | 200 | — |
| 成功（建立） | 201 | — |
| 請求格式錯誤 | 400 | `VALIDATION_ERROR` |
| 未認證 | 401 | `UNAUTHORIZED` |
| 未授權 | 403 | `FORBIDDEN` |
| 資源不存在 | 404 | `NOT_FOUND` |
| 速率限制超過 | 429 | `RATE_LIMIT_EXCEEDED` |
| 內部錯誤 | 500 | `INTERNAL_ERROR` |

### 9.3 Global Exception Handler

```csharp
// 全域例外處理（不洩露堆疊追蹤）
app.UseExceptionHandler(appError => {
    appError.Run(async context => {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var correlationId = context.Request.Headers["X-Correlation-ID"].ToString();
        await context.Response.WriteAsJsonAsync(new ApiError {
            ErrorCode = "INTERNAL_ERROR",
            Message = "An unexpected error occurred.",
            CorrelationId = correlationId
        });
    });
});
```

---

## 10. 常見開發問題

### Q1: 執行整合測試失敗（找不到 Docker）

**解決**：整合測試使用 Testcontainers，需要 Docker Desktop 執行中。

```bash
# 確認 Docker 執行狀態
docker ps

# 若未執行，啟動 Docker Desktop 後重試
dotnet test src/Integration.Tests/Integration.Tests.csproj
```

---

### Q2: Gateway 回傳 Mock 憑證

**原因**：`AdcsProxy:Url` 未設定，Gateway 會自動回退至 mock 模式。

**解決**：在 Gateway 的 appsettings 設定 Windows Agent URL：

```json
{
  "AdcsProxy": {
    "Url": "http://your-windows-agent:8080"
  }
}
```

---

### Q3: E2E 測試失敗（ERR_CONNECTION_REFUSED）

**原因**：E2E 測試需要 Portal.UI 執行於 `http://localhost:5261`。

**解決**：

```bash
# 先啟動 Portal.UI
cd src/Portal.UI/Portal.UI && dotnet run

# 等待就緒後執行 E2E 測試
dotnet test src/E2E.Tests/E2E.Tests.csproj
```

---

### Q4: 401 Unauthorized 錯誤

**原因**：Bearer Token 不正確或格式錯誤。

**解決**：

```bash
# 確認 Token 值
curl http://localhost:5069/api/security/probe \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# 應回傳: {"status": "Authorized"}
```

確認 Portal `appsettings.json` 中 `Portal:ApiAuthToken` 與請求 Header 一致。

---

### Q5: Blazor UI 畫面空白

**原因**：`app.MapStaticAssets()` 必須在 `app.UseStaticFiles()` 之前呼叫（Blazor 靜態資源載入問題）。

**解決**：確認 `Program.cs` 中的順序：

```csharp
app.MapStaticAssets();     // 必須在前
app.UseStaticFiles();      // 在後
```
