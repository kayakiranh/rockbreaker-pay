# RockBreaker Pay

.NET 8 / C# 12 ile geliştirilmiş, MSSQL + Dapper kullanan modüler monolith dijital wallet örnek projesidir.

> **TR:** Proje öğrenilebilirlik ve finansal işlem izlenebilirliği için bilinçli olarak açık, küçük ve doğrudan kod kullanır.  
> **EN:** The project intentionally favors explicit, small and direct code for learnability and financial traceability.

## Architecture

Ana uygulama tek deploy edilen **modular monolith** yapısındadır:

- Identity / Profile
- Wallet
- Wallet Transfers
- Bank Transfers
- Money Requests
- Payment Instructions
- Campaigns
- Fraud
- Government Bills
- Notifications
- Audit
- Reporting
- Health

Dış sistemler gerçek servisleri simüle eden ayrı process/projelerdir:

- `RockBreaker.FakeBanking.Api`
- `RockBreaker.FakeCampaign.Api`
- `RockBreaker.FakeNotification.Api`
- `RockBreaker.FakeCutoff.Api`
- `RockBreaker.FakeGovernment.Soap`
- `RockBreaker.FakeKyc.Api`

## Core architectural patterns

| Pattern | Kullanıldığı yer |
|---|---|
| Modular Monolith | Ana RockBreaker.Pay API |
| Repository Pattern | Dapper persistence katmanı |
| Dependency Inversion | Repository/client/service interface'leri |
| Application Service | Wallet, identity, transfer, bill, instruction use-case'leri |
| Explicit Mapper | AutoMapper yerine açık mapping |
| Double-Entry Ledger | Wallet-to-wallet transfer |
| Pessimistic Locking | Bakiye değiştiren SQL akışları |
| Unit of Work | Atomik finansal SQL transaction'ları |
| Idempotency | Para transferleri ve fatura ödeme |
| Transactional Outbox | Finansal işlem sonrası notification |
| Saga / Compensation | Bank transferi ve SOAP fatura ödeme |
| Rule Engine | DB tabanlı fraud kuralları |
| Anti-Corruption Layer | Fake REST/SOAP dış servis adapter'ları |
| BackgroundService | Outbox ve otomatik talimat worker'ları |
| Append-Only Audit | Fraud event ve HTTP audit geçmişi |
| CQRS-style Read Model | Regülasyon raporları |

## Technology

- .NET 8
- C# 12 maximum
- ASP.NET Core Web API
- MSSQL
- Dapper
- FluentValidation
- JWT + Refresh Token Rotation
- PBKDF2-SHA256 password hashing
- Serilog console logging
- Elasticsearch searchable audit projection
- CoreWCF fake SOAP government service
- xUnit
- GitHub Actions CI

AutoMapper, Entity Framework ve Swagger UI kullanılmaz.

## OpenAPI

Swagger UI yoktur.

OpenAPI JSON:

```text
GET /openapi/v1.json
```

.NET 8 üzerinde UI eklemeden OpenAPI JSON üretmek için NSwag document generator kullanılır.

## Financial consistency

Bakiye değiştiren kritik işlemler:

1. Wallet row'u `UPDLOCK, ROWLOCK` ile kilitlenir.
2. Bakiye kontrol edilir.
3. Wallet bakiyesi güncellenir.
4. `WalletTransactions` business transaction kaydı yazılır.
5. `LedgerEntries` debit/credit kaydı yazılır.
6. Gerekiyorsa `OutboxMessages` aynı SQL transaction içinde yazılır.
7. SQL transaction commit edilir.

Wallet-to-wallet transferde kaynak ve hedef wallet'lar deterministik sırada kilitlenir; bu deadlock ve double-spend riskini azaltır.

## Fraud

Fraud kuralları hard-code edilmez. `dbo.FraudRules` tablosundan okunur.

Başlangıç kuralları:

- Single transaction amount
- Daily transaction amount
- Daily transaction count

Aksiyonlar:

