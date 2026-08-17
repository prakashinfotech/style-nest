# Auth Login & Registration Bugfix Design

## Overview

Five contract mismatches between the Angular 21 frontend and the .NET 10 Auth API cause every login, registration, and logout flow to fail. The bugs are all located in `frontend/src/app/core/services/auth.service.ts` and the NgRx layer that consumes it. No backend changes are required — the backend contract is correct and the frontend must be brought into alignment.

The fix strategy is purely additive mapping on the frontend:

1. **Response shape** — `AuthService.login()` and `AuthService.register()` must map the flat `AuthResponseDto` into the `{ user: User; tokens: AuthTokens }` shape the NgRx effects destructure.
2. **Missing `confirmPassword`** — `AuthService.register()` must accept and forward `confirmPassword` in the request body.
3. **Logout body** — `AuthService.logout()` must accept the current `refreshToken` and send `{ refreshToken }` in the body.
4. **Roles missing from `UserDto`** — the response mapping must decode the JWT access token claims to extract roles and populate `User.roles`.
5. **`expiresIn` type** — the response mapping must convert `accessTokenExpiresAt` (ISO 8601 string) to a Unix timestamp number for `AuthTokens.expiresIn`.

The NgRx `logoutEffect` must also be updated to read `refreshToken` from the store and pass it to `AuthService.logout()`.

---

## Glossary

- **Bug_Condition (C)**: The set of runtime inputs that trigger a defect — in this spec, any call to `login()`, `register()`, or `logout()` on the current unfixed `AuthService`.
- **Property (P)**: The desired post-condition that must hold after the fix — the returned/dispatched data must be correctly shaped and populated.
- **Preservation**: Existing behaviors that must remain unchanged — form validation, interceptor logic, guard redirects, and token refresh flow.
- **`AuthService`**: The Angular service in `frontend/src/app/core/services/auth.service.ts` that wraps all HTTP calls to the Auth API.
- **`AuthResponseDto`**: The flat backend response record `{ accessToken, refreshToken, accessTokenExpiresAt: DateTime, user: UserDto }` returned by `/api/auth/login` and `/api/auth/register`.
- **`UserDto`**: The backend record `{ id: Guid, email, firstName, lastName }` — notably **without** a `roles` field.
- **`LoginResponse` / `RegisterResponse`**: The frontend interface `{ user: User; tokens: AuthTokens }` that NgRx effects destructure via `map(({ user, tokens }) => ...)`.
- **`AuthTokens`**: The frontend model `{ accessToken: string; refreshToken: string; expiresIn: number }` where `expiresIn` is a Unix timestamp in milliseconds.
- **`User`**: The frontend model `{ id, email, firstName, lastName, phoneNumber, roles: string[] }`.
- **`selectIsAdmin`**: The NgRx selector `u?.roles.includes('Admin') ?? false` — throws a runtime error when `roles` is `undefined`.
- **JWT claim extraction**: Parsing the base64url-encoded payload of the access token to read `ClaimTypes.Role` claims without a library dependency.

---

## Bug Details

### Bug Condition

All five bugs share a single trigger: any call to `AuthService.login()`, `AuthService.register()`, or `AuthService.logout()` on the current unfixed code. The bugs manifest because the service methods either send the wrong request shape or return the wrong response shape.

**Formal Specification:**

```
FUNCTION isBugCondition(call)
  INPUT: call — one of { login(email, password), register(...), logout() }
  OUTPUT: boolean

  IF call.method == 'login' OR call.method == 'register'
    RETURN TRUE   // response is never mapped; user and tokens are always undefined
  END IF

  IF call.method == 'register'
    RETURN TRUE   // request body missing confirmPassword → HTTP 400
  END IF

  IF call.method == 'logout'
    RETURN TRUE   // request body is {} → refresh token never revoked
  END IF

  RETURN FALSE
END FUNCTION
```

### Examples

