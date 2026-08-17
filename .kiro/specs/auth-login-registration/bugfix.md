# Bugfix Requirements Document

## Introduction

The login and registration features are broken end-to-end due to a series of contract mismatches between the Angular frontend and the .NET Auth API. When a user attempts to register or log in, the NgRx effects call `AuthService` methods that send incorrectly shaped request bodies and expect a response shape that the backend never produces. As a result, every login and registration attempt fails silently or with an unhandled error, the NgRx store is never populated with user/token data, and protected routes (cart, checkout, account) remain inaccessible. Additionally, logout never revokes the refresh token on the server because the token is not included in the request body.

The bugs span four distinct contract points:

1. **Response shape** — the backend returns a flat `AuthResponseDto`; the frontend expects a nested `{ user, tokens }` wrapper that does not exist.
2. **Register request body** — the backend requires `confirmPassword`; the frontend never sends it.
3. **Logout request body** — the backend requires `{ refreshToken }`; the frontend sends `{}`.
4. **User model roles** — the backend `UserDto` omits `roles`; the frontend `selectIsAdmin` selector reads `user.roles`, causing the admin guard to always deny access.

---

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a user submits valid login credentials THEN the system sends `POST /api/auth/login` and receives `{ accessToken, refreshToken, accessTokenExpiresAt, user: { id, email, firstName, lastName } }` but the frontend `AuthService.login()` maps the raw response directly to `{ user: User; tokens: AuthTokens }`, so `user` and `tokens` are both `undefined` and `AuthActions.loginSuccess` is dispatched with undefined values.

1.2 WHEN a user submits a valid registration form THEN the system sends `POST /api/auth/register` with body `{ firstName, lastName, email, password }` (missing `confirmPassword`), causing the backend FluentValidation to return HTTP 400 and the registration to fail.

1.3 WHEN a logged-in user clicks logout THEN the system sends `POST /api/auth/logout` with an empty body `{}`, so the backend cannot find the refresh token, the token is never revoked in the database, and the server returns HTTP 400 or silently ignores the request.

1.4 WHEN the NgRx store is populated after a successful login THEN `user.roles` is `undefined` because the backend `UserDto` does not include a `roles` field, causing `selectIsAdmin` to throw a runtime error and the admin guard to always redirect to `/`.

1.5 WHEN the frontend `AuthTokens` model reads `expiresIn` (a number) THEN the backend sends `accessTokenExpiresAt` (an ISO 8601 datetime string), so the expiry field is always `undefined` in the store.

### Expected Behavior (Correct)

2.1 WHEN a user submits valid login credentials THEN the system SHALL map the flat `AuthResponseDto` response into the `{ user: User; tokens: AuthTokens }` shape expected by the NgRx effects, populating the store with a valid `User` (including `roles`) and `AuthTokens` (including `accessToken`, `refreshToken`, and `expiresIn`), and redirect the user to `/`.

2.2 WHEN a user submits a valid registration form THEN the system SHALL send `POST /api/auth/register` with body `{ firstName, lastName, email, password, confirmPassword }` so the backend validator accepts the request and creates the account.

2.3 WHEN a logged-in user clicks logout THEN the system SHALL read the current `refreshToken` from the NgRx store and send `POST /api/auth/logout` with body `{ refreshToken }` so the backend revokes the token in the database.

2.4 WHEN the backend returns a successful auth response THEN the system SHALL include the user's roles array in the mapped `User` object by decoding the JWT access token claims (specifically `ClaimTypes.Role`) so that `selectIsAdmin` correctly identifies admin users.

2.5 WHEN the frontend maps `accessTokenExpiresAt` from the backend response THEN the system SHALL convert the ISO 8601 datetime string to a Unix timestamp number and store it as `expiresIn` in `AuthTokens` so the token expiry is correctly tracked.

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a user submits an invalid email or missing password on the login form THEN the system SHALL CONTINUE TO show inline validation errors without dispatching any NgRx action.

3.2 WHEN a user submits a registration form with mismatched passwords THEN the system SHALL CONTINUE TO show the "Passwords do not match" error and block submission.

3.3 WHEN a user submits a registration form with a password that does not meet complexity requirements THEN the system SHALL CONTINUE TO show the password validation error and block submission.

3.4 WHEN the backend returns HTTP 401 for an expired access token THEN the system SHALL CONTINUE TO dispatch `AuthActions.refreshToken()` via the error interceptor to attempt a token refresh.

3.5 WHEN the backend returns HTTP 401 and no refresh token exists in the store THEN the system SHALL CONTINUE TO dispatch `AuthActions.logoutSuccess()` and redirect to `/auth/login`.

3.6 WHEN a non-admin authenticated user navigates to `/admin` THEN the system SHALL CONTINUE TO redirect them to `/` via the admin guard.

3.7 WHEN an unauthenticated user navigates to `/cart`, `/checkout`, or `/account` THEN the system SHALL CONTINUE TO redirect them to `/auth/login` via the auth guard.

3.8 WHEN the auth interceptor runs on any HTTP request THEN the system SHALL CONTINUE TO attach the `Authorization: Bearer <token>` header when an access token exists in the store.
