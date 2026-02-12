# ADCS Interaction Strategy

## Problem
The `Gateway` service may run on Linux (containers), but Microsoft ADCS requires DCOM/RPC (COM) for certificate enrollment, which is Windows-only.

## Selected Strategy: Windows Agent Proxy
We will implement a lightweight **Windows Agent** (can be co-located with the `Collector`) that runs on a domain-joined Windows machine.

### Architecture
1. **Gateway (Linux/Container)**: 
   - Exposes a REST API for the `Portal`.
   - Uses an `IRemoteCA` client to communicate with the `Windows Agent`.
2. **Windows Agent (Windows)**:
   - Authenticated Web API (mTLS or API Key).
   - Uses `CERTCLILib.dll` (COM) to interact with ADCS.
   - Handles `CertRequest.Submit` and `CertRequest.GetCertificate`.

### Why this strategy?
- **Robustness**: Does not require exposing ADCS via Web Enrollment or CES (which are often disabled in hardened environments).
- **Flexibility**: The Gateway remains cross-platform.
- **Security**: The Proxy can be placed in a restricted zone with specific permissions to call the CA.

## Alternative: CES/CEP
If the enterprise already has Certificate Enrollment Web Services (CES) and Certificate Enrollment Policy (CEP) enabled, the Gateway can use SOAP requests to talk to them directly. However, implementing MS-WSTEP in .NET Core on Linux is complex.

## Implementation Plan
1. Create `WindowsAgent` project.
2. Implement `CaProxyController` in `WindowsAgent`.
3. Update `AdcsGatewayService` in `Gateway` to call the `WindowsAgent`.