**Bug 1 — Response shape mismatch:**
- Input: `authService.login('user@example.com', 'Password1')` with backend returning `{ accessToken: 'eyJ...', refreshToken: 'abc', accessTokenExpiresAt: '2025-01-01T00:00:00Z', user: { id: '...', email: '...', firstName: 'Jane', lastName: 'Doe' } }`
- Actual: `map(({ user, tokens }) => ...)` destructures `undefined` for both — `AuthActions.loginSuccess({ user: undefined, tokens: undefined })` is dispatched.
- Expected: `{ user: { id: '...', email: '...', firstName: 'Jane', lastName: 'Doe', phoneNumber: null, roles: ['Customer'] }, tokens: { accessToken: 'eyJ...', refreshToken: 'abc', expiresIn: 1735689600000 } }`

**Bug 2 — Missing `confirmPassword`:**
- Input: `authService.register('Jane', 'Doe', 'jane@example.com', 'Password1')` dispatched from `RegisterComponent.submit()`
- Actual: HTTP body sent is `{ firstName, lastName, email, password }` — backend FluentValidation returns HTTP 400 `"Passwords do not match."`
- Expected: HTTP body must include `confirmPassword: 'Password1'`

**Bug 3 — Logout sends empty body:**
- Input: `authService.logout()` called from `logoutEffect` with `refreshToken = 'abc123'` in the store
- Actual: HTTP body sent is `{}` — backend `LogoutAsync` receives an empty `RefreshRequestDto`, token is never revoked
- Expected: HTTP body must be `{ refreshToken: 'abc123' }`

**Bug 4 — `user.roles` missing:**
- Input: Successful login response mapped to `User` object
- Actual: `User.roles` is `undefined` — `selectIsAdmin` evaluates `undefined.includes('Admin')` → runtime error; admin guard always redirects to `/`
- Expected: `User.roles` is `['Customer']` (or `['Admin']` for admin users), decoded from JWT claims

**Bug 5 — `expiresIn` type mismatch:**
- Input: Backend sends `accessTokenExpiresAt: '2025-01-01T00:00:00Z'`
- Actual: `AuthTokens.expiresIn` is `undefined` because the field name doesn't match and no conversion is applied
- Expected: `AuthTokens.expiresIn = new Date('2025-01-01T00:00:00Z').getTime()` → `1735689600000`

---

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**
- Login form inline validation (invalid email, missing password) must continue to show errors without dispatching NgRx actions — `LoginComponent` is not modified.
- Register form validation (password mismatch, complexity rules) must continue to block submission — `RegisterComponent`'s `passwordsMatch` validator and `Validators.pattern` are not modified.
- The auth interceptor must continue to attach `Authorization: Bearer <token>` on every outgoing request when an access token exists in the store.
- The error interceptor must continue to dispatch `AuthActions.refreshToken()` on HTTP 401 when a refresh token exists, and `AuthActions.logoutSuccess()` when it does not.
- The admin guard must continue to redirect non-admin users to `/` and unauthenticated users to `/auth/login`.
- The auth guard must continue to redirect unauthenticated users to `/auth/login` for protected routes.
- The `AuthService.refreshToken()` method signature and behavior must remain unchanged.

**Scope:**
All code paths that do NOT go through `AuthService.login()`, `AuthService.register()`, or `AuthService.logout()` are completely unaffected by this fix. The fix is confined to `auth.service.ts` and the `logoutEffect` in `auth.effects.ts`.

---

## Hypothesized Root Cause

1. **Missing response mapping in `AuthService`**: `login()` and `register()` are typed as returning `Observable<LoginResponse>` but the actual HTTP response is `AuthResponseDto` (flat shape). TypeScript's structural typing allows the cast at compile time, but at runtime the destructuring `{ user, tokens }` in the NgRx effects finds no matching keys. The fix requires a `map()` operator in the service that transforms the flat response into the nested shape.

2. **`register()` does not accept or forward `confirmPassword`**: The method signature is `register(firstName, lastName, email, password)` — `confirmPassword` is never a parameter. The `RegisterComponent` collects it in the form but `RegisterComponent.submit()` only dispatches `{ firstName, lastName, email, password }`. The `AuthActions.register` action also lacks a `confirmPassword` prop. The fix requires threading `confirmPassword` from the component through the action to the service.

