import { Subject } from 'rxjs';

/**
 * Lets sibling components tell each other that something they all display has changed.
 *
 * A stream rather than a signal, deliberately. This was a signal read inside an effect, and
 * the reload it triggered wrote to the loading signal. Angular refuses a signal write inside
 * a reactive context (NG0600), so the effect threw before the request was sent and the
 * spinner it had just switched on never switched off.
 *
 * An effect is for keeping state in step, not for reacting to an event. Reloading on a
 * notification is an event, so it belongs on a subscription.
 */
export abstract class RefreshNotifier {
  private readonly changed = new Subject<void>();

  /** Emits after every successful create, update or delete. Never emits on subscribe, so a
   * subscriber loads once itself and then follows along. */
  readonly changes$ = this.changed.asObservable();

  notifyChanged(): void {
    this.changed.next();
  }
}
