import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, switchMap } from 'rxjs';
import { GroupSessionsResponse } from '@app/models/group-orders/group-sessions-response.model';
import { GroupStatusResponse } from '@app/models/group-orders/group-status.model';
import { OwnerSessionResponse } from '@app/models/group-orders/owner-session.model';
import { SubmittedDish, SubmitGroupPreOrderRequest } from '@app/models/group-orders/submit-group-pre-order.model';
import { GuestInvite } from '@app/models/reservations/guest-invite.model';
import { AuthenticationService } from '@app/auth/services/authentication.service';
import { SKIP_ERROR_TOAST } from '@app/auth/skip-error-toast.context';
import { API_BASE_URL } from '@app/shared/config/api-base-url.token';
import { GuestInviteService } from '@app/services/reservation/guest-invite.service';

@Injectable({
  providedIn: 'root',
})
export class GroupOrdersService {
  private readonly baseUrl = inject(API_BASE_URL);
  private readonly http = inject(HttpClient);
  private readonly guestInviteService = inject(GuestInviteService);
  private readonly auth = inject(AuthenticationService);

  private ownerHeaders(): HttpHeaders {
    const idToken = this.auth.getTokens()?.idToken;
    return idToken ? new HttpHeaders({ Authorization: `Bearer ${idToken}` }) : new HttpHeaders();
  }

  /** reservationId → invite-token → invite (з GroupReservationId і даними для шапки) */
  getGroupInvite$(reservationId: string): Observable<GuestInvite> {
    return this.guestInviteService
      .getInviteToken$(reservationId)
      .pipe(switchMap((response) => this.guestInviteService.getInvite$(response.token)));
  }

  /** silent: фонове оновлення — помилку не показуємо toast-ом (рівень 3) */
  getSessions$(groupReservationId: string, options: { silent?: boolean } = {}): Observable<GroupSessionsResponse> {
    return this.http.get<GroupSessionsResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions`,
      { context: silentContext(options), headers: this.ownerHeaders() },
    );
  }

  /** DELETE /guest/groups/{id}/sessions/{guestSessionId} (бонус) — відповідь у форматі GET sessions */
  removeSession$(groupReservationId: string, guestSessionId: string): Observable<GroupSessionsResponse> {
    return this.http.delete<GroupSessionsResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/sessions/${encodeURIComponent(guestSessionId)}`,
      { headers: this.ownerHeaders() },
    );
  }

  getOwnerSessionId$(reservationId: string): Observable<string> {
    return this.http
      .get<OwnerSessionResponse>(`${this.baseUrl}/reservations/${encodeURIComponent(reservationId)}/guest-session-id`)
      .pipe(map((response) => response.guestSessionId));
  }

  getStatus$(groupReservationId: string, options: { silent?: boolean } = {}): Observable<GroupStatusResponse> {
    return this.http.get<GroupStatusResponse>(
      `${this.baseUrl}/guest/groups/${encodeURIComponent(groupReservationId)}/status`,
      { context: silentContext(options), headers: this.ownerHeaders() },
    );
  }

  submit$(groupReservationId: string, reservationId: string): Observable<SubmittedDish[]> {
    const body: SubmitGroupPreOrderRequest = { reservationId };
    return this.http.post<SubmittedDish[]>(
      `${this.baseUrl}/group-reservations/${encodeURIComponent(groupReservationId)}/submit`,
      body,
    );
  }
}

function silentContext(options: { silent?: boolean }): HttpContext {
  return new HttpContext().set(SKIP_ERROR_TOAST, options.silent ?? false);
}

