import { TestBed } from '@angular/core/testing';
import { provideEffects } from '@ngrx/effects';
import { provideMockActions } from '@ngrx/effects/testing';
import { MockStore, provideMockStore } from '@ngrx/store/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import type { Action } from '@ngrx/store';
import * as authEffects from './auth.effects';
import { AuthActions } from './auth.actions';
import { AuthService } from '../../core/services/auth.service';
import type { User, AuthTokens } from '../../core/models/user.model';

describe('Auth Effects', () => {
  let actions$: Subject<Action>;
  let store: MockStore;
  let dispatchSpy: ReturnType<typeof vi.spyOn>;
  let authServiceMock: {
    login: ReturnType<typeof vi.fn>;
    register: ReturnType<typeof vi.fn>;
    logout: ReturnType<typeof vi.fn>;
    refreshToken: ReturnType<typeof vi.fn>;
  };

  const mockUser: User = {
    id: 'user-1', email: 'test@test.com',
    firstName: 'Test', lastName: 'User',
    phoneNumber: null, roles: [],
  };
  const mockTokens: AuthTokens = {
    accessToken: 'access-token', refreshToken: 'refresh-token', expiresIn: 0,
  };

  beforeEach(() => {
    actions$ = new Subject<Action>();
    authServiceMock = {
      login:        vi.fn(),
      register:     vi.fn(),
      logout:       vi.fn(),
      refreshToken: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        provideMockActions(() => actions$.asObservable()),
        provideMockStore({
          initialState: {
            auth: { user: null, accessToken: null, refreshToken: null, isLoading: false, error: null },
          },
        }),
        provideRouter([]),
        { provide: AuthService, useValue: authServiceMock },
        provideEffects(authEffects),
      ],
    });

    store       = TestBed.inject(MockStore);
    dispatchSpy = vi.spyOn(store, 'dispatch');
  });

  describe('loginEffect', () => {
    it('should dispatch loginSuccess when service call succeeds', async () => {
      authServiceMock.login.mockReturnValue(of({ user: mockUser, tokens: mockTokens }));

      actions$.next(AuthActions.login({ email: 'test@test.com', password: 'Password@123' }));

      await new Promise((r) => setTimeout(r, 0));

      expect(authServiceMock.login).toHaveBeenCalledWith('test@test.com', 'Password@123');
      expect(dispatchSpy).toHaveBeenCalledWith(
        AuthActions.loginSuccess({ user: mockUser, tokens: mockTokens }),
      );
    });

    it('should dispatch loginFailure when service call fails', async () => {
      authServiceMock.login.mockReturnValue(throwError(() => new Error('Login failed')));

      actions$.next(AuthActions.login({ email: 'test@test.com', password: 'wrong' }));

      await new Promise((r) => setTimeout(r, 0));

      expect(dispatchSpy).toHaveBeenCalledWith(
        AuthActions.loginFailure({ error: 'Login failed' }),
      );
    });
  });
});