3. **`logout()` ignores the stored refresh token**: The method takes no parameters and sends `{}`. The `logoutEffect` calls `authService.logout()` with no arguments. The fix requires `logoutEffect` to first select `refreshToken` from the store and pass it to `authService.logout(refreshToken)`.

4. **`UserDto` has no `roles` field and the mapping doesn't decode the JWT**: The backend `IssueTokensAsync` encodes roles into the JWT access token claims but does not include them in `UserDto`. The frontend mapping must decode the JWT payload (base64url decode the second segment) and extract the role claims to populate `User.roles`.

5. **Field name mismatch and no type conversion for token expiry**: The backend field is `accessTokenExpiresAt` (a `DateTime` serialized as ISO 8601); the frontend model field is `expiresIn` (a `number`). The mapping must rename the field and convert `new Date(accessTokenExpiresAt).getTime()`.

---

## Correctness Properties

Property 1: Bug Condition — Auth Response Correctly Mapped to Store Shape

_For any_ successful call to `AuthService.login()` or `AuthService.register()` where the backend returns a valid `AuthResponseDto`, the fixed service SHALL return an `Observable<{ user: User; tokens: AuthTokens }>` where:
- `user.id`, `user.email`, `user.firstName`, `user.lastName` match the `UserDto` fields
- `user.phoneNumber` is `null`
- `user.roles` is a non-empty `string[]` decoded from the JWT access token claims
- `tokens.accessToken` and `tokens.refreshToken` match the `AuthResponseDto` fields
- `tokens.expiresIn` is a finite `number` equal to `new Date(accessTokenExpiresAt).getTime()`

**Validates: Requirements 2.1, 2.4, 2.5**

Property 2: Bug Condition — Register Request Includes `confirmPassword`

_For any_ call to `AuthService.register(firstName, lastName, email, password, confirmPassword)`, the fixed service SHALL send an HTTP POST body that includes `confirmPassword` equal to the `password` argument, so the backend FluentValidation accepts the request.

**Validates: Requirements 2.2**

Property 3: Bug Condition — Logout Sends Refresh Token in Body

_For any_ call to `AuthService.logout(refreshToken)` where `refreshToken` is a non-empty string, the fixed service SHALL send an HTTP POST body of `{ refreshToken }` so the backend can revoke the token in the database.

**Validates: Requirements 2.3**

Property 4: Preservation — Non-Auth-Service Code Paths Unchanged

_For any_ input that does NOT go through `AuthService.login()`, `AuthService.register()`, or `AuthService.logout()` (i.e., form validation, interceptors, guards, `refreshToken()`), the fixed code SHALL produce exactly the same behavior as the original code.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8**

---

## Fix Implementation

### Changes Required

**File 1**: `frontend/src/app/core/services/auth.service.ts`

**Specific Changes:**

1. **Add `BackendAuthResponse` interface** — define the flat shape matching `AuthResponseDto`:
   ```typescript
   interface BackendAuthResponse {
     accessToken: string;
     refreshToken: string;
     accessTokenExpiresAt: string;
     user: { id: string; email: string; firstName: string; lastName: string };
   }
   ```

2. **Add `mapAuthResponse()` helper** — converts `BackendAuthResponse` to `{ user: User; tokens: AuthTokens }`:
   - Decode JWT payload: `JSON.parse(atob(accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))`
   - Extract roles from claim key `'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'` (ASP.NET Core `ClaimTypes.Role` serialization) — normalize to `string[]`
   - Map `user`: spread `UserDto` fields, set `phoneNumber: null`, set `roles`
   - Map `tokens`: copy `accessToken` and `refreshToken`, set `expiresIn: new Date(accessTokenExpiresAt).getTime()`

3. **Update `login()` return type and add mapping**:
   ```typescript
   login(email: string, password: string): Observable<LoginResponse> {
     return this.http.post<BackendAuthResponse>(`${this.base}/auth/login`, { email, password })
       .pipe(map(mapAuthResponse));
   }
   ```

