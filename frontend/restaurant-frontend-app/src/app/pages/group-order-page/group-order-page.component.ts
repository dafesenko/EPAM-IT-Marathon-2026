import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { forkJoin, switchMap } from 'rxjs';
import { ParticipantCardComponent } from '@app/components/group-orders/participant-card/participant-card.component';
import { Participant } from '@app/models/group-orders/participant.model';
import { GroupReservationStatus } from '@app/models/group-orders/group-status.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { GroupOrdersService } from '@app/services/group-orders/group-orders.service';
import { ToastService } from '@app/services/toast/toast.service';
import { ModalComponent } from '@app/shared/custom-components/modal/modal.component';
import { consolidatePreOrder } from '@app/shared/helpers/consolidate-pre-order';

type LoadState = 'loading' | 'ready' | 'notFound' | 'error';

@Component({
  selector: 'app-group-order-page',
  imports: [ParticipantCardComponent, ModalComponent, CurrencyPipe, DatePipe],
  templateUrl: './group-order-page.component.html',
  styleUrl: './group-order-page.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager,
})
export class GroupOrderPageComponent implements OnInit {
  private readonly service = inject(GroupOrdersService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  private readonly reservationId = this.route.snapshot.paramMap.get('reservationId') ?? '';

  readonly loadState = signal<LoadState>('loading');
  readonly invite = signal<GuestInvite | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly ownerSessionId = signal('');
  readonly status = signal<GroupReservationStatus>('Open');
  readonly submitting = signal(false);
  readonly dialogOpen = signal(false);

  readonly groupId = computed(() => this.invite()?.GroupReservationId ?? '');
  readonly guestCount = computed(() => this.participants().length);
  readonly consolidated = computed(() => consolidatePreOrder(this.participants()));
  readonly hasAnyDishes = computed(() => this.participants().some((person) => person.items.length > 0));
  readonly notSubmittedCount = computed(() => this.participants().filter((person) => !person.isSubmitted).length);
  readonly warningText = computed(() => {
    const count = this.notSubmittedCount();
    const subject = count === 1 ? 'participant hasn’t' : 'participants haven’t';
    return `${count} ${subject} submitted their cart yet — their dishes won't be included in the order`;
  });
  readonly statusLabel = computed(() =>
    this.status() === 'Open' ? 'Collecting pre-orders' : 'Sent to Restaurant',
  );
  // підтверджених страв немає — відправляти нічого
  readonly canSubmit = computed(
    () => this.status() === 'Open' && !this.submitting() && this.consolidated().items.length > 0,
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loadState.set('loading');
    this.service
      .getGroupInvite$(this.reservationId)
      .pipe(
        switchMap((invite) => {
          this.invite.set(invite);
          return forkJoin({
            sessions: this.service.getSessions$(invite.GroupReservationId),
            ownerId: this.service.getOwnerSessionId$(this.reservationId),
            status: this.service.getStatus$(invite.GroupReservationId),
          });
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ sessions, ownerId, status }) => {
          this.participants.set(sessions.participants);
          this.ownerSessionId.set(ownerId);
          this.status.set(status.status);
          this.loadState.set('ready');
        },
        error: (error: HttpErrorResponse) => this.loadState.set(error.status === 404 ? 'notFound' : 'error'),
      });
  }

  openSubmitDialog(): void {
    if (this.canSubmit()) this.dialogOpen.set(true);
  }

  closeSubmitDialog(): void {
    if (!this.submitting()) this.dialogOpen.set(false);
  }

  confirmSubmit(): void {
    if (!this.canSubmit()) return;

    this.submitting.set(true);
    this.service
      .submit$(this.groupId(), this.reservationId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.dialogOpen.set(false);
          this.status.set('Submitted');
          this.toast.success('Pre-order sent to the restaurant');
        },
        // помилку покаже tokenInterceptor; екран лишаємо живим, кнопку можна натиснути знову
        error: () => {
          this.submitting.set(false);
          this.dialogOpen.set(false);
        },
      });
  }
}
