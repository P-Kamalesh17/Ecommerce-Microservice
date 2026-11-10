# ECommerce API Gateway: Frontend Integration Docs (TSX)

**Base URL (every request goes through the gateway):** `https://localhost:7000`
Never call the services directly (7200, 7004, 7300, ...).

Field names below are the exact JSON spellings (case-sensitive, camelCase). Copy them as written.
Ready-made types: `src/types/api.ts`. Ready-made client: `src/lib/api.ts`.

---

## 0. Conventions

### Headers
| Case | Headers |
|---|---|
| Public JSON request | `Content-Type: application/json` |
| Protected JSON request | `Content-Type: application/json` and `Authorization: Bearer <token>` |
| Protected request with no body (GET/DELETE) | `Authorization: Bearer <token>` |
| File upload (create/update product) | `Authorization: Bearer <token>` only. Do NOT set `Content-Type`; the browser sets the multipart boundary. |

### Common types
```ts
type ApiError = { message: string };           // body of every 4xx/5xx
type MessageResponse = { message: string };    // body of simple success replies
// Dates are ISO 8601 strings, e.g. "2026-10-31T10:15:00Z"
```

### Status codes
`200` OK, `201` Created, `400` bad input or business rule, `401` missing/invalid/expired token or wrong login, `403` wrong role, `404` not found, `500` server error.

### Roles
`type UserRole = "Admin" | "Customer" | "Shipment"`. The gateway only checks that the token is valid; each service checks the role (403 if it does not match).

### Token and user id
- **The frontend never sends a userId.** For every endpoint the backend reads the user from the token (`sub` claim). Just send `Authorization: Bearer <token>`.
- Login gives `token` (expires in about 15 min) and `refreshToken` (7 days).
- **The access token is encrypted (JWE) and opaque.** It has 5 dot-separated parts and its contents cannot be read in the browser. **Do not decode it** (no `jwt-decode`, no `atob`); store it and send it back as `Authorization: Bearer <token>`.
- Login, register and refresh also return `userId`, `email` and `roles` next to the token. Use those for the UI (for example `roles.includes("Admin")`), or call `GET /api/Authentication/me`. They are for display only: the server decides what you may do from the token, never from these fields.
- A plain (unencrypted) signed token is rejected with `401`.
- On a `401`, call `/api/Authentication/refresh` once and retry. If that fails, go to the login page.

### Enums
```ts
// SENT as a NUMBER in PUT /api/Order/{id}/status
const OrderStatusCode = { Pending: 1, Paid: 2, Processing: 3, Shipped: 4, Delivered: 5, Cancelled: 6 } as const;
// RECEIVED as a STRING in order.orderStatus
type OrderStatusName = "Pending" | "Paid" | "Processing" | "Shipped" | "Delivered" | "Cancelled";
// RECEIVED as a STRING in payment.status
type PaymentStatus = "Pending" | "Success" | "Failed" | "Refunded";
```

---

## 1. Authentication (public, no Authorization header)

### 1.1 Register
`POST /api/Authentication/register`
```ts
interface RegisterRequest {
  email: string;            // required, valid email
  password: string;         // required
  fullName: string;         // required, max 100
  phoneNumber?: string | null; // optional, valid phone number, max 20
  address?: string | null;  // optional, max 500
}
```
```json
{ "email": "user@mail.com", "password": "Pass@12345", "fullName": "John Doe", "phoneNumber": "+91 98765 43210", "address": "12 Street, Chennai" }
```
Response `201`:
```ts
interface AuthResult {
  token: string;          // encrypted JWE (5 parts), opaque: do not decode
  refreshToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  roles: string[];        // e.g. ["Customer"]; for UI only
}
```
```json
{ "token": "eyJhbGciOiJkaXIiLCJlbmMiOiJBMjU2Q0JDLUhTNTEyIi...", "refreshToken": "q3Zk...", "expiresAt": "2026-11-01T10:15:00Z",
  "userId": "9f1c2a...", "email": "user@mail.com", "roles": ["Customer"] }
```
Errors: `400 { "message": "A user with this email already exists." }` (or password rule text).

### 1.2 Login
`POST /api/Authentication/login`
```ts
interface LoginRequest { email: string; password: string; }   // email, NOT userId/username
```
```json
{ "email": "user@mail.com", "password": "Pass@12345" }
```
Response `200`: `AuthResult` (same JSON as above).
Errors: `401 { "message": "Invalid email or password." }` or `{ "message": "Account is temporarily locked. Please try again later." }`.