- Allow
- Reject
- BlockWallet

Her değerlendirme `dbo.FraudEvents` tablosuna:

- WalletId
- Amount
- RiskScore
- Action
- TriggeredRules
- CreatedAtUtc

bilgileriyle kaydedilir.

Admin/Auditor:

```text
GET /api/fraud/rules
```

Admin:

```text
PUT /api/fraud/rules/{ruleId}
```

## Audit logging

HTTP audit bilgileri kalıcı olarak MSSQL `dbo.AuditLogs` tablosuna yazılır.

Elasticsearch **arama/projection** katmanıdır; regulator-grade source of truth MSSQL'dir.

Audit kaydı şunları içerir:

- CorrelationId
- TraceId
- HTTP method
- Path
- Request body
- Response body
- HTTP status
- Response time
- Success/fail
- Client IP
- User-Agent
- UTC timestamp

Password, access token, refresh token, OTP/token alanları ve 16 haneli sayıların orta kısmı merkezi masker ile gizlenir.

## Regulatory reporting

Yalnız `Admin` ve `Auditor` rolleri erişebilir.

JSON:

```text
GET /api/reports/transactions
GET /api/reports/fraud
GET /api/reports/audit
```

CSV:

```text
GET /api/reports/transactions.csv
GET /api/reports/fraud.csv
GET /api/reports/audit.csv
```

Tek sorgu maksimum 366 gün ve 100.000 satır ile sınırlandırılmıştır.

## Main API controllers

- `AuthController`
- `ProfileController`
- `WalletsController`
- `WalletTransfersController`
- `BankTransfersController`
- `MoneyRequestsController`
- `PaymentInstructionsController`
- `CampaignsController`
- `BillsController`
- `FraudRulesController`
- `ReportsController`
- `HealthController`
- `CustomerJourneyController`

## Customer success journey

Register'dan başarılı wallet transferine kadar gerekli endpoint sırası ve örnek success request'leri tek endpoint'ten görülebilir:

```text
GET /api/customer-journey/register-to-transfer
```

Bu endpoint işlem yapmaz; aşağıdaki gerçek akışı dokümante eder:

```text
Register
  -> KYC Verify
  -> Create Wallet
  -> Open Fake Bank Account
  -> Bank to Wallet
  -> Check Balance
  -> Wallet to Wallet Transfer
```

Register cevabı access token ürettiği için ilk journey'de ayrıca login zorunlu değildir. Login, sonraki oturum için opsiyonel adım olarak gösterilir.

## Fake service responsibilities

### FakeBanking

- Fake bank account creation
- Bank movements
- Bank → Wallet
- Wallet → Bank
- Saga compensation/reversal

### FakeCampaign

Dummy company campaign list.

### FakeNotification

- SMS
- E-mail
- Push notification

### FakeCutoff

- Europe/Istanbul timezone
- Working hours
- Weekend/holiday rules
- Wallet-to-wallet remains available 24/7

### FakeKyc

Harici KYC sağlayıcısını simüle eder:

- `POST /api/kyc/verifications`
- `GET /api/kyc/verifications/{verificationId}`
- normal kullanıcı verisi -> `Verified`
- `@manual.local` e-posta domaini -> `Pending`
- `@reject.local` e-posta domaini -> `Rejected`

Ana API'deki `POST /api/kyc/submit` bu servisi çağırır. Provider sonucu `dbo.KycEvents` audit geçmişine provider verification ID ve gerekçesiyle yazılır. `Pending` sonuçlar Admin review akışına devam eder.

### FakeGovernment SOAP

CoreWCF SOAP endpoint:

```text
/GovernmentService.svc
```

Operations:

- `GetBills`
- `PayBill`

## Identity

Supported flows:

- Register
- Login
- JWT access token
- Refresh token rotation
- Forgot password
- Reset password
- Profile update
- Notification preferences
- KYC status field
- Role field

Production deployments must override `Jwt:Key` with a secret manager/environment variable.

