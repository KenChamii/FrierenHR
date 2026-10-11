import { Component, OnInit, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { SetupService } from '../../../core/services/setup.service';
import { SetupStepDto } from '../../../core/models/setup.model';

/**
 * What a new company still has to do, in the order the steps depend on each other.
 *
 * Driven by the company's own data rather than by a tour somebody clicks through, so it
 * cannot claim a step is done when it is not, and it reappears if the thing is removed.
 */
@Component({
  selector: 'app-setup-checklist',
  standalone: true,
  imports: [CommonModule, RouterLink, ButtonModule],
  templateUrl: './setup-checklist.component.html',
  styleUrl: './setup-checklist.component.scss',
})
export class SetupChecklistComponent implements OnInit {
  private readonly setup = inject(SetupService);

  readonly visible = computed(() => this.setup.showChecklist());
  readonly progress = computed(() => this.setup.progress());

  readonly steps = computed(() => this.progress()?.steps ?? []);

  /** The one being pointed at. Only ever a required step. */
  readonly nextStep = computed(() =>
    this.steps().find(s => !s.done && !s.optional) ?? null);

  readonly percent = computed(() => {
    const progress = this.progress();
    if (!progress || progress.required === 0) return 0;
    return Math.round((progress.completed / progress.required) * 100);
  });

  ngOnInit(): void { this.setup.refresh(); }

  isNext(step: SetupStepDto): boolean { return this.nextStep()?.key === step.key; }

  /**
   * Marks a step done by hand. Each one is detected from real data, but a detection can be
   * wrong, and a step stuck on "not done" with no way past it is worse than no checklist.
   */
  markDone(step: SetupStepDto): void { this.setup.acknowledge(step.key); }

  dismiss(): void { this.setup.dismiss(); }
}
