import { Component, DestroyRef, OnInit, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';
import { ButtonModule } from 'primeng/button';
import { SetupService } from '../../core/services/setup.service';

/**
 * Carries somebody from one setup step to the next without going back to the dashboard.
 *
 * It re-reads progress on every navigation, so finishing a step is noticed and the banner
 * moves on by itself. It does not redirect on its own: adding one department rarely means
 * you are finished adding departments, and being yanked to another screen mid-task is worse
 * than a button.
 */
@Component({
  selector: 'app-setup-banner',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  templateUrl: './setup-banner.component.html',
  styleUrl: './setup-banner.component.scss',
})
export class SetupBannerComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly setup = inject(SetupService);

  readonly step = computed(() => this.setup.nextStep());
  readonly progress = computed(() => this.setup.progress());

  /** Hidden while standing on the step's own screen: the work is already in front of you. */
  readonly visible = computed(() => {
    const step = this.step();
    return !!step && !this.router.url.startsWith(step.route);
  });

  /** True once every earlier step is done, so the banner can say "next" rather than "still". */
  readonly onTheirWay = computed(() => (this.progress()?.completed ?? 0) > 0);

  ngOnInit(): void {
    this.setup.refresh();

    // Re-read after each navigation. Leaving a setup screen is the moment its step most
    // likely just became done, and this is what makes the banner advance on its own.
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.setup.refresh());
  }

  go(): void {
    const step = this.step();
    if (step) this.router.navigateByUrl(step.route);
  }

  /** For a step whose detection got it wrong, so nothing can be stuck with no way past. */
  markDone(): void {
    const step = this.step();
    if (step) this.setup.acknowledge(step.key);
  }

  hide(): void { this.setup.dismiss(); }
}
