# Integration Repository Design

## Purpose

This repository will host integration projects built with Camel Karavan and Apache ActiveMQ Artemis. The goal is to provide a consistent, message-driven integration framework for connecting enterprise systems with clear separation between transport, transformation, orchestration, and operational concerns.

## Technology Baseline

- Camel Karavan: `4.14.2`
- Apache ActiveMQ Artemis: `2.44`
- Runtime style: Camel Main / Camel JBang generated Karavan integrations
- Containerization: Docker-based local runtime and deployment packaging

### Reference deployment input

The repository includes [Reference/docker-compose.yml](/c:/Repos/TestIntegration-1/Reference/docker-compose.yml) as a deployment reference for how Camel Karavan and Artemis are wired together.

- Artemis is deployed with image `apache/activemq-artemis:2.44.0-alpine`
- Karavan is deployed with image `ghcr.io/apache/camel-karavan:4.14.2`
- Both services share the `karavan` Docker network
- Karavan is configured to connect to Artemis at `tcp://artemis:61616`
- The compose file should be treated as a reference for infrastructure setup and local stack design
- The repository can reuse this structure when creating its own runtime compose files

## Repository Intent

The repository is intended to support multiple integration projects over time. Each project should follow a common pattern:

- Define routes and integrations in Camel Karavan
- Use Artemis as the central broker for decoupling producers and consumers
- Externalize environment-specific configuration
- Support local development, test, and container deployment
- Keep integration logic modular so individual projects can evolve independently

## Core Architecture

The preferred architecture is event-driven and broker-centered.

### Key components

- Source systems publish requests or business events into Artemis queues or topics
- Camel Karavan routes consume messages, apply validation and transformation, and invoke target system APIs
- Integration routes emit success, retry, and failure events to dedicated broker destinations
- Operational visibility is provided through Camel health, metrics, and tracing capabilities already present in the runtime configuration

### Design principles

- Use Artemis to reduce direct point-to-point coupling between systems
- Keep canonical payload definitions stable where practical
- Make retry and dead-letter handling explicit
- Separate transport concerns from business mapping logic
- Favor idempotent and observable routes

## Initial Project: Odoo and Aras Integration

The first project in this repository will integrate Odoo and Aras.

### Initial objective

Create a foundation for synchronizing business data and events between:

- Odoo as an ERP/business operations system
- Aras as a PLM/product lifecycle system

### First implementation focus

The first implemented flow will send a part from Aras to Odoo, including the related BOM structure required by the reference process.

### Initial integration pattern

- Aras is the source system for part and BOM data
- A part creation or update event, or a scheduled extraction, publishes the Aras payload into Artemis
- A Camel Karavan route consumes the message, transforms the Aras part model into the Odoo product model, and invokes Odoo APIs
- The integration also creates or updates the corresponding BOM structure in Odoo after the product records are available
- The route emits success, retry, and failure outcomes to dedicated broker destinations
- Reverse flows from Odoo back to Aras are out of scope for the first implementation

### Early scope recommendation

Keep the first implementation focused on one primary business flow: Aras part synchronization into Odoo as a product or item record, with the associated BOM structure created in Odoo as part of the same flow.

This keeps the first implementation aligned with the existing reference behavior while still limiting scope to one source domain before expanding into additional objects such as documents or change-related data.

### Part sync design considerations

- Define the Aras part fields that must map into Odoo product fields
- Define the Aras BOM fields that must map into Odoo BOM records
- Decide whether the trigger is event-driven, scheduled, or both
- Identify the business key used for idempotent upsert behavior in Odoo
- Capture validation failures separately from transport or API failures
- Preserve the original Aras payload for audit and troubleshooting

### BOM sync design considerations

- Confirm whether the route creates BOMs only after all referenced child products exist in Odoo
- Define how parent and child part numbers map to Odoo `product.template` and `product.product` records
- Decide whether BOM creation is create-only or create-or-replace for repeated messages
- Handle BOM ordering and partial failure cases explicitly
- Ensure BOM processing remains traceable to the originating Aras part event

### Reference implementation input

The repository includes a reference folder with the file [Reference/Send Part To Odoo Server.cs](/c:/Repos/TestIntegration-1/Reference/Send%20Part%20To%20Odoo%20Server.cs).

- This Aras method should be treated as a reference for the Aras-to-Odoo part flow
- It contains the required Aras and Odoo fields for the existing send-part and BOM behavior
- It should be used to inform field mapping, payload design, and route behavior in the Camel Karavan project
- It should not be treated as the production implementation for this repository
- The Karavan project should reimplement the integration behavior using the repository's broker-centered architecture

The repository also includes [Reference/docker-compose.yml](/c:/Repos/TestIntegration-1/Reference/docker-compose.yml) as a reference for the Karavan and Artemis deployment topology.

- It shows the expected broker and Karavan container relationship
- It shows the expected Artemis ports `61616` and `8161`
- It shows the Karavan UI port `8080`
- It shows the broker configuration expected by Karavan through `KARAVAN_MQ_BROKERS_*` environment variables

## Messaging Design

Artemis should be the integration backbone for the repo.

### Queue and topic approach

- Inbound queue per source workflow
- Processing queue for normalized integration messages
- Retry queue for transient failures
- Dead-letter queue for unrecoverable failures
- Optional event topics for downstream subscribers and audit-style notifications

### Message expectations

- Include correlation IDs
- Include source system and entity metadata in headers
- Preserve original payload where useful for troubleshooting
- Version message schemas when breaking changes are introduced

## Project Structure Guidance

As the repo grows, each integration project should keep:

- Project-specific Camel Karavan routes
- Project-specific configuration files
- Shared conventions for broker destinations, error handling, and observability
- Documentation for system mappings and operational runbooks

## Non-Functional Requirements

- Reliable delivery through broker-backed processing
- Clear retry and dead-letter behavior
- Environment-specific configuration without route redesign
- Health and metrics enabled by default
- Container-ready deployment artifacts

## Near-Term Next Steps

1. Define the Aras part and BOM payloads and the target Odoo product and BOM fields.
2. Stand up Artemis `2.44` as the shared messaging layer for local development.
3. Create the initial Camel Karavan `4.14.2` integration project structure for Aras-to-Odoo part and BOM sync.
4. Define queue names, message contracts, idempotency rules, and failure-handling conventions.
5. Implement the first end-to-end route for part creation/update and BOM creation in Odoo, then expand iteratively.