### 1.3 Refresh token
`POST /api/Authentication/refresh`
```ts
interface RefreshTokenRequest { refreshToken: string; }
```
```json
{ "refreshToken": "q3Zk..." }
```
Response `200`: `AuthResult`. Errors: `401 { "message": "Invalid refresh token." }` or `"Refresh token has expired. Please log in again."`.

### 1.4 Logout
`POST /api/Authentication/logout` (body is `RefreshTokenRequest`)
Response `200`: `{ "message": "Logged out successfully." }`

### 1.5 My profile (needs `Authorization: Bearer <token>`)
`GET /api/Authentication/me`. No body, no userId: the user comes from the token.
```ts
interface UserProfile {
  id: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  address: string | null;   // the default shipping address used by checkout
}
```
```json
{ "id": "9f1c...", "email": "user@mail.com", "fullName": "John Doe", "phoneNumber": "+91 98765 43210", "address": "12 Street, Chennai" }
```
Errors: `401` (missing/invalid token).

---

## 2. Category

```ts
interface Category { categoryId: number; categoryName: string; }
interface CategoryRequest { categoryName: string; }   // required, max 100
```

| Method | URL | Auth | Request body | Response |
|---|---|---|---|---|
| GET | `/api/Category/get-all-category` | No | none | `200` `Category[]` |
| GET | `/api/Category/get-category-by-id/{categoryid}` | No | none | `200` `Category`, `404` |
| POST | `/api/Category/create-category` | Bearer | `CategoryRequest` | `201` `Category` |
| PUT | `/api/Category/update-category/{id}` | Bearer | `CategoryRequest` | `200` `Category`, `404` |
| DELETE | `/api/Category/delete-category-by-id/{id}` | Bearer | none | `200` `MessageResponse`, `404` |

Response example (`GET get-all-category`):
```json
[ { "categoryId": 1, "categoryName": "Shoes" }, { "categoryId": 2, "categoryName": "Shirts" } ]
```
Request example (create / update): `{ "categoryName": "Shoes" }`

---

## 3. Product

```ts
interface Product {
  id: number;
  name: string;
  description: string | null;
  imageUrl: string;            // relative path, e.g. "/images/abc.jpg"
  price: number;
  stockQuantity: number;
  createdAt: string;
  updatedAt: string | null;
  categoryId: number;
  categoryName: string;
}
```
```json
{ "id": 1, "name": "Nike Air", "description": "Running shoe", "imageUrl": "/images/abc.jpg",
  "price": 4999.0, "stockQuantity": 20, "createdAt": "2026-10-31T09:00:00Z", "updatedAt": null,
  "categoryId": 2, "categoryName": "Shoes" }
```
Image in TSX: `<img src={"https://localhost:7000" + product.imageUrl} />` (or `imageSrc(product.imageUrl)`).

| Method | URL | Auth | Request | Response |
|---|---|---|---|---|
| GET | `/api/Product/get-all-products` | No | none | `200` `Product[]` |
| GET | `/api/Product/get-product-by-id/{id}` | No | none | `200` `Product`, `404 { message: "Product not found." }` |
| GET | `/api/Product/get-by-category/{categoryId}` | No | none | `200` `Product[]` |
| GET | `/api/Product/filter?minPrice=100&maxPrice=5000&categoryId=2` | No | query: `minPrice?: number`, `maxPrice?: number`, `categoryId?: number` | `200` `Product[]` |
| POST | `/api/Product/create-product` | Bearer | form-data (below) | `201` `Product` |
| PUT | `/api/Product/update-product-by-id/{id}` | Bearer | form-data (below) | `200` `Product`, `404` |
| DELETE | `/api/Product/delete-product-by-id/{id}` | Bearer | none | `200 { "message": "Product deleted successfully." }`, `404` |
| POST | `/api/Product/reserve-stock` | Bearer (Admin/Shipment) | `StockItem[]` | `200 { "message": "Stock reserved." }` |
| POST | `/api/Product/release-stock` | Bearer (Admin/Shipment) | `StockItem[]` | `200 { "message": "Stock released." }` |

### Create / update product (multipart/form-data)
Field names are PascalCase here (this is the one place that is not camelCase):

