# Economy Portfolio — API Reference

Base URL: `https://<railway-domain>/api`

All protected endpoints require a `Bearer` token in the `Authorization` header.

---

## Auth

### GET /auth/me
Returns the profile of the currently authenticated user. Used by the frontend to restore session on page load.

**Auth:** required
**Response 200:** `{ id, name, email, emailVerified }`
**Response 401:** token missing, expired, or invalid
**Response 404:** user ID from token no longer exists

---

### POST /auth/register
Creates a new account and sends a 6-digit verification code by email. The account is unverified until the code is submitted to `POST /auth/verify-code` with `type: 1`. A BRL wallet is auto-created for the user.

**Auth:** none (rate-limited: 10 req/min per IP)
**Body:**
```json
{ "name": "Erick", "email": "user@email.com", "password": "Senha123" }
```
Password rules: 8–100 chars, at least one uppercase, one lowercase, one digit.
**Response 200:** `{ message: "..." }`
**Response 409:** email already registered

---

### POST /auth/login
Validates credentials and sends a 2FA code by email. The JWT is only returned after the code is submitted to `POST /auth/verify-code` with `type: 0`.

**Auth:** none (rate-limited)
**Body:**
```json
{ "email": "user@email.com", "password": "Senha123" }
```
**Response 200:** `{ message: "..." }`
**Response 401:** wrong email/password, or email not verified yet

---

### POST /auth/verify-code
Validates the 6-digit code sent by email and returns the JWT. This is step 2 for both registration and login.

**Auth:** none (rate-limited)
**Body:**
```json
{ "email": "user@email.com", "code": "482910", "type": 1 }
```

| type | When to use |
|------|-------------|
| 0    | After `POST /login` |
| 1    | After `POST /register` |
| 2    | Not used here — use `POST /reset-password` |

On success with `type: 1`: marks email as verified.
On success with `type: 0`: returns JWT + refresh token immediately.
**Response 200:** `{ token, refreshToken }`
**Response 401:** code wrong, already used, or expired (10-minute window)

---

### POST /auth/forgot-password
Sends a password reset code to the email. If the email is not registered, response is identical to success (prevents user enumeration).

**Auth:** none (rate-limited)
**Body:**
```json
{ "email": "user@email.com" }
```
**Response 200:** `{ message: "..." }`

---

### POST /auth/reset-password
Resets the password using the code received from `POST /forgot-password`. On success the user can log in with the new password.

**Auth:** none (rate-limited)
**Body:**
```json
{ "email": "user@email.com", "code": "193847", "newPassword": "NovaSenha456" }
```
**Response 200:** `{ message: "..." }`
**Response 401:** code wrong, already used, or expired

---

## Assets

The asset catalog is global — it represents tradeable instruments (stocks, crypto, ETFs), not individual holdings. Holdings are tracked in Positions.

### GET /assets
Returns the full list of assets. Used to populate search/select fields in the frontend.

**Auth:** required
**Response 200:** `[{ id, symbol, name, type, currency, currentPrice, createdAt }]`

---

### GET /assets/{id}
Returns a single asset by its GUID.

**Auth:** required
**Response 200:** asset object
**Response 404:** asset not found

---

### GET /assets/symbol/{symbol}
Returns a single asset by ticker symbol (e.g. `AAPL`, `BTC`). Case-insensitive.

**Auth:** required
**Response 200:** asset object
**Response 404:** symbol not found

---

### POST /assets
Registers a new asset in the global catalog. Once created, any user can trade it.

**Auth:** required
**Body:**
```json
{ "symbol": "AAPL", "name": "Apple Inc.", "type": "stock", "currency": "USD", "currentPrice": 213.49 }
```
**Response 201:** asset object (with `Location` header pointing to `GET /assets/{id}`)
**Response 409:** symbol already exists

---

### PUT /assets/{id}/price
Updates the current market price of an asset. Does not create a transaction — only refreshes the reference price used for P&L calculation.

In production this would be called by a background job pulling from a market data provider. For now, the frontend sends the price before buy/sell operations.

**Auth:** required
**Body:**
```json
{ "currentPrice": 220.00 }
```
**Response 200:** updated asset object
**Response 404:** asset not found

---

## Wallets

A wallet represents a balance in a specific currency (BRL, USD, BTC, etc.). Each user can hold one wallet per currency. A BRL wallet is auto-created on registration.

### GET /wallets
Returns all wallets for the authenticated user, ordered by currency.

**Auth:** required
**Response 200:** `[{ id, currency, balance, createdAt }]`

---

### GET /wallets/{id}
Returns a single wallet by GUID. Only returns wallets belonging to the authenticated user.

**Auth:** required
**Response 200:** wallet object
**Response 404:** wallet not found or belongs to another user

---

### GET /wallets/currency/{currency}
Returns the wallet for a specific currency (e.g. `USD`). Useful for checking balance before a buy or convert operation.

**Auth:** required
**Response 200:** wallet object
**Response 404:** user has no wallet for that currency

---