4. **Update `register()` signature, request body, and add mapping**:
   ```typescript
   register(firstName: string, lastName: string, email: string, password: string, confirmPassword: string): Observable<RegisterResponse> {
     return this.http.post<BackendAuthResponse>(`${this.base}/auth/register`, { firstName, lastName, email, password, confirmPassword })
       .pipe(map(mapAuthResponse));
   }
   ```

5. **Update `logout()` to accept and send `refreshToken`**:
   ```typescript
   logout(refreshToken: string): Observable<void> {
     return this.http.post<void>(`${this.base}/auth/logout`, { refreshToken });
   }
   ```

---

**File 2**: `frontend/src/app/store/auth/auth.actions.ts`

**Specific Changes:**

6. **Add `confirmPassword` to the `Register` action props**:
   ```typescript
   'Register': props<{ firstName: string; lastName: string; email: string; password: string; confirmPassword: string }>(),
   ```

---

**File 3**: `frontend/src/app/store/auth/auth.effects.ts`

**Specific Changes:**

7. **Update `registerEffect`** to pass `confirmPassword` from the action to the service:
   ```typescript
   exhaustMap(({ firstName, lastName, email, password, confirmPassword }) =>
     authService.register(firstName, lastName, email, password, confirmPassword).pipe(...)
   ```

8. **Update `logoutEffect`** to select `refreshToken` from the store before calling `logout()`:
   ```typescript
   export const logoutEffect = createEffect(
     (actions$ = inject(Actions), authService = inject(AuthService), store = inject(Store)) =>
       actions$.pipe(
         ofType(AuthActions.logout),
         exhaustMap(() =>
           store.select(selectRefreshToken).pipe(
             take(1),
             exhaustMap((refreshToken) =>
               authService.logout(refreshToken ?? '').pipe(
                 map(() => AuthActions.logoutSuccess()),
                 catchError(() => of(AuthActions.logoutSuccess())),
               )
             ),
           )
         ),
       ),
     { functional: true },
   );
   ```

---

**File 4**: `frontend/src/app/features/auth/register.component.ts`

**Specific Changes:**

9. **Update `submit()` to include `confirmPassword` in the dispatched action**:
   ```typescript
   const { firstName, lastName, email, password, confirmPassword } = this.form.getRawValue();
   this.store.dispatch(AuthActions.register({ firstName, lastName, email, password, confirmPassword }));
   ```

---

## Testing Strategy

### Validation Approach

The testing strategy follows a two-phase approach: first, surface counterexamples that demonstrate each bug on the unfixed code, then verify the fix works correctly and preserves existing behavior. All tests target `AuthService` and the NgRx effects in isolation using Angular's `TestBed` with `HttpClientTestingModule` and NgRx testing utilities.

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate each bug BEFORE implementing the fix. Confirm or refute the root cause analysis.

**Test Plan**: Write unit tests that call the unfixed `AuthService` methods with a mocked HTTP backend returning a valid `AuthResponseDto`, then assert the expected mapped shape. Run these tests on the UNFIXED code to observe failures.

**Test Cases**:

1. **Login response mapping test** (will fail on unfixed code): Call `authService.login('a@b.com', 'pw')`, mock HTTP to return a valid `AuthResponseDto`, assert that the emitted value has `result.user.email === 'a@b.com'` and `result.tokens.accessToken` is defined.

2. **Register missing `confirmPassword` test** (will fail on unfixed code): Call `authService.register(...)`, capture the outgoing HTTP request body via `HttpTestingController`, assert that `body.confirmPassword` is defined.

3. **Logout empty body test** (will fail on unfixed code): Call `authService.logout()`, capture the outgoing HTTP request body, assert that `body.refreshToken` is a non-empty string.

4. **Roles extraction test** (will fail on unfixed code): Call `authService.login(...)`, mock HTTP to return a `BackendAuthResponse` with a JWT containing a role claim, assert that `result.user.roles` is `['Customer']`.