| Form field | Datatype | Rule |
|---|---|---|
| `Name` | string | required, max 200 |
| `Description` | string | optional, max 1000 |
| `CategoryId` | number | required, 1 or more |
| `Price` | number | 0 or more |
| `StockQuantity` | number | 0 or more |
| `ImageFile` | File | optional |

```tsx
const fd = new FormData();
fd.append("Name", name);
fd.append("Description", description);
fd.append("CategoryId", String(categoryId));
fd.append("Price", String(price));
fd.append("StockQuantity", String(stockQuantity));
if (file) fd.append("ImageFile", file);
await fetch("https://localhost:7000/api/Product/create-product", {
  method: "POST",
  headers: { Authorization: `Bearer ${token}` },   // no Content-Type
  body: fd,
});
```

`reserve-stock` and `release-stock` are used by the Order service (with a secret internal key), not by the frontend:
```ts
interface StockItem { productId: number; quantity: number; }   // both 1 or more
```
```json
[ { "productId": 1, "quantity": 2 } ]
```

---

## 4. Cart (all routes need `Authorization: Bearer <token>`; the cart is always the logged-in user's)

```ts
interface CartItem {
  id: number;            // this is the cartItemId used in update/remove
  productId: number;
  productName: string;
  imageUrl: string;
  unitPrice: number;
  quantity: number;
  subtotal: number;
}
interface Cart {
  id: number;
  userId: string;
  createdAt: string;
  updatedAt: string | null;
  items: CartItem[];
}
interface CartSummary {
  subtotal: number;
  discountAmount: number;
  tax: number;
  shippingCost: number;
  totalAmount: number;
}
interface AddCartItemRequest { productId: number; quantity: number; }  // productId >= 1, quantity 1..100. No userId: it comes from the token
interface UpdateCartItemRequest { quantity: number; }                                   // 1..100
```

| Method | URL | Request body | Response |
|---|---|---|---|
| GET | `/api/Cart` | none | `200` `Cart`, `404 { message: "Cart not found for this user." }` |
| GET | `/api/Cart/summary` | none | `200` `CartSummary` |
| POST | `/api/Cart/items` | `AddCartItemRequest` | `200 { "message": "Item added to cart." }` |
| PUT | `/api/Cart/items/{cartItemId}` | `UpdateCartItemRequest` | `200 { "message": "Cart item updated." }` |
| DELETE | `/api/Cart/items/{cartItemId}` | none | `200 { "message": "Item removed from cart." }` |
| DELETE | `/api/Cart` | none | `200 { "message": "Cart cleared." }` |

Request example (add item): `{ "productId": 1, "quantity": 2 }`
Request example (update item): `{ "quantity": 3 }`

Response example (`GET /api/Cart`):
```json
{ "id": 1, "userId": "9f1c...", "createdAt": "2026-10-31T09:00:00Z", "updatedAt": null,
  "items": [ { "id": 5, "productId": 1, "productName": "Nike Air", "imageUrl": "/images/abc.jpg",
               "unitPrice": 4999, "quantity": 2, "subtotal": 9998 } ] }
```
Response example (`summary`):
```json
{ "subtotal": 9998, "discountAmount": 0, "tax": 0, "shippingCost": 0, "totalAmount": 9998 }
```
Old frontend paths `/api/Cart/add-item`, `/update-item/{id}`, `/remove-item/{id}`, `/get-cart` and `/api/Cart/{userId}` no longer exist. Updating or removing an item that is not in your own cart returns `404 { "message": "Invalid cart item ID." }`.

---

## 5. Orders (all routes need `Authorization: Bearer <token>`)

