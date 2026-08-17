import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { AuthTokens, User } from '../../core/models/user.model';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    'Login':         props<{ email: string; password: string }>(),
    'Login Success': props<{ user: User; tokens: AuthTokens }>(),
    'Login Failure': props<{ error: string }>(),

    'Register':         props<{ firstName: string; lastName: string; email: string; password: string }>(),
    'Register Success': props<{ user: User; tokens: AuthTokens }>(),
    'Register Failure': props<{ error: string }>(),

    'Refresh Token':         emptyProps(),
    'Refresh Token Success': props<{ tokens: AuthTokens }>(),
    'Refresh Token Failure': emptyProps(),

    'Logout':         emptyProps(),
    'Logout Success': emptyProps(),

    'Load Profile':         emptyProps(),
    'Load Profile Success': props<{ user: User }>(),
    'Load Profile Failure': props<{ error: string }>(),

    'Clear Error': emptyProps(),

    // ENH-AUTH-001 — Facebook OAuth 2.0 Login
    'Facebook Login':          emptyProps(),
    'Facebook Callback':       props<{ code: string }>(),
    'Facebook Merge Required': props<{ mergeToken: string }>(),
    'Facebook Merge Confirm':  props<{ mergeToken: string; password: string }>(),

    // ENH-AUTH-002 — Apple Sign-In
    'Apple Login':    emptyProps(),
    'Apple Callback': props<{ idToken: string }>(),
  },
});
