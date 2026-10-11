import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Sends an account still on a temporary password to the change screen.
 *
 * The API enforces this too, refusing every other endpoint with a 403. This guard exists so
 * the person sees the right screen rather than a wall of failed requests.
 */
export const passwordChangeGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.mustChangePassword()) return true;

  // Already there, so let it through rather than redirecting to itself.
  if (state.url.startsWith('/set-password')) return true;

  return router.createUrlTree(['/set-password']);
};
