export interface SetupStepDto {
  key: string;
  title: string;
  /** What goes wrong if the step is skipped. */
  why: string;
  route: string;
  done: boolean;
  optional: boolean;
}

export interface SetupProgressDto {
  completed: number;
  required: number;
  isComplete: boolean;
  /** The first unfinished required step, or null when there is none. */
  nextRoute: string | null;
  steps: SetupStepDto[];
}
