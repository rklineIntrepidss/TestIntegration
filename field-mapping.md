# Field Mapping: Aras Part and BOM to Odoo

## Purpose

This document captures the field mapping implied by the reference method [Reference/Send Part To Odoo Server.cs](/c:/Repos/TestIntegration-1/Reference/Send%20Part%20To%20Odoo%20Server.cs). It should be used as the baseline mapping for the Camel Karavan implementation.

The reference method is the source of truth for the current field set, but the Karavan project should implement the mapping through the repository's broker-centered integration architecture.

## Scope

This mapping covers:

- Aras part data sent to Odoo product records
- Aras BOM relationships sent to Odoo BOM records
- Required lookup and configuration fields used by the integration

## Source Trigger Input

The reference method starts with:

- Aras method input property: `partId`

This is used to load:

- the selected parent part
- all related child BOM records through `Part BOM`
- the repeated part hierarchy returned by `GetItemRepeatConfig`

## Aras Configuration Mapping

The reference method reads Odoo connection settings from Aras item type `ISS_Aras_Odoo_Connector`.

| Aras configuration field | Purpose |
| --- | --- |
| `iss_odoo_url` | Odoo JSON-RPC endpoint URL |
| `iss_odoo_database` | Odoo database name |
| `iss_odoo_api_key` | Odoo API key or password token |
| `iss_odoo_temp_folder` | Temporary folder used for image extraction |
| `iss_odoo_user_id` | Odoo user ID for API calls |

## Part Field Mapping

### Aras fields read by the reference method

| Aras field | Usage in reference |
| --- | --- |
| `item_number` | Primary part identifier and Odoo lookup key |
| `name` | Product display name |
| `description` | Product sales description |
| `classification` | Read from Aras but not currently sent to Odoo |
| `make_buy` | Read from Aras but not currently sent to Odoo |
| `thumbnail` | Optional image file reference for product image upload |

### Odoo product target

The reference method creates records in:

- Odoo model: `product.template`

### Implemented part field mapping

| Aras source | Odoo target | Notes |
| --- | --- | --- |
| `item_number` | `default_code` | Primary business key used by the reference for later lookups |
| `name` | `name` | Product name |
| `description` | `description_sale` | Sales description in Odoo |
| `thumbnail` file bytes | `image_1920` | File is checked out from Aras, read from disk, then base64 encoded |

### Reference payload shape for product create

| Odoo field | Value source |
| --- | --- |
| `default_code` | Aras `item_number` |
| `name` | Aras `name` |
| `description_sale` | Aras `description` |
| `image_1920` | Base64-encoded thumbnail file when available |

## BOM Field Mapping

### Aras BOM fields read by the reference method

| Aras relationship or field | Usage in reference |
| --- | --- |
| `Part BOM.related_id` | Identifies the child part |
| Child `item_number` | Used to locate the child product in Odoo |
| `Part BOM.quantity` | Quantity used in Odoo BOM line |

### Odoo BOM target

The reference method creates records in:

- Odoo model: `mrp.bom`

It also resolves child product variants from:

- Odoo model: `product.product`

### Implemented BOM mapping

| Aras source | Odoo target | Notes |
| --- | --- | --- |
| Parent part `item_number` | `mrp.bom.product_tmpl_id` lookup input | Parent template located by `product.template.default_code` |
| Child part `item_number` | `bom_line_ids[].product_id` lookup input | Child variant located by `product.product.default_code` |
| `Part BOM.quantity` | `bom_line_ids[].product_qty` | Quantity per child line |

### Reference payload shape for BOM create

| Odoo field | Value source |
| --- | --- |
| `product_tmpl_id` | Lookup of parent `product.template.id` by parent `item_number` |
| `product_id` | `false` |
| `type` | `"normal"` |
| `product_qty` | `1.0` |
| `product_uom_id` | Read from parent `product.template.uom_id` |
| `bom_line_ids` | Derived from Aras child parts and quantities |

### BOM line payload shape

Each BOM line is created as:

- Odoo command tuple: `[0, 0, {...}]`

With fields:

| Odoo BOM line field | Value source |
| --- | --- |
| `product_id` | Lookup of child `product.product.id` by child `item_number` |
| `product_qty` | Aras `Part BOM.quantity` |

## Odoo Lookup Rules

The reference method depends on these Odoo lookups:

| Purpose | Odoo model | Search field |
| --- | --- | --- |
| Parent product template lookup | `product.template` | `default_code` |
| Child product variant lookup | `product.product` | `default_code` |
| Parent UoM lookup | `product.template` | `uom_id` via `read` |

## Processing Rules Implied by the Reference

- The integration creates product records before creating BOM records.
- BOM creation depends on all referenced child products already existing in Odoo.
- `item_number` is the operational key used across create and lookup logic.
- Product creation in the reference is create-only; it does not currently perform an explicit update path.
- BOM creation in the reference is create-only; it does not currently replace an existing BOM.

## Fields Present but Not Yet Mapped to Odoo

The reference reads these Aras fields but does not send them to Odoo in the current payload:

| Aras field | Current status |
| --- | --- |
| `classification` | Available for future mapping |
| `make_buy` | Available for future mapping |

The reference also passes a `partType` argument into `CreateOdooProduct`, but it is not used in the Odoo payload.

## Recommended Karavan Mapping Decisions

- Preserve `item_number` as the canonical product key unless a better business key is explicitly approved.
- Carry both part data and BOM data in the normalized message so the product and BOM routes stay correlated.
- Distinguish between create-only reference behavior and any future upsert behavior added in Karavan.
- Treat missing child products, failed image extraction, and failed Odoo lookups as explicit error cases.
- Keep the original Aras payload available for troubleshooting and replay.

## Open Questions

- Should the Karavan flow remain create-only to match the reference, or implement product and BOM upsert behavior?
- Should `classification` and `make_buy` be mapped to custom Odoo fields?
- Should image handling remain file-based, or be normalized earlier in the integration flow?
- Should BOM reprocessing replace existing BOM lines or create a new BOM revision strategy?
