import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  const BASE = 'http://localhost:5001/api/v1';

  const mockApiResponse = {
    accessToken: 'mock-access-token',
    refreshToken: 'mock-refresh-token',
    accessTokenExpiresAt: new Date().toISOString(),
    user: { id: 'user-1', email: 'test@test.com', firstName: 'Test', lastName: 'User' },
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service  = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('login() should return user and tokens on valid credentials', () => {
    service.login('test@test.com', 'Password@123').subscribe((result) => {
      expect(result.user.email).toBe('test@test.com');
      expect(result.tokens.accessToken).toBe('mock-access-token');
    });

    const req = httpMock.expectOne(`${BASE}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'test@test.com', password: 'Password@123' });
    req.flush(mockApiResponse);
  });

  it('login() should propagate error on 401 response', () => {
    let caughtError: { status: number } | null = null;

    service.login('test@test.com', 'wrong').subscribe({
      next: () => { throw new Error('should not succeed'); },
      error: (err: { status: number }) => { caughtError = err; },
    });

    const req = httpMock.expectOne(`${BASE}/auth/login`);
    req.flush({ message: 'Unauthorized' }, { status: 401, statusText: 'Unauthorized' });

    expect(caughtError).not.toBeNull();
    expect(caughtError!.status).toBe(401);
  });

  it('register() should call POST /api/v1/auth/register with correct body', () => {
    service.register('New', 'User', 'new@test.com', 'Password@123').subscribe((result) => {
      expect(result.user.firstName).toBe('Test');
    });

    const req = httpMock.expectOne(`${BASE}/auth/register`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      firstName: 'New',
      lastName: 'User',
      email: 'new@test.com',
      password: 'Password@123',
      confirmPassword: 'Password@123',
    });
    req.flush(mockApiResponse);
  });
});
