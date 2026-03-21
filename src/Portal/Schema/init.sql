-- SQL Schema for Enterprise PKI Manager
-- Dapper/EF Core ready

CREATE TABLE IF NOT EXISTS Certificates (
    Id UUID PRIMARY KEY,
    CommonName VARCHAR(255) NOT NULL,
    SerialNumber VARCHAR(128) UNIQUE NOT NULL,
    Thumbprint VARCHAR(128) UNIQUE NOT NULL,
    IssuerDN TEXT NOT NULL,
    NotBefore TIMESTAMP WITH TIME ZONE NOT NULL,
    NotAfter TIMESTAMP WITH TIME ZONE NOT NULL,
    Algorithm VARCHAR(64) NOT NULL,
    KeySize INT NOT NULL,
    IsPQC BOOLEAN DEFAULT FALSE,
    RawData BYTEA,
    Status VARCHAR(32) DEFAULT 'Active',
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Endpoints (
    Id UUID PRIMARY KEY,
    Hostname VARCHAR(255) NOT NULL,
    IPAddress VARCHAR(64),
    Type VARCHAR(64) NOT NULL,
    LastHeartbeat TIMESTAMP WITH TIME ZONE
);

CREATE TABLE IF NOT EXISTS CertificateDeployments (
    CertificateId UUID REFERENCES Certificates(Id),
    EndpointId UUID REFERENCES Endpoints(Id),
    DeployedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    LastSeen TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (CertificateId, EndpointId)
);

CREATE TABLE IF NOT EXISTS CertificateRequests (
    Id UUID PRIMARY KEY,
    Requester VARCHAR(255) NOT NULL,
    CSR TEXT NOT NULL,
    TemplateName VARCHAR(128) NOT NULL,
    Status VARCHAR(32) DEFAULT 'Pending',
    CertificateId UUID REFERENCES Certificates(Id),
    RequestedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS DeploymentJobs (
    Id UUID PRIMARY KEY,
    CertificateId UUID REFERENCES Certificates(Id),
    TargetHostname VARCHAR(255) NOT NULL,
    StoreLocation VARCHAR(255) NOT NULL,
    Status VARCHAR(32) DEFAULT 'Pending',
    ErrorMessage TEXT,
    PfxData TEXT,
    PfxPassword TEXT,
    CreatedAt TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP WITH TIME ZONE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_certificates_not_after ON Certificates(NotAfter);
CREATE INDEX IF NOT EXISTS idx_certificates_thumbprint ON Certificates(Thumbprint);

-- ============================================================
-- Seed Data for Demo / Testing
-- ============================================================

-- Endpoints (Collector Agents)
INSERT INTO Endpoints (Id, Hostname, IPAddress, Type, LastHeartbeat) VALUES
    ('a1000000-0000-0000-0000-000000000001', 'WIN-COLLECTOR-01', '10.0.5.120', 'Windows', NOW() - INTERVAL '30 seconds'),
    ('a1000000-0000-0000-0000-000000000002', 'LINUX-NODE-04', '10.0.5.142', 'Linux', NOW() - INTERVAL '12 minutes'),
    ('a1000000-0000-0000-0000-000000000003', 'WIN-IIS-PROD-02', '10.0.10.55', 'Windows', NOW() - INTERVAL '2 minutes'),
    ('a1000000-0000-0000-0000-000000000004', 'K8S-WORKER-07', '10.0.20.77', 'Linux', NOW() - INTERVAL '1 minute'),
    ('a1000000-0000-0000-0000-000000000005', 'F5-LB-EDGE-01', '10.0.1.10', 'F5', NOW() - INTERVAL '45 seconds')
ON CONFLICT DO NOTHING;

-- Certificates
INSERT INTO Certificates (Id, CommonName, SerialNumber, Thumbprint, IssuerDN, NotBefore, NotAfter, Algorithm, KeySize, IsPQC, Status, CreatedAt, UpdatedAt) VALUES
    ('c1000000-0000-0000-0000-000000000001', '*.internal.enterprise.com', 'SN-2024-WILD-001', 'A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2', 'CN=Enterprise Root CA, DC=enterprise, DC=local', NOW() - INTERVAL '60 days', NOW() + INTERVAL '305 days', 'RSA', 4096, false, 'Active', NOW() - INTERVAL '60 days', NOW()),
    ('c1000000-0000-0000-0000-000000000002', 'vault.internal.enterprise.com', 'SN-2024-VAULT-002', 'PQC-DILITHIUM3-ABCDEF1234567890ABCDEF12', 'CN=Enterprise PQC CA, DC=enterprise, DC=local', NOW() - INTERVAL '30 days', NOW() + INTERVAL '150 days', 'ML-KEM-768', 3168, true, 'Active', NOW() - INTERVAL '30 days', NOW()),
    ('c1000000-0000-0000-0000-000000000003', 'legacy-app.internal', 'SN-2023-LEGACY-003', 'Z9Y8X7W6V5U4T3S2R1Q0Z9Y8X7W6V5U4T3S2R1Q0', 'CN=Enterprise Root CA, DC=enterprise, DC=local', NOW() - INTERVAL '340 days', NOW() + INTERVAL '15 days', 'RSA', 2048, false, 'Active', NOW() - INTERVAL '340 days', NOW()),
    ('c1000000-0000-0000-0000-000000000004', 'api-gateway.enterprise.com', 'SN-2024-APIGW-004', 'B4C5D6E7F8A9B0C1D2E3F4A5B6C7D8E9F0A1B2C3', 'CN=Enterprise Root CA, DC=enterprise, DC=local', NOW() - INTERVAL '90 days', NOW() + INTERVAL '275 days', 'ECDSA', 384, false, 'Active', NOW() - INTERVAL '90 days', NOW()),
    ('c1000000-0000-0000-0000-000000000005', 'monitoring.soc.enterprise.com', 'SN-2024-SOC-005', 'D1E2F3A4B5C6D7E8F9A0B1C2D3E4F5A6B7C8D9E0', 'CN=Enterprise PQC CA, DC=enterprise, DC=local', NOW() - INTERVAL '15 days', NOW() + INTERVAL '350 days', 'Dilithium3', 2592, true, 'Active', NOW() - INTERVAL '15 days', NOW()),
    ('c1000000-0000-0000-0000-000000000006', 'old-intranet.internal', 'SN-2022-OLD-006', 'E5F6A7B8C9D0E1F2A3B4C5D6E7F8A9B0C1D2E3F4', 'CN=Enterprise Root CA, DC=enterprise, DC=local', NOW() - INTERVAL '400 days', NOW() - INTERVAL '5 days', 'RSA', 1024, false, 'Expired', NOW() - INTERVAL '400 days', NOW()),
    ('c1000000-0000-0000-0000-000000000007', 'db-cluster.enterprise.com', 'SN-2024-DB-007', 'F2A3B4C5D6E7F8A9B0C1D2E3F4A5B6C7D8E9F0A1', 'CN=Enterprise Root CA, DC=enterprise, DC=local', NOW() - INTERVAL '45 days', NOW() + INTERVAL '320 days', 'RSA', 4096, false, 'Active', NOW() - INTERVAL '45 days', NOW()),
    ('c1000000-0000-0000-0000-000000000008', 'pqc-test.lab.enterprise.com', 'SN-2024-PQCTEST-008', 'A9B0C1D2E3F4A5B6C7D8E9F0A1B2C3D4E5F6A7B8', 'CN=Enterprise PQC CA, DC=enterprise, DC=local', NOW() - INTERVAL '5 days', NOW() + INTERVAL '360 days', 'ML-DSA-65', 4032, true, 'Active', NOW() - INTERVAL '5 days', NOW())
ON CONFLICT DO NOTHING;

-- Certificate Deployments
INSERT INTO CertificateDeployments (CertificateId, EndpointId, DeployedAt, LastSeen) VALUES
    ('c1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000001', NOW() - INTERVAL '30 days', NOW()),
    ('c1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000003', NOW() - INTERVAL '30 days', NOW()),
    ('c1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000005', NOW() - INTERVAL '28 days', NOW()),
    ('c1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000003', NOW() - INTERVAL '200 days', NOW()),
    ('c1000000-0000-0000-0000-000000000004', 'a1000000-0000-0000-0000-000000000005', NOW() - INTERVAL '60 days', NOW()),
    ('c1000000-0000-0000-0000-000000000007', 'a1000000-0000-0000-0000-000000000004', NOW() - INTERVAL '40 days', NOW())
ON CONFLICT DO NOTHING;

-- Deployment Jobs
INSERT INTO DeploymentJobs (Id, CertificateId, TargetHostname, StoreLocation, Status, ErrorMessage, CreatedAt, CompletedAt) VALUES
    ('d1000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000001', 'WIN-IIS-PROD-02', 'My/LocalMachine', 'Completed', NULL, NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour 55 minutes'),
    ('d1000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000004', 'F5-LB-EDGE-01', 'clientssl/api-gateway', 'Completed', NULL, NOW() - INTERVAL '5 hours', NOW() - INTERVAL '4 hours 50 minutes'),
    ('d1000000-0000-0000-0000-000000000003', 'c1000000-0000-0000-0000-000000000002', 'K8S-WORKER-07', '/etc/pki/tls/certs/', 'InProgress', NULL, NOW() - INTERVAL '15 minutes', NULL),
    ('d1000000-0000-0000-0000-000000000004', 'c1000000-0000-0000-0000-000000000003', 'WIN-IIS-PROD-02', 'My/LocalMachine', 'Failed', 'Access denied to Certificate Store. Insufficient permissions for service account.', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '2 hours 58 minutes'),
    ('d1000000-0000-0000-0000-000000000005', 'c1000000-0000-0000-0000-000000000007', 'K8S-WORKER-07', '/var/lib/postgresql/certs/', 'Pending', NULL, NOW() - INTERVAL '5 minutes', NULL)
ON CONFLICT DO NOTHING;

-- Certificate Requests
INSERT INTO CertificateRequests (Id, Requester, CSR, TemplateName, Status, CertificateId, RequestedAt) VALUES
    ('f1000000-0000-0000-0000-000000000001', 'admin@enterprise.local', 'MOCK-CSR-DATA-001', 'WebServer', 'Issued', 'c1000000-0000-0000-0000-000000000001', NOW() - INTERVAL '60 days'),
    ('f1000000-0000-0000-0000-000000000002', 'security@enterprise.local', 'MOCK-CSR-DATA-002', 'PQCWebServer', 'Issued', 'c1000000-0000-0000-0000-000000000002', NOW() - INTERVAL '30 days'),
    ('f1000000-0000-0000-0000-000000000003', 'devops@enterprise.local', 'MOCK-CSR-DATA-003', 'WebServer', 'Pending', NULL, NOW() - INTERVAL '1 hour')
ON CONFLICT DO NOTHING;