### POST /wallets
Creates a new wallet for the authenticated user in the given currency. BRL is auto-created on registration.

**Auth:** required
**Body:**
```json
{ "currency": "USD" }
```
**Response 201:** wallet object
**Response 409:** user already has a wallet for that currency

---

## Positions

Positions represent the user's open investment holdings. They are created/updated automatically on buy, and reduced/removed on sell. You cannot create or delete positions directly.

Key fields:
- `averagePrice`: weighted average cost of all purchases of that asset
- `currentValue`: quantity × asset's current market price
- `profitLoss`: currentValue − totalInvested

### GET /positions
Returns all open positions for the authenticated user, with real-time P&L. Returns `[]` if no assets have been bought.

**Auth:** required
**Response 200:** `[{ id, assetId, assetSymbol, quantity, averagePrice, totalInvested, currentValue, profitLoss, profitLossPercentage }]`

---

### GET /positions/summary
Returns a consolidated portfolio summary for the dashboard. Aggregates all positions and wallet balances.

Includes:
- `totalInvested`, `totalCurrentValue`, `totalProfitLoss`, `totalProfitLossPercentage`
- `walletBalances`: list of wallets with balance (for pie chart)
- `assetAllocations`: each position's weight in the portfolio (for pie chart)

**Auth:** required
**Response 200:** portfolio summary object

---

### GET /positions/{id}
Returns a single position by GUID with real-time P&L. Only returns positions belonging to the authenticated user.

**Auth:** required
**Response 200:** position object
**Response 404:** position not found or belongs to another user

---

## Transactions

Every money movement goes through this controller and generates an immutable record.

Transaction types:
- `DEPOSIT` — adds BRL to the BRL wallet (entry point for all capital)
- `CONVERSION` — exchanges balance between two of the user's wallets
- `BUY` — purchases an asset, debiting the wallet and creating/updating a position
- `SELL` — sells an asset, crediting the wallet and reducing/removing a position

### GET /transactions
Returns the authenticated user's transaction history. All query params are optional; results are ordered by most recent first.

**Auth:** required
**Query params:** `type`, `currency`, `assetId` (GUID), `fromDate` (UTC), `toDate` (UTC)
**Response 200:** `[{ id, type, total, currency, quantity, price, transactionAt }]`

---

### GET /transactions/summary
Returns aggregated transaction data for the dashboard charts. Accepts the same optional filters as `GET /transactions`.

Includes:
- `totalDeposits`, `totalBuys`, `totalSells`, `totalConversions`
- `byType`: count and total amount per type (for bar/pie charts)
- `monthlyHistory`: month-by-month breakdown (for line charts)

**Auth:** required
**Response 200:** summary object

---

### GET /transactions/{id}
Returns a single transaction by GUID. Only returns transactions belonging to the authenticated user.

**Auth:** required
**Response 200:** transaction object
**Response 404:** not found or belongs to another user

---

### POST /transactions/deposit
Deposits BRL into the user's BRL wallet. This must be done before any buy or conversion.

**Auth:** required
**Body:**
```json
{ "amount": 1000.00 }
```
**Response 201:** transaction object
**Response 400:** amount ≤ 0 or BRL wallet not found

---

### POST /transactions/convert
Converts an amount from one wallet to another (e.g. BRL → USD). The exchange rate must be provided by the frontend using a live market quote.

**Auth:** required
**Body:**
```json
{ "fromCurrency": "BRL", "toCurrency": "USD", "amount": 500.00, "exchangeRate": 5.75 }
```
Rules: `fromCurrency ≠ toCurrency`, both wallets must exist, source balance must be sufficient.
**Response 201:** transaction object
**Response 400:** validation error or insufficient balance

---

### POST /transactions/buy
Purchases a quantity of an asset using the wallet that matches the asset's currency. Debits the wallet and creates/updates the position. If the user already holds this asset, the average price is recalculated.

**Auth:** required
**Body:**
```json
{ "assetId": "uuid", "quantity": 2.5, "price": 213.49 }
```
Rules: asset must exist in the catalog, wallet in asset's currency must have sufficient balance.
**Response 201:** transaction object
**Response 400:** validation error or insufficient balance

---

### POST /transactions/sell
Sells a quantity of an asset from the user's position. Credits the wallet and reduces the position. If the entire quantity is sold, the position is removed.

**Auth:** required
**Body:**
```json
{ "assetId": "uuid", "quantity": 1.0, "price": 220.00 }
```
Rules: user must have an open position for the asset, quantity sold cannot exceed quantity held.
**Response 201:** transaction object
**Response 400:** no position found or insufficient quantity

---

## Error responses

All endpoints return errors in the format:
```json
{ "message": "Description of the error" }
```

| Status | Meaning |
|--------|---------|
| 400    | Validation error or business rule violation |
| 401    | Not authenticated or credentials invalid |
| 404    | Resource not found |
| 409    | Conflict (duplicate email, symbol, wallet currency) |
| 429    | Rate limit exceeded (auth endpoints) |
| 500    | Internal server error |
