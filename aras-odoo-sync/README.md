# Aras Odoo Sync

Initial Camel Karavan project scaffold for the Aras-to-Odoo part and BOM integration.

Current scope:

- consume normalized Aras part and BOM messages from Artemis
- split product and BOM processing into separate route stages
- provide placeholders for Odoo product and BOM delivery

Runtime assumption:

- deployment and execution happen in the existing dev Karavan environment connected to this repository
- this project folder is the source scaffold for that environment
- no separate local compose stack is required in this repository

Reference inputs:

- `../Reference/Send Part To Odoo Server.cs`
- `../Reference/docker-compose.yml`
