import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DoctorProfile, DoctorQuery } from '../models/profile.models';

@Injectable({ providedIn: 'root' })
export class DoctorService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/doctors`;

  /** Список для пациентов: только врачи со статусом «At work». */
  getForPatients(query: DoctorQuery = {}): Observable<DoctorProfile[]> {
    return this.http.get<DoctorProfile[]>(`${this.apiUrl}/patient-view`, {
      params: this.toParams(query),
    });
  }

  /** Список для персонала: все статусы. */
  getForAdmin(query: DoctorQuery = {}): Observable<DoctorProfile[]> {
    return this.http.get<DoctorProfile[]>(`${this.apiUrl}/admin-view`, {
      params: this.toParams(query),
    });
  }

  getById(id: string): Observable<DoctorProfile> {
    return this.http.get<DoctorProfile>(`${this.apiUrl}/${id}`);
  }

  private toParams(query: DoctorQuery): Record<string, string> {
    const params: Record<string, string> = {};
    if (query.name) params['name'] = query.name;
    if (query.specialization) params['specialization'] = query.specialization;
    if (query.officeId) params['officeId'] = query.officeId;
    return params;
  }
}