## Money requests

Lifecycle:

```text
Pending
  -> Accepted
  -> Rejected
  -> Cancelled
  -> Expired
```

Accept operation reuses the same wallet transfer application service.

## Automatic payment instructions

Frequencies:

- Once
- Daily
- Weekly
- Monthly

States:

- Active
- Paused
- Cancelled
- Completed
- Failed

`.NET BackgroundService` processes due instructions. No Hangfire or RabbitMQ is required for this monolith version.

## Notifications

Financial wallet-to-wallet transactions write `WalletTransferCompleted` to `OutboxMessages`.

`OutboxNotificationWorker`:

1. Reads unprocessed messages.
2. Resolves wallet owners.
3. Honors SMS/E-mail/Push preferences.
4. Calls FakeNotification.
5. Marks the message processed.
6. On failure, increments retry count and stores `LastError`.

## Health probes

```text
GET /health/live
GET /health/ready
```

- `live`: process check
- `ready`: MSSQL + Elasticsearch active dependency check

## Local infrastructure

Start MSSQL and Elasticsearch:

```bash
docker compose up -d
```

Default development ports:

| Component | Port |
|---|---:|
| RockBreaker.Pay.Api | 5100 |
| FakeBanking | 5101 |
| FakeCampaign | 5102 |
| FakeNotification | 5103 |
| FakeCutoff | 5104 |
| FakeGovernment SOAP | 5105 |
| FakeKyc | 5106 |
| MSSQL | 1433 |
| Elasticsearch | 9200 |

Run projects:

```bash
dotnet run --project src/RockBreaker.Pay.Api --urls http://localhost:5100
dotnet run --project src/RockBreaker.FakeBanking.Api --urls http://localhost:5101
dotnet run --project src/RockBreaker.FakeCampaign.Api --urls http://localhost:5102
dotnet run --project src/RockBreaker.FakeNotification.Api --urls http://localhost:5103
dotnet run --project src/RockBreaker.FakeCutoff.Api --urls http://localhost:5104
dotnet run --project src/RockBreaker.FakeGovernment.Soap --urls http://localhost:5105
dotnet run --project src/RockBreaker.FakeKyc.Api --urls http://localhost:5106
```

## Database

EF Core migration kullanılmaz.

SQL scriptleri sıra ile uygulanır:

```text
database/scripts/001_core_schema.sql
database/scripts/002_fraud_rules.sql
database/scripts/003_identity.sql
database/scripts/004_wallet_user_relation.sql
database/scripts/005_money_requests.sql
database/scripts/006_payment_instructions.sql
database/scripts/007_fraud_events.sql
database/scripts/008_payment_instruction_reliability.sql
database/scripts/009_kyc_events.sql
database/scripts/010_kyc_provider_reference.sql
```

Yeni DB değişikliklerinde mevcut script değiştirilmez; yeni sıra numaralı script eklenir.

## Build and test

```bash
dotnet restore RockBreaker.Pay.sln
dotnet build RockBreaker.Pay.sln --configuration Release
dotnet test RockBreaker.Pay.sln --configuration Release
```

GitHub Actions her `main` push ve pull request için restore/build/test çalıştırır.

## Commit convention

Repository Conventional Commits mantığını kullanır:

```text
feat(wallet): ...
feat(fraud): ...
fix(build): ...
fix(security): ...
test(wallet): ...
docs: ...
chore: ...
```

Her commit tek mantıksal değişiklik grubunu taşımalıdır.

## Security notes

- Production secret'ları source control'e koymayın.
- JWT key production'da environment/secret manager üzerinden verilmelidir.
- Audit sisteminde password/token/OTP tutulmaz.
- Wallet bakiyesini doğrudan SQL ile değiştirmeyin; ledger-aware application flow kullanılmalıdır.
- Financial POST endpoint'lerinde `Idempotency-Key` kullanın.
- Regülasyon rapor endpoint'lerini yalnız yetkili rollere açın.