5. **`expiresIn` conversion test** (will fail on unfixed code): Call `authService.login(...)`, mock HTTP to return `accessTokenExpiresAt: '2025-01-01T00:00:00Z'`, assert that `result.tokens.expiresIn === 1735689600000`.

**Expected Counterexamples**:
- `result.user` is `undefined` and `result.tokens` is `undefined` (Bug 1)
- `body.confirmPassword` is `undefined` (Bug 2)
- `body` is `{}` with no `refreshToken` key (Bug 3)
- `result.user.roles` is `undefined` (Bug 4)
- `result.tokens.expiresIn` is `undefined` (Bug 5)

### Fix Checking

**Goal**: Verify that for all inputs where the bug condition holds, the fixed service produces the expected behavior.

**Pseudocode:**
```
FOR ALL call WHERE isBugCondition(call) DO
  result := fixedAuthService[call.method](call.args)
  ASSERT expectedBehavior(result)
END FOR
```

### Preservation Checking

**Goal**: Verify that for all inputs where the bug condition does NOT hold, the fixed code produces the same result as the original code.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalCode(input) = fixedCode(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation checking because:
- It generates many test cases automatically across the input domain
- It catches edge cases that manual unit tests might miss
- It provides strong guarantees that behavior is unchanged for all non-buggy inputs

**Test Plan**: Observe behavior on UNFIXED code first for interceptors, guards, and form validation, then write property-based tests capturing that behavior.

**Test Cases**:

1. **Auth interceptor preservation**: Verify that for any HTTP request with a non-null access token in the store, the interceptor attaches `Authorization: Bearer <token>` — unchanged by the fix.

2. **Error interceptor 401 preservation**: Verify that for any HTTP 401 response on a non-refresh endpoint, the interceptor dispatches `refreshToken` (if token exists) or `logoutSuccess` (if not) — unchanged by the fix.

3. **Login form validation preservation**: Verify that submitting the login form with an invalid email or missing password shows inline errors and does not dispatch `AuthActions.login` — `LoginComponent` is not modified.

4. **Register form validation preservation**: Verify that submitting the register form with mismatched passwords shows the mismatch error and blocks submission — `RegisterComponent`'s validator is not modified.

5. **`AuthService.refreshToken()` preservation**: Verify that `refreshToken()` continues to send `{ refreshToken }` and return `Observable<AuthTokens>` — this method is not modified.

### Unit Tests

- Test `mapAuthResponse()` helper in isolation with various `BackendAuthResponse` inputs including single-role, multi-role, and no-role JWT payloads
- Test `AuthService.login()` and `register()` with `HttpTestingController` to assert correct request bodies and mapped response shapes
- Test `AuthService.logout()` to assert `{ refreshToken }` is sent in the body
- Test `logoutEffect` with a mock store containing a refresh token, asserting the token is passed to `authService.logout()`
- Test `registerEffect` to assert `confirmPassword` is forwarded from the action to the service

### Property-Based Tests

- Generate random valid `BackendAuthResponse` objects (varying JWT payloads with 0–5 roles) and verify `mapAuthResponse()` always produces a `User` with a `string[]` roles array and `AuthTokens` with a finite `number` for `expiresIn`
- Generate random `accessTokenExpiresAt` ISO 8601 strings and verify `new Date(s).getTime()` is always a finite number stored as `expiresIn`
- Generate random non-auth HTTP requests and verify the auth interceptor behavior is unchanged (attaches header iff token exists)

### Integration Tests

- Test full login flow: dispatch `AuthActions.login`, mock HTTP, assert store is populated with correct `user` (including `roles`) and `accessToken`/`refreshToken`
- Test full register flow: dispatch `AuthActions.register` with `confirmPassword`, mock HTTP, assert store is populated and redirect to `/` occurs
- Test full logout flow: dispatch `AuthActions.logout` with a refresh token in the store, assert HTTP body contains `{ refreshToken }` and store is cleared
- Test `selectIsAdmin` after login as admin: assert selector returns `true` when roles include `'Admin'`
- Test `selectIsAdmin` after login as customer: assert selector returns `false` when roles are `['Customer']`
