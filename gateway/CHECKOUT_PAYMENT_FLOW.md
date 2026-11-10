# Checkout and Payment Flow (Frontend Hand-off)

**Base URL:** `https://localhost:7000` (gateway). All requests below need `Authorization: Bearer <token>` and the user must have the **Customer** role.
JSON requests also need `Content-Type: application/json`. Field names are exact and case-sensitive (camelCase).
The frontend never sends a `userId`; the server reads it from the token.

Ready-made types and client: `src/types/api.ts` and `src/lib/api.ts`.

---

## The flow at a glance

```
Cart has items
   |
   v
1. POST /api/Order/checkout          -> Order created, orderStatus "Pending", NO payment yet, cart emptied
   |
   v
2. GET  /api/Order/my-orders         -> paymentId === null  => payment still pending (show "Pay now")
   |
   +--- user pays OK ------------->  3a. POST /api/Payment/confirm-payment { orderId, transactionId }
   |                                      -> stock checked and REDUCED
   |                                      -> OK:  payment "Success", order "Paid"
   |                                      -> 409: out of stock, order stays "Pending"
   |
   +--- payment fails / cancelled ->  3b. POST /api/Payment/fail-payment { orderId }
                                          -> payment "Failed", order "Cancelled", items put back in the cart
```

---

## Shared types

```ts
type OrderStatusName = "Pending" | "Paid" | "Processing" | "Shipped" | "Delivered" | "Cancelled";
type PaymentStatus   = "Pending" | "Success" | "Failed" | "Refunded";

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
  orderStatus: OrderStatusName;
  shippingAddress: string;
  totalAmount: number;
  createdAt: string;                      // ISO date
  updatedAt: string | null;
  paymentId: number | null;               // only filled by GET my-orders; null = no payment yet
  paymentStatus: PaymentStatus | null;    // only filled by GET my-orders; "Pending" when paymentId is null
  items: OrderItem[];
}

interface Payment {
  id: number;
  orderId: number;
  userId: string;
  amount: number;
  status: PaymentStatus;
  transactionId: string | null;
  paidAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

type ApiError = { message: string };      // body of 401 / 403 / 404 / 409 / 500 errors
```

---

## Step 1: Checkout

`POST /api/Order/checkout`

**Request body** (the whole body is optional; send `{}` or nothing to use the profile address):
```ts
interface CheckoutRequest {
  shippingAddress?: string | null;   // optional, max 1000 characters
}
```
```json
{ "shippingAddress": "12 Street, Chennai" }
```
or
```json
{}
```
- If `shippingAddress` is sent, it is used.
- If it is missing or empty, the address saved on the user's profile (`GET /api/Authentication/me` -> `address`) is used.

**Response `201`** (`Order`):
```json
{
  "id": 10,
  "userId": "9f1c2a...",
  "orderStatus": "Pending",
  "shippingAddress": "12 Street, Chennai",
  "totalAmount": 9998,
  "createdAt": "2026-11-01T09:30:00Z",
  "updatedAt": null,
  "paymentId": null,
  "paymentStatus": null,
  "items": [
    { "id": 1, "orderId": 10, "productId": 1, "productName": "Nike Air", "quantity": 2, "unitPrice": 4999, "subtotal": 9998 }
  ]
}
```
Keep `id` (this is the `orderId` for the payment steps). The cart is now empty. **No payment exists yet.**

**Errors**
| Status | Body | When |
|---|---|---|
| 400 | `{ "message": "Your cart is empty." }` | nothing in the cart |
| 400 | `{ "message": "No shipping address was given and none is saved on your profile." }` | no address sent and none on the profile |
| 409 | `{ "message": "Not enough stock. Nike Air: requested 5, only 2 in stock." }` | a product is short |
| 401 | (empty body) | missing, invalid or expired token: refresh the token or log in again |
| 403 | (empty body) | logged in but not a Customer |
| 502 | `{ "message": "A dependent service (cart, product, payment or auth) is unavailable. Please try again later." }` | another service is down |

---

## Step 2: My orders (decide what to show)

`GET /api/Order/my-orders` (no body)

**Response `200`** (`Order[]`, newest first):
```json
[
  {
    "id": 10, "userId": "9f1c2a...", "orderStatus": "Pending", "shippingAddress": "12 Street, Chennai",
    "totalAmount": 9998, "createdAt": "2026-11-01T09:30:00Z", "updatedAt": null,
    "paymentId": null, "paymentStatus": "Pending",
    "items": [ { "id": 1, "orderId": 10, "productId": 1, "productName": "Nike Air", "quantity": 2, "unitPrice": 4999, "subtotal": 9998 } ]
  },
  {
    "id": 9, "userId": "9f1c2a...", "orderStatus": "Paid", "shippingAddress": "12 Street, Chennai",
    "totalAmount": 2500, "createdAt": "2026-10-31T10:00:00Z", "updatedAt": "2026-10-31T10:05:00Z",
    "paymentId": 3, "paymentStatus": "Success",
    "items": [ { "id": 7, "orderId": 9, "productId": 4, "productName": "Cap", "quantity": 1, "unitPrice": 2500, "subtotal": 2500 } ]
  }
]
```
How to read it:
| `paymentId` | `paymentStatus` | `orderStatus` | Show |
|---|---|---|---|
| `null` | `"Pending"` | `"Pending"` | **Pay now** (calls confirm or fail) |
| number | `"Success"` | `"Paid"` and later | Paid |
| number | `"Failed"` | `"Cancelled"` | Payment failed / cancelled |
| number | `"Refunded"` | any | Refunded |

