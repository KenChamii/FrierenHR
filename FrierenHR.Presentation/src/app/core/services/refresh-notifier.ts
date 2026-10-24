import { Subject } from 'rxjs';

/** Lets sibling components tell each other that something they all display has changed. */
export abstract class RefreshNotifier {
  private readonly changed = new Subject<void>();

  /** Emits after every successful create, update or delete. */
  readonly changes$ = this.changed.asObservable();

  notifyChanged(): void {
    this.changed.next();
  }
}
