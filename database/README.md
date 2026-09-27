# RockBreaker.Pay Database

TR: Veritabanı değişiklikleri EF migration yerine sürümlü MSSQL scriptleriyle yönetilir. Scriptler numara sırasıyla uygulanır.

EN: Database changes are managed through versioned MSSQL scripts instead of EF migrations. Apply scripts in numeric order.

## Architecture

- Dapper is the data-access technology.
- Balance-changing operations use explicit SQL transactions.
- Wallet rows use `UPDLOCK, ROWLOCK` during mutations to reduce double-spend races.
- Financial history is represented by transaction headers and immutable ledger entries.
- `AuditLogs` is the durable regulatory audit store. Elasticsearch is a searchable secondary log destination.
- `OutboxMessages` supports reliable side effects without coupling financial commits to external systems.
