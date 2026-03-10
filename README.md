# TestIntegration

This repository contains the initial scaffold for an Aras-to-Odoo integration built with Camel Karavan `4.14.2` and ActiveMQ Artemis `2.44`.

Current repository assets:

- `aras-odoo-sync/` as the first Camel integration project scaffold
- `design.md`, `execution-plan.md`, and `field-mapping.md` for design and implementation guidance
- `Reference/` for source reference artifacts used to shape the Karavan implementation

Deployment is expected to happen through the existing dev environment already connected to this Git repository. This repository is intended to hold the Karavan project source rather than a separate local runtime stack.

Primary project files:

- `aras-odoo-sync/application.properties`
- `aras-odoo-sync/aras-part-bom-sync.camel.yaml`
- `Reference/Send Part To Odoo Server.cs`
- `Reference/docker-compose.yml`
