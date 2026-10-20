import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AstronautDutiesResponse } from './astronaut-duty.models';

@Injectable({ providedIn: 'root' })
export class AstronautDutyService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/AstronautDuty';

  getByName(name: string): Observable<AstronautDutiesResponse> {
    return this.http.get<AstronautDutiesResponse>(
      `${this.apiUrl}/${encodeURIComponent(name)}`,
    );
  }
}
