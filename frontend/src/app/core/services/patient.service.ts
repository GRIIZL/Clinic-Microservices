import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePatientRequest,
  CreatePatientResponse,
  PatientProfile,
} from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/patients`;

  getAll(name?: string): Observable<PatientProfile[]> {
    return this.http.get<PatientProfile[]>(this.apiUrl, {
      params: name ? { name } : {},
    });
  }

  getById(id: string): Observable<PatientProfile> {
    return this.http.get<PatientProfile>(`${this.apiUrl}/${id}`);
  }

  getByAccountId(accountId: string): Observable<PatientProfile> {
    return this.http.get<PatientProfile>(`${this.apiUrl}/by-account/${accountId}`);
  }

  create(request: CreatePatientRequest): Observable<CreatePatientResponse> {
    return this.http.post<CreatePatientResponse>(this.apiUrl, request);
  }

  linkAccount(profileId: string, accountId: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${profileId}/link-account`, null, {
      params: { accountId },
    });
  }

  delete(id: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
