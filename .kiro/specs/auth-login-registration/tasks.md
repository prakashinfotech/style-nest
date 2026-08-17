# Implementation Plan

- [ ] 1. Write bug condition exploration test
  - **Property 1: Bug Condition** - Auth Service Contract Mismatches
  - **CRITICAL**: This test MUST FAIL on unfixed code — failure confirms the bugs exist
  - **DO NOT attempt to fix the test or the code when it fails**
  - **NOTE**: This test encodes the expected behavior — it will validate the fix when it passes after implementation
  - **GOAL**: Surface counterexamples that demonstrate all 5 bugs before implementing any fix
  - **Scoped PBT Approach**: Scope the property to concrete failing cases for each bug to ensure reproducibility
  - Create `frontend/src/app/core/services/auth.service.spec.ts` with Angular `TestBed` + `HttpClientTestingModule`
  - Use `fast-check` to generate random valid `BackendAuthResponse` objects (varying JWT payloads with 0–5 roles)
  - **Bug 1 — Response shape**: Call `authService.login('a@b.com', 'pw')`, mock HTTP to return a valid `AuthResponseDto`, assert `result.user.email === 'a@b.com'` and `result.tokens.accessToken` is defined — **EXPECTED FAILURE**: `result.user` is `undefined`
  - **Bug 2 — Missing `confirmPassword`**: Call `authService.register(...)`, capture outgoing HTTP request body via `HttpTestingController`, assert `body.confirmPassword` is defined — **EXPECTED FAILURE**: `body.confirmPassword` is `undefined`
  - **Bug 3 — Logout empty body**: Call `authService.logout()` (current signature takes no args), capture outgoing HTTP request body, assert `body.refreshToken` is a non-empty string — **EXPECTED FAILURE**: body is `{}`
  - **Bug 4 — Roles missing**: Call `authService.login(...)`, mock HTTP to return a `BackendAuthResponse` with a JWT containing a role claim, assert `result.user.roles` is `['Customer']` — **EXPECTED FAILURE**: `result.user` is `undefined` (or `result.user.roles` is `undefined`)
  - **Bug 5 — `expiresIn` type mismatch**: Call `authService.login(...)`, mock HTTP to return `accessTokenExpiresAt: '2025-01-01T00:00:00Z'`, assert `result.tokens.expiresIn === 1735689600000` — **EXPECTED FAILURE**: `result.tokens` is `undefined`
  - Run tests on UNFIXED code
  - **EXPECTED OUTCOME**: All 5 assertions FAIL (this is correct — it proves the bugs exist)
  - Document counterexamples found: `result.user` is `undefined`, `result.tokens` is `undefined`, `body.confirmPassword` is `undefined`, logout body is `{}`, `result.user.roles` is `undefined`, `result.tokens.expiresIn` is `undefined`
  - Mark task complete when tests are written, run, and failures are documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [ ] 2. Write preservation property tests (BEFORE implementing fix)
  - **Property 2: Preservation** - Non-Auth-Service Code Paths Unchanged
  - **IMPORTANT**: Follow observation-first methodology — observe behavior on UNFIXED code first, then write tests
  - Create `frontend/src/app/core/interceptors/auth.interceptor.spec.ts` (or add to existing spec) for interceptor preservation
  - Create `frontend/src/app/core/interceptors/error.interceptor.spec.ts` (or add to existing spec) for error interceptor preservation
  - Create `frontend/src/app/features/auth/register.component.spec.ts` for form validation preservation
  - Use `fast-check` for property-based tests where applicable
  - **Auth interceptor preservation**: For any HTTP request with a non-null access token in the store, observe that the interceptor attaches `Authorization: Bearer <token>` — write property-based test asserting this holds for all non-null token strings
  - **Error interceptor 401 preservation**: For any HTTP 401 response on a non-refresh endpoint, observe that the interceptor dispatches `refreshToken` (if token exists) or `logoutSuccess` (if not) — write tests capturing this behavior
  - **Register form validation preservation**: Observe that submitting the register form with mismatched passwords shows the "Passwords do not match" error and blocks submission — `RegisterComponent`'s `passwordsMatch` validator and `Validators.pattern` are not modified
  - **Login form validation preservation**: Observe that submitting the login form with an invalid email or missing password shows inline errors and does not dispatch `AuthActions.login` — `LoginComponent` is not modified
  - **`AuthService.refreshToken()` preservation**: Observe that `refreshToken()` sends `{ refreshToken }` and returns `Observable<AuthTokens>` — this method is not modified
  - Run all preservation tests on UNFIXED code
  - **EXPECTED OUTCOME**: All preservation tests PASS (this confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [ ] 3. Fix all 5 auth contract mismatches

  - [ ] 3.1 Add `BackendAuthResponse` interface and `mapAuthResponse()` helper to `auth.service.ts`
    - In `frontend/src/app/core/services/auth.service.ts`, add `BackendAuthResponse` interface above the existing `LoginResponse`/`RegisterResponse` interfaces:
      ```typescript
      interface BackendAuthResponse {
        accessToken: string;
        refreshToken: string;
        accessTokenExpiresAt: string;
        user: { id: string; email: string; firstName: string; lastName: string };
      }
      ```
    - Add `mapAuthResponse()` helper function that:
      - Decodes JWT payload: `JSON.parse(atob(accessToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))`
      - Extracts roles from claim key `'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'` (ASP.NET Core `ClaimTypes.Role`), normalizing to `string[]` (handles both single string and array)
      - Maps `user`: spreads `UserDto` fields (`id`, `email`, `firstName`, `lastName`), sets `phoneNumber: null`, sets `roles`
      - Maps `tokens`: copies `accessToken` and `refreshToken`, sets `expiresIn: new Date(accessTokenExpiresAt).getTime()`
    - _Bug_Condition: isBugCondition(call) where call.method == 'login' OR call.method == 'register' — response is never mapped; user and tokens are always undefined_
    - _Expected_Behavior: returns `{ user: { id, email, firstName, lastName, phoneNumber: null, roles: string[] }, tokens: { accessToken, refreshToken, expiresIn: number } }`_
    - _Preservation: `AuthService.refreshToken()` method is not modified_
    - _Requirements: 2.1, 2.4, 2.5_

  - [ ] 3.2 Update `login()` and `register()` in `auth.service.ts` to use `BackendAuthResponse` and `mapAuthResponse()`
    - Change `login()` to type the HTTP call as `post<BackendAuthResponse>` and pipe through `map(mapAuthResponse)`
    - Change `register()` to accept `confirmPassword: string` as a 5th parameter, type the HTTP call as `post<BackendAuthResponse>`, include `confirmPassword` in the request body, and pipe through `map(mapAuthResponse)`
    - Change `logout()` to accept `refreshToken: string` as a parameter and send `{ refreshToken }` in the body instead of `{}`
    - _Bug_Condition: isBugCondition(call) where call.method == 'register' — request body missing confirmPassword → HTTP 400_
    - _Bug_Condition: isBugCondition(call) where call.method == 'logout' — request body is {} → refresh token never revoked_
    - _Expected_Behavior: register sends `{ firstName, lastName, email, password, confirmPassword }`; logout sends `{ refreshToken }`_
    - _Preservation: `refreshToken()` method signature and behavior unchanged_
    - _Requirements: 2.2, 2.3_

  - [ ] 3.3 Add `confirmPassword` to `AuthActions.register` in `auth.actions.ts`
    - In `frontend/src/app/store/auth/auth.actions.ts`, update the `'Register'` event props to include `confirmPassword: string`:
      ```typescript
      'Register': props<{ firstName: string; lastName: string; email: string; password: string; confirmPassword: string }>(),
      ```
    - _Bug_Condition: `RegisterComponent.submit()` dispatches `AuthActions.register` without `confirmPassword` — action prop missing_
    - _Expected_Behavior: action carries `confirmPassword` so the effect can forward it to the service_
    - _Requirements: 2.2_

  - [ ] 3.4 Update `registerEffect` in `auth.effects.ts` to forward `confirmPassword`
    - In `frontend/src/app/store/auth/auth.effects.ts`, update `registerEffect` to destructure `confirmPassword` from the action and pass it to `authService.register()`:
      ```typescript
      exhaustMap(({ firstName, lastName, email, password, confirmPassword }) =>
        authService.register(firstName, lastName, email, password, confirmPassword).pipe(...)
      ```
    - _Bug_Condition: `registerEffect` calls `authService.register(firstName, lastName, email, password)` — `confirmPassword` never forwarded_
    - _Expected_Behavior: `confirmPassword` is passed as 5th argument to `authService.register()`_
    - _Requirements: 2.2_

  - [ ] 3.5 Update `logoutEffect` in `auth.effects.ts` to select and pass `refreshToken`
    - In `frontend/src/app/store/auth/auth.effects.ts`, update `logoutEffect` to inject `Store`, select `refreshToken` via `selectRefreshToken`, and pass it to `authService.logout()`:
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
    - Add `take` to the rxjs imports and `Store` to the `@ngrx/store` imports; import `selectRefreshToken` from `auth.selectors`
    - _Bug_Condition: `logoutEffect` calls `authService.logout()` with no arguments — refresh token never sent_
    - _Expected_Behavior: `logoutEffect` selects `refreshToken` from store and passes it to `authService.logout(refreshToken)`_
    - _Preservation: `logoutRedirectEffect` and all other effects are not modified_
    - _Requirements: 2.3_

  - [ ] 3.6 Update `RegisterComponent.submit()` in `register.component.ts` to dispatch `confirmPassword`
    - In `frontend/src/app/features/auth/register.component.ts`, update `submit()` to destructure and dispatch `confirmPassword`:
      ```typescript
      const { firstName, lastName, email, password, confirmPassword } = this.form.getRawValue();
      this.store.dispatch(AuthActions.register({ firstName, lastName, email, password, confirmPassword }));
      ```
    - The `confirmPassword` form control already exists in the form group — only the `submit()` method needs updating
    - _Bug_Condition: `submit()` destructures `{ firstName, lastName, email, password }` — `confirmPassword` is collected in the form but never dispatched_
    - _Expected_Behavior: `confirmPassword` is included in the dispatched `AuthActions.register` action_
    - _Preservation: `passwordsMatch` validator, `Validators.pattern`, and all template bindings are not modified_
    - _Requirements: 2.2_

  - [ ] 3.7 Verify bug condition exploration test now passes
    - **Property 1: Expected Behavior** - Auth Service Contract Mismatches
    - **IMPORTANT**: Re-run the SAME tests from task 1 — do NOT write new tests
    - The tests from task 1 encode the expected behavior for all 5 bugs
    - When these tests pass, it confirms all 5 bugs are fixed
    - Run the exploration tests from step 1 against the fixed code
    - **EXPECTED OUTCOME**: All 5 assertions PASS (confirms all bugs are fixed)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 3.8 Verify preservation tests still pass
    - **Property 2: Preservation** - Non-Auth-Service Code Paths Unchanged
    - **IMPORTANT**: Re-run the SAME tests from task 2 — do NOT write new tests
    - Run all preservation property tests from step 2 against the fixed code
    - **EXPECTED OUTCOME**: All preservation tests PASS (confirms no regressions)
    - Confirm interceptors, guards, form validation, and `refreshToken()` behavior are all unchanged
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [ ] 4. Checkpoint — Ensure all tests pass
  - Run the full frontend test suite: `cd frontend && npx ng test --run` (or `npx vitest --run` if Vitest is configured)
  - Ensure all exploration tests (task 1) pass — confirming all 5 bugs are fixed
  - Ensure all preservation tests (task 2) pass — confirming no regressions
  - Ensure all other existing tests continue to pass
  - Ask the user if any questions arise about test failures or ambiguous behavior