```ts
interface OrderItem {
  id: number;
  orderId: number;
  productId: number;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}
interface Order {
  id: number;
  userId: string;
  orderStatus: OrderStatusName;   // string, e.g. "Pending"
  shippingAddress: string;
  totalAmount: number;
  createdAt: string;
  updatedAt: string | null;
  paymentId: number | null;       // ONLY filled by GET my-orders. null = no payment made yet (still Pending)
  paymentStatus: PaymentStatus | null;   // ONLY by GET my-orders: "Pending" (no payment yet) | "Success" | "Failed" | "Refunded"
  items: OrderItem[];
}
interface CheckoutRequest { shippingAddress?: string | null; }  // OPTIONAL, max 1000. Empty/omitted = use the address saved on the profile
interface UpdateOrderStatusRequest { status: number; }          // 1..6, see OrderStatusCode
```
```json
{ "id": 10, "userId": "9f1c...", "orderStatus": "Pending", "shippingAddress": "12 Street, Chennai",
  "totalAmount": 9998, "createdAt": "2026-10-31T09:30:00Z", "updatedAt": null,
  "paymentId": null, "paymentStatus": "Pending",
  "items": [ { "id": 1, "orderId": 10, "productId": 1, "productName": "Nike Air", "quantity": 2, "unitPrice": 4999, "subtotal": 9998 } ] }
```

| Method | URL | Role | Request | Response |
|---|---|---|---|---|
| POST | `/api/Order/checkout` | Customer | `CheckoutRequest` (the body may be omitted or `{}`) | `201` `Order` with status `Pending`. Built from the user's cart, which is then emptied. **No payment is created.** |
| GET | `/api/Order/my-orders` | Customer | none | `200` `Order[]`, each with `paymentId` / `paymentStatus` |
| GET | `/api/Order/get-all-orders` | Admin, Shipment | optional query `?orderStatus=1` (number 1..6) | `200` `Order[]` |
| GET | `/api/Order/get-order-by-id/{id}` | owner or staff | none | `200` `Order`, `404` |
| GET | `/api/Order/{orderId}/items` | owner or staff | none | `200` `OrderItem[]` |
| GET | `/api/Order/items/{id}` | owner or staff | none | `200` `OrderItem`, `404` |
| PUT | `/api/Order/{id}/approve` | Admin, Shipment | none | `200` `Order` (status becomes Processing; a `Paid` order already holds its stock, a `Pending` one takes it now) |
| PUT | `/api/Order/{id}/status` | Admin, Shipment | `UpdateOrderStatusRequest` | `200` `Order` |
| PUT | `/api/Order/{id}/cancel` | Admin, Customer | none | `200` `Order` (stock returned if the order was `Paid` or `Processing`) |
| DELETE | `/api/Order/{id}` | Admin | none | `200 { "message": "Order deleted successfully." }`, `404` |

Request example (status): `{ "status": 4 }` (Shipment role may only send 4 Shipped or 5 Delivered).
Use `/approve` for Processing and `/cancel` for Cancelled; `/status` rejects both.
**Checkout address:** send `shippingAddress` to ship somewhere else; leave it out (or empty) to use `profile.address`. If neither exists: `400 { "message": "No shipping address was given and none is saved on your profile." }`.
Typical `400` messages: `"Your cart is empty."`, `"An order cannot move from X to Y."`. Typical `409`: `"Not enough stock. ..."`.
Typical `403`/`401` messages: `"You do not have access to this order."`, `"You can only cancel your own orders."`.

---

## 6. Payments (all routes need `Authorization: Bearer <token>`)

```ts
interface Payment {
  id: number;
  orderId: number;
  userId: string;
  amount: number;
  status: PaymentStatus;          // "Pending" | "Success" | "Failed" | "Refunded"
  transactionId: string | null;
  paidAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}
interface ConfirmPaymentRequest { orderId: number; transactionId: string; }   // orderId 1 or more; transactionId REQUIRED, max 200
interface FailPaymentRequest { orderId: number; }                              // 1 or more
```
**A payment row only exists after the customer confirms or fails it.** Before that, the order has no `paymentId` and counts as Pending.
```json
{ "id": 3, "orderId": 10, "userId": "9f1c...", "amount": 9998, "status": "Success",
  "transactionId": "TXN123", "paidAt": "2026-10-31T09:35:00Z", "createdAt": "2026-10-31T09:35:00Z", "updatedAt": null }
```

