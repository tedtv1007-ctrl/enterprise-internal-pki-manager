# Enterprise Internal PKI Manager — 系統架構設計文件 (System Architecture Design)

> **文件類型**：系統架構設計文件（System Architecture Design Document）  
> **目標讀者**：系統架構師、技術主管、資安工程師  
> **版本**：v2.0.0  
> **最後更新**：2026-04-30  
> **系統代號**：Enterprise PKI Manager

---

## 目錄

1. [系統概述與架構目標](#1-系統概述與架構目標)
2. [架構概覽](#2-架構概覽)
3. [元件架構設計](#3-元件架構設計)
   - 3.1 [Portal（業務核心）](#31-portal業務核心)
   - 3.2 [Gateway（CA 邊界）](#32-gatewayca-邊界)
   - 3.3 [Collector（分散式代理）](#33-collector分散式代理)
   - 3.4 [Windows Agent（ADCS 代理）](#34-windows-agentadcs-代理)
   - 3.5 [Portal.UI（管理介面）](#35-portalui管理介面)
4. [安全架構](#4-安全架構)
5. [資料架構](#5-資料架構)
6. [整合架構](#6-整合架構)
7. [部署架構](#7-部署架構)
8. [系統操作手冊](#8-系統操作手冊)
9. [可擴展性設計](#9-可擴展性設計)
10. [後量子密碼學（PQC）策略](#10-後量子密碼學pqc策略)
11. [架構決策記錄（ADR）](#11-架構決策記錄adr)

---

## 1. 系統概述與架構目標

### 1.1 系統定位

Enterprise Internal PKI Manager 是企業內部 X.509 憑證生命週期管理平台，解決以下核心問題：

- **憑證分散管理**：企業環境中憑證散落於多個伺服器、負載平衡器和應用系統
- **到期風險**：人工追蹤憑證到期導致服務中斷風險
- **安全合規**：PFX 私鑰保護、稽核日誌、存取控制不一致
- **PQC 遷移**：組織需逐步遷移至後量子密碼學演算法

### 1.2 架構目標

| 品質屬性 | 目標 | 衡量指標 |
|----------|------|----------|
| **安全性** | 最小權限、金鑰加密、稽核追蹤 | 零 PFX 明文暴露；所有端點 [Authorize] |
| **可靠性** | Agent 故障不影響 Portal | Collector 離線不影響 UI 和 API |
| **可擴展性** | 支援 1000+ 受管端點 | PostgreSQL 索引優化；分頁 API |
| **可維護性** | 清晰的服務邊界 | Gateway 隔離 CA 實作；接口隔離 |
| **可觀測性** | 完整稽核日誌和請求追蹤 | X-Correlation-ID；PKI_AUDIT 前綴日誌 |

---

## 2. 架構概覽

### 2.1 整體架構圖

```
┌─────────────────────────────────────────────────────────────────────┐
│                     Enterprise Network                               │
│                                                                      │
│  ┌──────────────┐         ┌──────────────┐                          │
│  │  Portal.UI   │ ──HTTP──▶  Portal API  │                          │
│  │  (Blazor)    │         │  (Port 5069) │                          │
│  │  Port 5261   │◀──JSON──│   PostgreSQL │                          │
│  └──────────────┘         └──────┬───────┘                          │
│                                  │ Bearer Token                      │
│                                  ▼                                   │
│                         ┌────────────────┐                          │
│                         │  Gateway API   │                          │
│                         │  (Port 5176)   │                          │
│                         │  ADCS Boundary │                          │
│                         └───────┬────────┘                          │
│                                 │ HTTP Proxy                         │
│                                 ▼                                    │
│                    ┌────────────────────────┐                       │
│                    │   Windows Agent        │                       │
│                    │   (Port 8080)          │                       │
│                    │   ADCS COM Interface   │                       │
│                    └───────────┬────────────┘                       │
│                                │ DCOM/RPC                            │
│                                ▼                                     │
│                    ┌────────────────────────┐                       │
│                    │  Active Directory       │                       │
│                    │  Certificate Services  │                       │
│                    │  (Enterprise CA)        │                       │
│                    └────────────────────────┘                       │
│                                                                      │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐            │
│  │  Collector  │    │  Collector  │    │  Collector  │  ···       │
│  │  WIN-01     │    │  WIN-02     │    │  LINUX-01   │            │
│  │  (Agent)    │    │  (Agent)    │    │  (Agent)    │            │
│  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘            │
│         └──────────────────┴──────────────────┘                    │
│                             │ Bearer Token                          │
│                             ▼                                       │
│                    ┌────────────────────────┐                       │
│                    │      Portal API         │                       │
│                    │   (Discovery/Deploy)   │                       │
│                    └────────────────────────┘                       │
└─────────────────────────────────────────────────────────────────────┘
```

### 2.2 架構模式

| 模式 | 應用場景 | 實作位置 |
|------|----------|----------|
| **Hub-and-Spoke** | Portal 為中心節點，Collector 為輻條 | Portal ↔ Collectors |
| **Facade** | Gateway 隔離 ADCS 複雜性 | Gateway ↔ ADCS |
| **Proxy** | Windows Agent 代理 COM 呼叫 | WindowsAgent ↔ ADCS |
| **Repository** | Dapper 資料存取抽象 | Portal Services |
| **Strategy** | 不同 CA 後端切換 | ICertificateAuthority |
| **Decorator** | Secret 加解密包裝器 | DeploymentJobSecretMapper |

### 2.3 技術棧

| 層次 | 技術 | 版本 | 用途 |
|------|------|------|------|
| 語言 | C# | 13.0 | 所有後端服務 |
| 框架 | .NET | 10.0 LTS | 所有服務執行環境 |
| Web API | ASP.NET Core | 10.0 | Portal, Gateway, WindowsAgent |
| Worker | .NET Generic Host | 10.0 | Collector |
| ORM | Dapper | 2.x | Portal 資料存取 |
| 資料庫 | PostgreSQL | 16.x | Portal 主資料庫 |
| UI | Blazor Server + WASM | 10.0 | Portal.UI |
| UI 元件 | Radzen.Blazor | 10.x | 介面元件 |
| 測試（單元） | xUnit + Moq + FluentAssertions | — | Portal/Gateway/Collector.Tests |
| 測試（E2E） | NUnit + Playwright | 1.58.0 | E2E.Tests |
| 測試（整合） | WebApplicationFactory + Testcontainers | — | Integration.Tests |
| 加密 | ASP.NET Core Data Protection | 10.0 | PFX 秘密加密 |

---

## 3. 元件架構設計

### 3.1 Portal（業務核心）

```
Portal/
├── Program.cs                  ← 服務註冊、中介層管道
├── Controllers/
│   ├── CertificatesController  ← 憑證 CRUD + 探索 + 申請
│   ├── DeploymentsController   ← 部署作業管理
│   ├── AgentsController        ← 代理程式列表
│   ├── DashboardController     ← 統計數據聚合
│   └── SecurityController      ← 認證探針
├── Services/
│   └── GatewayService          ← Gateway HTTP 客戶端
├── Security/
│   └── DeploymentJobSecretMapper ← PFX 加解密
├── Schema/
│   └── init.sql               ← 資料庫 DDL
└── PortalApiBearerAuthenticationHandler ← 認證處理器
```

**依賴圖**：

```
CertificatesController
    ├── IDbConnection (PostgreSQL/Dapper)
    └── GatewayService → HttpClient → Gateway API

DeploymentsController
    ├── IDbConnection
    └── DeploymentJobSecretMapper → IDataProtector

AgentsController
    └── IDbConnection
```

**設計約束**：
- 所有資料庫操作必須使用參數化查詢（防 SQL Injection）
- 回傳憑證列表時，`RawData` 欄位**必須**設為 null
- 回傳部署作業列表給 UI 時，`PfxData` 和 `PfxPassword` **必須**清空

---

### 3.2 Gateway（CA 邊界）

```
Gateway/
├── Program.cs                  ← 最小依賴設定
├── Controllers/
│   └── CaController            ← Issue + Revoke
├── Services/
│   └── AdcsGatewayService      ← CA 代理邏輯
├── GatewayIssueRequestThrottle ← 速率限制實作
└── GatewayServiceBearerAuthenticationHandler
```

**設計原則**：
- Gateway 是 **無狀態** 服務（無資料庫）
- 與 ADCS 的整合細節完全封裝於 `AdcsGatewayService`
- 速率限制在控制器層處理，不依賴外部快取（in-memory）
- 如果 Windows Agent 不可用，自動降級為 Mock 模式（避免開發阻塞）

**可替換後端**：透過 `ICertificateAuthority` 接口，未來可切換至：
- Microsoft ADCS（當前）
- Let's Encrypt（ACME 協議）
- HashiCorp Vault PKI
- DigiCert/Entrust 外部 CA

---

### 3.3 Collector（分散式代理）

```
Collector/
├── Program.cs                  ← Worker 服務登記
├── Services/
│   ├── WindowsDiscoveryService ← 實作 IDiscoveryService
│   ├── WindowsDeploymentService ← 實作 IDeploymentService
│   ├── ReportingService        ← Portal API 客戶端
│   └── CertificateRequestService ← CSR 生成 + 提交
└── CollectorWorker (Background Service)
```

**設計原則**：
- Collector 是**單向主動連接**：只有 Collector 呼叫 Portal，Portal 不主動推送
- 每個作業循環完全獨立，無共享狀態
- 探索服務和部署服務透過接口隔離，方便針對不同 OS 替換實作
- 所有 HTTP 呼叫包裝在 try-catch，確保 Agent 故障不停止整個 Worker

**平台擴展點**：

| 平台 | 介面 | 現有實作 |
|------|------|----------|
| Windows | `IDiscoveryService` | `WindowsDiscoveryService` |
| Windows | `IDeploymentService` | `WindowsDeploymentService` |
| Linux (計畫) | `IDiscoveryService` | `LinuxDiscoveryService`（待實作） |
| F5 (計畫) | `IDeploymentService` | `F5DeploymentService`（待實作） |

---

### 3.4 Windows Agent（ADCS 代理）

```
WindowsAgent/
├── Program.cs      ← Minimal API + Windows 驗證
└── POST /api/adcs/submit
```

**設計說明**：

Windows Agent 解決一個關鍵問題：ADCS 的 COM/DCOM 介面**只能在 Windows 上呼叫**，但 Gateway 可能部署在 Linux 容器中。透過此代理模式：

```
Linux Container (Gateway)
    → HTTP POST /api/adcs/submit
        → Windows Server (Windows Agent)
            → COM CCertRequest
                → Active Directory Certificate Services
```

**Windows Agent 的職責限制**：
- 只接受 Gateway 的 HTTP 請求（不對外部網路開放）
- 只執行 ADCS CSR 提交（不做任何業務邏輯）
- OS 必須是 Windows，否則直接拒絕（`BadRequest`）

---

### 3.5 Portal.UI（管理介面）

```
Portal.UI/
├── Portal.UI/                  ← Blazor Server（SSR + 路由）
│   ├── Program.cs
│   ├── Components/
│   │   ├── App.razor           ← 根元件（InteractiveAuto）
│   │   ├── Routes.razor
│   │   └── Layout/
│   │       ├── MainLayout.razor ← RadzenLayout 結構
│   │       └── NavMenu.razor   ← 4 個導覽項目
│   └── appsettings.json
└── Portal.UI.Client/           ← Blazor WASM（互動元件）
    ├── Pages/
    │   ├── Home.razor          ← 儀表板
    │   ├── Certificates.razor  ← 憑證清單
    │   ├── Deployments.razor   ← 部署管理
    │   └── Agents.razor        ← 代理程式
    └── Services/
        └── PkiApiClient.cs     ← HTTP API 客戶端
```

**渲染策略**：

| 元件 | 渲染模式 | 說明 |
|------|----------|------|
| App.razor | `InteractiveAuto` | 初始 SSR，後切換為 WASM |
| 所有 Pages | WASM（Hydration 後） | 互動 UI 在瀏覽器端執行 |
| MainLayout | Server | 佈局維持 Server 渲染 |

---

## 4. 安全架構

### 4.1 身份驗證層次

```
┌────────────────────────────────────────────────────────────────┐
│  Layer 1: Transport Security                                    │
│  - TLS 1.2/1.3 (HTTPS 強制)                                   │
│  - HSTS (Strict-Transport-Security)                            │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│  Layer 2: Service-to-Service Authentication                     │
│  - Static Bearer Tokens (高熵隨機字串)                         │
│  - Portal ↔ Collector: Portal:ApiAuthToken                     │
│  - Portal ↔ Gateway: Gateway:ServiceAuthToken                  │
│  - Portal.UI ↔ Portal: PortalApi:AuthToken                     │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│  Layer 3: Authorization Policy                                  │
│  - Gateway: GatewayIssuePolicy (scope = gateway.issue)         │
│  - Portal: [Authorize] on all controllers                       │
│  - Rate Limiting (per-partition)                               │
└────────────────────────────────────────────────────────────────┘
                           ↓
┌────────────────────────────────────────────────────────────────┐
│  Layer 4: Data Protection                                       │
│  - ASP.NET Core Data Protection API                            │
│  - PFX 資料加密存儲 (DeploymentJobSecretMapper)               │
│  - 靜態金鑰：production 環境需設定 Key Ring 持久化             │
└────────────────────────────────────────────────────────────────┘
```

### 4.2 HTTP 安全標頭

所有 Portal 和 Gateway 回應包含以下安全標頭：

| 標頭 | 值 | 目的 |
|------|-----|------|
| `X-Content-Type-Options` | `nosniff` | 防止 MIME 嗅探攻擊 |
| `X-Frame-Options` | `DENY` | 防止 Clickjacking |
| `X-XSS-Protection` | `0` | 停用過時的瀏覽器 XSS filter（現代瀏覽器 CSP 已取代） |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | 控制 Referrer 洩露 |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | 最小權限瀏覽器 API |
| `Content-Security-Policy` | `default-src 'self'; script-src 'self'; ...` | 防止 XSS / 資料注入 |
| `Strict-Transport-Security` | `max-age=31536000` | 強制 HTTPS 一年 |

### 4.3 敏感資料保護策略

| 資料 | 靜態保護 | 傳輸保護 | UI 曝露 |
|------|----------|----------|---------|
| PFX 私鑰（Binary） | DB 不存儲 | TLS | 永不回傳 |
| PFX Base64 | AES-256（Data Protection）| TLS | 清空（ForUiList）|
| PFX 密碼 | AES-256（Data Protection）| TLS | 清空（ForUiList）|
| Bearer Token | 環境變數 | TLS | 永不回傳 |
| PostgreSQL 密碼 | 環境變數 / Secret Manager | 本地連線 | 永不回傳 |

### 4.4 稽核日誌設計

所有安全相關操作記錄到稽核日誌，前綴 `[PKI_AUDIT]`：

```
[PKI_AUDIT] Certificate issued: Template=WebServer, CN=web01.internal.corp,
            Principal=PortalApiClient, CorrelationId=abc-123

[PKI_AUDIT] Certificate revoked: SerialNumber=4A00B3C2, Reason=KeyCompromise,
            Principal=PortalApiClient, CorrelationId=def-456

[PKI_AUDIT] DeploymentJob status updated: JobId=..., Status=Completed,
            CorrelationId=ghi-789
```

**稽核日誌保留策略**（建議）：
- 開發環境：Console sink，不持久化
- 生產環境：寫入 SIEM/ELK，保留 2 年

### 4.5 OWASP Top 10 對應

| 威脅 | 對應措施 |
|------|----------|
| A01 - 存取控制失效 | 所有端點 `[Authorize]`；Gateway 額外 scope |
| A02 - 密碼學失敗 | Data Protection 加密 PFX；TLS 強制 |
| A03 - 注入 | Dapper 參數化查詢；無動態 SQL |
| A04 - 不安全設計 | ICertificateAuthority 接口隔離；CA 邊界 |
| A05 - 安全設定錯誤 | 完整安全標頭；CORS 白名單 |
| A06 - 脆弱元件 | `dotnet list package --vulnerable` 定期掃描 |
| A07 - 認證失效 | Bearer Token；Token 輪換策略 |
| A08 - 資料完整性失效 | Dapper 型別化查詢；DTO 驗證 |
| A09 - 記錄和監控不足 | PKI_AUDIT 前綴；X-Correlation-ID |
| A10 - 偽造請求 | SameSite Cookie；CSP |

---

## 5. 資料架構

### 5.1 資料庫 ER 圖

```
┌───────────────────────────────────────────────────────────────────┐
│  Certificates                                                      │
│  ─────────────────────────────────────────────────────────────── │
│  PK  Id               UUID NOT NULL                               │
│      CommonName       VARCHAR(255) NOT NULL                       │
│      SerialNumber     VARCHAR(128) UNIQUE NOT NULL                │
│      Thumbprint       VARCHAR(128) UNIQUE NOT NULL                │
│      IssuerDN         TEXT NOT NULL                               │
│      NotBefore        TIMESTAMPTZ NOT NULL                        │
│      NotAfter         TIMESTAMPTZ NOT NULL  ← INDEX               │
│      Algorithm        VARCHAR(64) NOT NULL                        │
│      KeySize          INT NOT NULL                                │
│      IsPQC            BOOLEAN DEFAULT FALSE                       │
│      RawData          BYTEA                                       │
│      Status           VARCHAR(32) DEFAULT 'Active'                │
│      CreatedAt        TIMESTAMPTZ                                 │
│      UpdatedAt        TIMESTAMPTZ                                 │
└────────────────────┬──────────────────────────────────────────────┘
                     │ 1:N
          ┌──────────┴──────────────┐
          ▼                         ▼
┌───────────────────────┐  ┌───────────────────────────────────────┐
│  CertificateRequests  │  │  DeploymentJobs                        │
│  ─────────────────── │  │  ─────────────────────────────────── │
│  PK  Id     UUID      │  │  PK  Id            UUID               │
│  FK  CertId UUID      │  │  FK  CertificateId UUID               │
│      Requester        │  │      TargetHostname VARCHAR(255)       │
│      CSR     TEXT     │  │      StoreLocation  VARCHAR(255)       │
│      Template         │  │      Status         VARCHAR(32)        │
│      Status           │  │      ErrorMessage   TEXT               │
│      RequestedAt      │  │      PfxData        TEXT (encrypted)   │
└───────────────────────┘  │      PfxPassword    TEXT (encrypted)   │
                           │      CreatedAt      TIMESTAMPTZ        │
                           │      CompletedAt    TIMESTAMPTZ        │
                           └───────────────────────────────────────┘

┌───────────────────────────────────────────────────────────────────┐
│  Endpoints                                                         │
│  ─────────────────────────────────────────────────────────────── │
│  PK  Id               UUID NOT NULL                               │
│      Hostname         VARCHAR(255) NOT NULL                       │
│      IPAddress        VARCHAR(64)                                 │
│      Type             VARCHAR(64) NOT NULL                        │
│      LastHeartbeat    TIMESTAMPTZ                                 │
└────────────────────┬──────────────────────────────────────────────┘
                     │ M:N
          ┌──────────┴──────────────┐
          ▼                         │
┌───────────────────────┐           │
│  CertificateDeployments│          │
│  ─────────────────── │           │
│  PK  CertificateId UUID │◄────────┘
│  PK  EndpointId    UUID │
│      DeployedAt        │
│      LastSeen          │
└───────────────────────┘
```

### 5.2 資料存取模式

**高頻查詢**（Portal 效能考量）：

| 查詢 | 索引 | 頻率 |
|------|------|------|
| 即將到期憑證 | `idx_certificates_not_after` | Dashboard 每次載入 |
| 代理程式線上狀態 | `LastHeartbeat` 欄位比較 | Dashboard 每次載入 |
| 主機名稱待辦作業 | `TargetHostname + Status` | 每個 Collector 循環 |

---

## 6. 整合架構

### 6.1 服務間通訊協議

| 通訊路徑 | 協議 | 認證 | 方向 |
|----------|------|------|------|
| Portal.UI → Portal | HTTP/HTTPS + Bearer | `PortalApi:AuthToken` | 單向 |
| Portal → Gateway | HTTP/HTTPS + Bearer + Scope | `Gateway:ServiceAuthToken` | 單向 |
| Gateway → WindowsAgent | HTTP（內網） | 無（網路隔離） | 單向 |
| WindowsAgent → ADCS | DCOM/RPC | Windows Kerberos | 單向 |
| Collector → Portal | HTTP/HTTPS + Bearer | `Portal:ApiAuthToken` | 單向 |

### 6.2 ADCS 整合流程

```
時序圖：憑證簽發完整流程

Client (UI)        Portal         Gateway      WindowsAgent    ADCS
    │                 │               │               │           │
    │──POST request──▶│               │               │           │
    │                 │─POST issue───▶│               │           │
    │                 │               │─POST submit──▶│           │
    │                 │               │               │──COM/RPC─▶│
    │                 │               │               │◀──cert────│
    │                 │               │◀──ProxyResp───│           │
    │                 │◀──Certificate─│               │           │
    │                 │               │               │           │
    │                 ├─Save to DB────│               │           │
    │◀──201 Created───│               │               │           │
```

### 6.3 Collector 探索與部署流程

```
時序圖：Collector 工作循環

Collector           Portal              WindowsAgent(target)
    │                   │                        │
    │─Discover certs──────────────────────────────▶ (X509Store)
    │◀──List<CertDiscovery>──────────────────────────
    │                   │                        │
    │──POST /discovery─▶│                        │
    │◀──200 OK──────────│                        │
    │                   │                        │
    │──GET /jobs/{host}▶│                        │
    │◀──List<Job>───────│                        │
    │                   │                        │
    │──Install PFX────────────────────────────────▶ (X509Store.Add)
    │◀──Success/Error────────────────────────────────
    │                   │                        │
    │──POST /status─────▶│                       │
    │◀──200 OK───────────│                       │
```

---

## 7. 部署架構

### 7.1 Docker Compose 生產部署

```yaml
# 建議的生產 docker-compose 結構
services:
  portal:
    image: enterprise-pki/portal:v2.0.0
    ports:
      - "5069:5069"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=pki;...
      - Portal__ApiAuthToken=${PORTAL_API_AUTH_TOKEN}
      - Gateway__Url=http://gateway:5176
      - Gateway__ServiceAuthToken=${GATEWAY_SERVICE_AUTH_TOKEN}
    depends_on:
      db:
        condition: service_healthy
    restart: always

  gateway:
    image: enterprise-pki/gateway:v2.0.0
    ports:
      - "5176:5176"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Gateway__ServiceAuthToken=${GATEWAY_SERVICE_AUTH_TOKEN}
      - AdcsProxy__Url=http://windows-agent-host:8080
    restart: always

  portal-ui:
    image: enterprise-pki/portal-ui:v2.0.0
    ports:
      - "5261:5261"
    environment:
      - PortalApi__BaseUrl=http://portal:5069
      - PortalApi__AuthToken=${PORTAL_API_AUTH_TOKEN}
    depends_on:
      - portal
    restart: always

  db:
    image: postgres:16-alpine
    volumes:
      - pki-data:/var/lib/postgresql/data
      - ./src/Portal/Schema/init.sql:/docker-entrypoint-initdb.d/init.sql
    environment:
      - POSTGRES_DB=pki
      - POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: always
```

### 7.2 服務部署拓撲

```
Deployment Zone 1 (DMZ / App Servers)
┌──────────────────────────────────────┐
│  Portal.UI (Blazor)  :5261           │
│  Portal API          :5069           │
│  Gateway API         :5176           │
│  PostgreSQL          :5432           │
└──────────────────────────────────────┘

Deployment Zone 2 (Windows DC Network)
┌──────────────────────────────────────┐
│  Windows Agent       :8080           │
│  Active Directory CA (ADCS)          │
└──────────────────────────────────────┘

Distributed Zones (All Managed Hosts)
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  Collector   │  │  Collector   │  │  Collector   │
│  WIN-SRV-01  │  │  WIN-SRV-02  │  │  LINUX-01    │
└──────────────┘  └──────────────┘  └──────────────┘
```

### 7.3 容器映像建置

```dockerfile
# Portal Dockerfile 範例
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5069

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Portal/Portal.csproj", "Portal/"]
COPY ["src/Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Portal/Portal.csproj"
COPY src/ .
RUN dotnet build "Portal/Portal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Portal/Portal.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Portal.dll"]
```

---

## 8. 系統操作手冊

### 8.1 日常運維檢查清單

#### 每日檢查

```bash
# 1. 確認所有服務正常運行
docker-compose ps
# 預期：所有服務狀態為 running

# 2. 檢查 API 健康
curl -s http://localhost:5069/api/security/probe \
  -H "Authorization: Bearer ${PORTAL_API_AUTH_TOKEN}"
# 預期：{"status":"Authorized"}

# 3. 檢查即將到期憑證
curl -s http://localhost:5069/api/dashboard/stats \
  -H "Authorization: Bearer ${PORTAL_API_AUTH_TOKEN}" | jq .expiringSoon
# 若 > 0，需立即處理
```

#### 每週檢查

```bash
# 確認無重大日誌錯誤
docker-compose logs --since 7d portal | grep -i "error\|critical"

# 確認 Collector 心跳正常（至少一台 Agent 在線）
curl -s http://localhost:5069/api/agents \
  -H "Authorization: Bearer ${PORTAL_API_AUTH_TOKEN}" \
  | jq '.items[] | select(.isOnline == false)'

# 確認 PostgreSQL 空間
docker exec pki-db psql -U postgres -d pki -c "SELECT pg_size_pretty(pg_database_size('pki'));"
```

#### 每月檢查

```bash
# NuGet 漏洞掃描
cd src && dotnet list package --vulnerable

# 確認 Token 未過期（如有輪換策略）
# 確認 Data Protection keys 未過期

# PostgreSQL VACUUM
docker exec pki-db psql -U postgres -d pki -c "VACUUM ANALYZE;"
```

---

### 8.2 憑證到期處理 SOP

```
[警報] expiringSoon > 0

步驟 1: 識別到期憑證
   curl /api/certificates?page=1&pageSize=200
   篩選 NotAfter < NOW() + 30d

步驟 2: 確認受影響服務
   curl /api/certificates/{id}
   查看 CertificateDeployments 確認已安裝的端點

步驟 3: 提交更新申請
   POST /api/certificates/request
   { "requester": "admin@corp", "csr": "<new-csr>", "templateName": "WebServer" }

步驟 4: 建立部署作業
   POST /api/deployments/jobs
   { "certificateId": "<new-cert-id>", "targetHostname": "<host>", ... }

步驟 5: 等待 Collector 執行
   GET /api/deployments/jobs/{jobId}
   等待 Status = "Completed"

步驟 6: 驗證
   確認目標主機 HTTPS 憑證已更新
```

---

### 8.3 Collector Agent 故障排除

#### Agent 顯示 Offline

```bash
# 確認 Collector 服務狀態
systemctl status enterprise-pki-collector   # Linux
sc query EnterprisePKICollector             # Windows PowerShell

# 確認 Collector 日誌
journalctl -u enterprise-pki-collector -n 50   # Linux

# 確認網路連通性
curl http://portal-host:5069/api/security/probe \
  -H "Authorization: Bearer ${PORTAL_API_AUTH_TOKEN}"

# 確認 Collector 設定正確
cat /etc/enterprise-pki-collector/appsettings.json
```

#### 部署作業卡在 Pending

```bash
# 確認 Collector 正在執行
curl /api/agents | jq '.items[] | select(.hostname == "TARGET-HOST")'

# 手動觸發 Collector 執行（如有提供）
# 或等待下一個循環（~60 分鐘）

# 查看部署作業詳情
curl /api/deployments/jobs/{jobId}
# 若 Status=Failed，查看 errorMessage
```

---

### 8.4 Gateway / ADCS 故障排除

#### 憑證簽發失敗（500 Internal Server Error）

```bash
# 確認 Gateway 健康
curl http://localhost:5176/api/security/probe -H "..."

# 確認 Windows Agent 可達
curl http://windows-agent-host:8080/api/adcs/submit \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"csr":"test","template":"WebServer","caConfig":"CA01\\Enterprise-CA"}'

# 確認 ADCS 服務狀態（在 Windows Agent 主機）
# Server Manager → Tools → Certification Authority
# 或：certutil -ping
```

#### 速率限制（429）

```bash
# 查看目前設定
cat src/Gateway/appsettings.json | grep -A5 RateLimiting

# 調整限制（需重啟 Gateway）
# Gateway:RateLimiting:PermitLimit: 提高值
# Gateway:RateLimiting:WindowSeconds: 降低值
```

---

### 8.5 資料庫維護

#### 備份

```bash
# 每日全量備份
docker exec pki-db pg_dump -U postgres pki > \
  /backup/pki-$(date +%Y%m%d).sql

# 驗證備份完整性
psql -U postgres -d pki_test < /backup/pki-$(date +%Y%m%d).sql
```

#### 還原

```bash
# 停止 Portal 服務
docker-compose stop portal

# 還原資料庫
docker exec -i pki-db psql -U postgres -d pki < /backup/pki-20260430.sql

# 重啟 Portal
docker-compose start portal
```

#### 清理過期記錄

```sql
-- 清理 90 天前已完成/失敗的部署作業
DELETE FROM DeploymentJobs
WHERE Status IN ('Completed', 'Failed')
  AND CompletedAt < NOW() - INTERVAL '90 days';

-- 清理 1 年前已到期的憑證記錄（保留前先備份）
DELETE FROM Certificates
WHERE Status = 'Expired'
  AND NotAfter < NOW() - INTERVAL '1 year';
```

---

### 8.6 效能調整

| 場景 | 症狀 | 解決方案 |
|------|------|----------|
| 大量憑證查詢慢 | GET /certificates 回應 > 2s | 確認 `idx_certificates_not_after` 索引存在 |
| Collector 探索慢 | 掃描 Windows 憑證庫耗時 > 30s | 限制掃描的 Store 類型 |
| Gateway 吞吐低 | 憑證簽發排隊 | 提高 `PermitLimit`；評估 Gateway 水平擴展 |
| Portal API 記憶體高 | OOM 重啟 | 設定 Docker `memory: 512m` |

---

## 9. 可擴展性設計

### 9.1 水平擴展方案

#### Portal API 無狀態化需求

Portal 目前使用 ASP.NET Core Data Protection（預設 in-memory key ring）。水平擴展前**必須**：

```csharp
// 設定共享 key ring（Redis 或資料庫）
services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()  // 或 .PersistKeysToStackExchangeRedis()
    .SetApplicationName("EnterprisePKI");
```

#### Rate Limiting 分散式問題

目前使用 in-memory FixedWindowRateLimiter。水平擴展前需改為分散式計數器：

```csharp
// 替換為 Redis-backed rate limiter
services.AddRateLimiter(options =>
    options.AddSlidingWindowLimiter("gateway", o =>
        o.Window = TimeSpan.FromSeconds(60)
        // + Redis 計數器...
    ));
```

### 9.2 Collector 擴展

Collector 天生支援水平擴展：
- 每個主機部署一個獨立 Collector 實例
- 透過 `hostname` 識別（`GET /api/deployments/jobs/{hostname}`）
- 無共享狀態，無需協調機制

### 9.3 未來整合擴展點

| 擴展點 | 介面 | 說明 |
|--------|------|------|
| 新 CA 後端 | `ICertificateAuthority` | 實作接口後在 Gateway 注入 |
| 新部署目標 | `IDeploymentService` | F5/JKS/K8s 部署服務 |
| 新探索來源 | `IDiscoveryService` | Linux/F5/Cloud 探索服務 |
| 通知系統 | 事件發布 | 憑證到期/部署完成通知 |
| LDAP 整合 | AD Group → Token 映射 | 企業 SSO |

---

## 10. 後量子密碼學（PQC）策略

### 10.1 現況評估

| 項目 | 現況 | 目標 |
|------|------|------|
| 資料模型 | `IsPQC`, `Algorithm` 欄位已就緒 | — |
| UI 顯示 | PQC 徽章、PQC Ready 統計 | — |
| 憑證申請 | UI 支援選擇 ML-KEM-768 / ML-DSA-65 | — |
| CSR 生成 | 目前 Collector 只支援 RSA-2048 | 加入 ML-KEM/ML-DSA CSR 生成 |
| ADCS 整合 | 依賴 CA 是否支援 PQC 模板 | 配置 PQC ADCS 模板 |

### 10.2 PQC 演算法支援計畫

| 演算法 | NIST 標準 | 用途 | 優先級 |
|--------|-----------|------|--------|
| ML-KEM-768 (Kyber) | FIPS 203 | 金鑰封裝（TLS 替代 RSA/ECDH） | High |
| ML-DSA-65 (Dilithium) | FIPS 204 | 數位簽章 | High |
| SLH-DSA (SPHINCS+) | FIPS 205 | Hash-based 簽章（備選） | Low |

### 10.3 遷移路徑

```
Phase 1 (現在): 追蹤現有 PQC 憑證（IsPQC 標記）
Phase 2 (Q3 2026): Collector CSR 生成支援 ML-DSA-65
Phase 3 (Q4 2026): ADCS 設定 PQC 憑證模板
Phase 4 (2027): 新簽發憑證預設為 PQC-Hybrid（RSA + ML-KEM 雙算法）
Phase 5 (2028): 完全遷移至 PQC 演算法
```

---

## 11. 架構決策記錄（ADR）

### ADR-001: 選擇靜態 Bearer Token 而非 JWT

**狀態**：已採用  
**背景**：需要服務間認證機制，但不引入外部 Identity Server 依賴。  
**決策**：使用高熵隨機字串作為 Bearer Token，自訂 `AuthenticationHandler` 驗證。  
**後果**：
- (+) 無外部依賴，部署簡單
- (+) 適合封閉企業內網
- (-) Token 輪換需要重啟服務
- (-) 無法做細粒度的使用者授權

**未來計畫**：引入 Keycloak 或 Entra ID（Azure AD）時替換為 OIDC/JWT。

---

### ADR-002: Gateway 隔離 CA 整合

**狀態**：已採用  
**背景**：ADCS 整合複雜且依賴 Windows COM，直接整合在 Portal 會增加耦合。  
**決策**：獨立 Gateway 服務作為 CA 邊界，Portal 透過 HTTP 介接。  
**後果**：
- (+) Portal 可跨平台部署（Linux 容器）
- (+) 可替換 CA 後端不影響 Portal
- (+) 獨立速率限制保護 CA
- (-) 增加一個網路跳躍

---

### ADR-003: Collector 採用 Pull-based 模型

**狀態**：已採用  
**背景**：Collector 可能在防火牆後，Portal 無法主動推送。  
**決策**：Collector 定期輪詢 Portal（Pull），而非 Portal 推送（Push）。  
**後果**：
- (+) 部署簡單，Collector 只需出站連線
- (+) Collector 故障不影響 Portal
- (-) 部署延遲最大 ~60 分鐘
- (-) 無法即時觸發緊急部署

**未來計畫**：考慮加入 SignalR 通道支援即時推送。

---

### ADR-004: Dapper 而非 EF Core

**狀態**：已採用  
**背景**：PKI 查詢需要精確控制 SQL（索引、條件、資料型別）。  
**決策**：使用 Dapper 輕量 ORM。  
**後果**：
- (+) 完全控制 SQL，易於優化
- (+) 透明查詢，無 N+1 問題
- (-) 無自動 Migration（需手動管理 DDL）
- (-) 更多 boilerplate 程式碼

---

### ADR-005: ASP.NET Core Data Protection 加密 PFX

**狀態**：已採用  
**背景**：PFX 私鑰資料必須加密存儲，但需要應用層可讀（部署時需解密）。  
**決策**：使用 `IDataProtector`（AES-256 + HMACSHA256），透過 `DeploymentJobSecretMapper` 封裝。  
**後果**：
- (+) 無需外部 KMS，開發環境零設定
- (+) .NET 內建，維護成本低
- (-) Key Ring 預設 in-memory，水平擴展需額外設定持久化

---

*文件結束*

> **版本歷史**
> | 版本 | 日期 | 作者 | 說明 |
> |------|------|------|------|
> | 1.0.0 | 2026-04-30 | Enterprise PKI Team | 初始版本 |
