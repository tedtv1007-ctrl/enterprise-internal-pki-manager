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
    Type VARCHAR(64) NOT NULL
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

-- Index for expiration monitoring
CREATE INDEX idx_certificates_not_after ON Certificates(NotAfter);
CREATE INDEX idx_certificates_thumbprint ON Certificates(Thumbprint);