| Method | URL | Role | Request | Response |
|---|---|---|---|---|
| GET | `/api/Payment/get-payment/{id}` | owner or Admin | none | `200` `Payment`, `404` |
| GET | `/api/Payment/get-payment-by-order/{orderId}` | owner or Admin | none | `200` `Payment`, `404` |
| GET | `/api/Payment/get-my-payments` | any logged-in user | none | `200` `Payment[]` (the caller's own payments) |
| GET | `/api/Payment/get-user-payments/{userId}` | Admin | none | `200` `Payment[]` (admin screens only) |
| GET | `/api/Payment/get-all-payments` | Admin | none | `200` `Payment[]` |
| POST | `/api/Payment/confirm-payment` | Customer (own order) | `ConfirmPaymentRequest` | `200` `Payment` (status `Success`; order becomes `Paid`) |
| POST | `/api/Payment/fail-payment` | Customer (own order) | `FailPaymentRequest` | `200` `Payment` (status `Failed`; order `Cancelled`; items go back into the cart) |
| POST | `/api/Payment/refund-payment/{id}` | Admin | none | `200` `Payment` |

Request example (confirm): `{ "orderId": 10, "transactionId": "TXN123" }`
Request example (fail): `{ "orderId": 10 }`

**Confirm:** the server first checks that every product is still in stock and then **takes the quantities off the product stock** (`stockQuantity` goes down). If one is short it returns `409 { "message": "Not enough stock. ..." }`, nothing is taken or recorded and the order stays `Pending`, so the user can call fail. Repeating a confirm that already succeeded returns the same payment (no duplicate).
**Fail:** the order becomes `Cancelled` and each item is added back to the user's cart (an item that is gone or out of stock is skipped). Repeating it returns the same Failed payment and does not add the items twice.
Other errors: `400` (missing `transactionId` / `orderId`), `403` (not your order), `404` (order not found), `409` (order already has a payment of a different result, or is not Pending).
Error: `403 { "message": "You can only view your own payments." }` when reading another user's payment.

---

## 7. Using it in TSX

```tsx
import { authApi, productApi, cartApi, orderApi, paymentApi, tokenStore, imageSrc } from "../lib/api";
import type { Product } from "../types/api";

// login: the token is opaque (encrypted), so take userId/email/roles from the response, not from the token
const result = await authApi.login({ email, password });   // AuthResult
tokenStore.set(result);
const isAdmin = result.roles.includes("Admin");

// list products
const products: Product[] = await productApi.getAll();

// add to cart, then checkout (address optional: omitted = profile address)
await cartApi.addItem({ productId: 1, quantity: 2 });   // no userId
const order = await orderApi.checkout();                // or checkout({ shippingAddress: "12 Street, Chennai" })

// order.orderStatus is "Pending" and there is no payment yet. Later, depending on the payment result:
const payment = await paymentApi.confirm({ orderId: order.id, transactionId: "TXN123" });  // order becomes Paid
// await paymentApi.fail({ orderId: order.id });         // order Cancelled, items back in the cart

// my orders: paymentId === null means payment is still Pending
const orders = await orderApi.myOrders();
```

Errors are thrown as `ApiException` (`.status`, `.message` = server `message`):
```tsx
try { await authApi.login({ email, password }); }
catch (e) { if (e instanceof ApiException) setError(e.message); }
```

## 8. Recommended flow
1. Login: `POST /api/Authentication/login`, store the tokens, read `userId` from the JWT.
2. Browse: `GET /api/Product/get-all-products` and `GET /api/Category/get-all-category`.
3. Cart: `POST /api/Cart/items`, then `GET /api/Cart` and `GET /api/Cart/summary`.
4. Checkout: `POST /api/Order/checkout` (address optional). The order is created `Pending` with **no payment**, and the cart is emptied.
5. Show pending orders: `GET /api/Order/my-orders`. An order with `paymentId: null` is waiting for payment.
6. Payment succeeded: `POST /api/Payment/confirm-payment { orderId, transactionId }`. The stock is checked and **reduced** (`409` if short); then the order is `Paid`.
7. Payment failed or the user gave up: `POST /api/Payment/fail-payment { orderId }`. The order is `Cancelled` and its items are back in the cart, so the user can check out again.
8. Track: `GET /api/Order/my-orders` and `GET /api/Payment/get-my-payments`.

## 9. Notes
- CORS allows `http://localhost:5173` and `http://localhost:3000` (change in `gateway/Program.cs`).
- There is no real payment provider: confirm trusts the `transactionId` the client sends. Fine for a simulated payment; a real provider needs server-side verification.
- Confirm takes the stock, so product `stockQuantity` drops at payment. Staff approval of a `Paid` order does not take it again, and cancelling a `Paid` or `Processing` order gives it back. A `Delivered` order keeps the stock deducted (it is sold).
- Response shapes were taken from the backend code and have not been checked against live responses.