If the payment service is temporarily down, `paymentId` and `paymentStatus` can both be `null`; treat that as "unknown, try again".

---

## Step 3a: Payment succeeded -> Confirm

`POST /api/Payment/confirm-payment`

**Request body**
```ts
interface ConfirmPaymentRequest {
  orderId: number;         // required, 1 or more (the order id from checkout)
  transactionId: string;   // REQUIRED, max 200 (the id from the payment provider)
}
```
```json
{ "orderId": 10, "transactionId": "TXN123456" }
```

**Response `200`** (`Payment`):
```json
{
  "id": 3,
  "orderId": 10,
  "userId": "9f1c2a...",
  "amount": 9998,
  "status": "Success",
  "transactionId": "TXN123456",
  "paidAt": "2026-11-01T09:35:00Z",
  "createdAt": "2026-11-01T09:35:00Z",
  "updatedAt": null
}
```
What happens on the server: it first checks that every product in the order is still in stock, then **reduces each product's `stockQuantity`** by the ordered amount, marks the order `Paid` and records this payment. Calling it again for the same order returns the same payment (no duplicate).

**Errors**
| Status | Body | When |
|---|---|---|
| 400 | validation problem (see below) | `orderId` missing/0 or `transactionId` missing/empty/over 200 chars |
| 403 | `{ "message": "You do not have access to this order." }` | order belongs to someone else |
| 404 | `{ "message": "Order not found." }` | no such order |
| 409 | `{ "message": "Not enough stock. Nike Air: requested 5, only 2 in stock." }` | a product ran out. **Nothing is recorded and the order stays `Pending`.** Tell the user, then let them call fail (or fix the order). |
| 409 | `{ "message": "Cannot confirm a payment for a cancelled order." }` | order already cancelled |
| 409 | `{ "message": "This order already has a Failed payment." }` | fail was already recorded |

---

## Step 3b: Payment failed -> Fail

`POST /api/Payment/fail-payment`

**Request body**
```ts
interface FailPaymentRequest {
  orderId: number;   // required, 1 or more
}
```
```json
{ "orderId": 10 }
```

**Response `200`** (`Payment`):
```json
{
  "id": 4,
  "orderId": 10,
  "userId": "9f1c2a...",
  "amount": 9998,
  "status": "Failed",
  "transactionId": null,
  "paidAt": null,
  "createdAt": "2026-11-01T09:36:00Z",
  "updatedAt": null
}
```
What happens on the server: the order becomes `Cancelled` and each order item is added back to the user's cart. After this, reload the cart with `GET /api/Cart` and let the user check out again. Calling it again returns the same payment and does not add the items twice.
An item that was deleted, is out of stock, or would push a cart line over 100 is skipped, so the cart can have fewer items than the order did.

**Errors**
| Status | Body | When |
|---|---|---|
| 400 | validation problem | `orderId` missing/0 |
| 403 | `{ "message": "You do not have access to this order." }` | order belongs to someone else |
| 404 | `{ "message": "Order not found." }` | no such order |
| 409 | `{ "message": "This order already has a Success payment, so it cannot be failed." }` | the order was already paid |

---

## Validation error body (400 from missing/invalid fields)

When a request body breaks a rule (for example `transactionId` is empty), the response is the standard ASP.NET format, not `{ message }`:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "TransactionId": ["The TransactionId field is required."] },
  "traceId": "00-..."
}
```
Show `Object.values(errors).flat()[0]` to the user. Handle both shapes:
```ts
const msg = body.message ?? Object.values(body.errors ?? {}).flat()[0] ?? "Something went wrong";
```

---

## TSX example (with the ready-made client)

```tsx
import { orderApi, paymentApi, cartApi, ApiException } from "../lib/api";

// 1. checkout (address optional)
const order = await orderApi.checkout();                       // or { shippingAddress: "..." }

// 2. list orders, find the ones waiting for payment
const orders = await orderApi.myOrders();
const unpaid = orders.filter(o => o.paymentId === null && o.orderStatus === "Pending");

// 3a. payment succeeded
try {
  const payment = await paymentApi.confirm({ orderId: order.id, transactionId });
} catch (e) {
  if (e instanceof ApiException && e.status === 409) {
    // out of stock: e.message says which product; offer "cancel and return items to cart"
  }
}

// 3b. payment failed or user cancelled
await paymentApi.fail({ orderId: order.id });
const cart = await cartApi.get();                               // items are back
```

## Notes
- There is no real payment provider yet: confirm accepts whatever `transactionId` is sent.
- Confirm reduces the product stock. If the order is later cancelled (while `Paid` or `Processing`) the stock goes back up; approving a `Paid` order does not reduce it a second time.
- Response shapes come from the backend code and have not been checked against live responses.
