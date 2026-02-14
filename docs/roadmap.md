# Implementation Roadmap

## Phase 1: Foundation (MVP)
- [ ] Set up Project Structure and Repository.
- [ ] Develop **CA Gateway** prototype (Basic ADCS interaction).
- [ ] Build **Center Control Portal** core (API skeleton, SQL Schema).
- [ ] Implement AD Authentication for Portal access.

## Phase 2: Integration & Discovery
- [x] Develop **Collector** agent for IIS discovery skeleton.
- [x] Implement **Windows Agent Proxy** for ADCS interaction.
- [x] Enable CSR generation and submission from Collector to Portal.
- [x] Implement end-to-end issuance flow (Collector -> Portal -> Gateway -> ADCS).

## Phase 3: Automation & Deployment
- [x] Implement 'Deployment' mechanism in Collector agent.
- [x] Create 'Deployment Jobs' API and tracking in Portal.
- [ ] Implement automated deployment for IIS (advanced bindings).
- [ ] Add support for F5 BIG-IP discovery and deployment.
- [ ] Create UI/Dashboard for certificate visibility and manual overrides.

## Phase 4: Expansion & Hardening
- [ ] Add Cloud (AWS/Azure) certificate discovery/management.
- [ ] Implement advanced alerting (Teams/Email) for expiry.
- [ ] External audit logging and compliance reporting.
