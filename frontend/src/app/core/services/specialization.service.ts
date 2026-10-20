import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  MedicalService,
  MedicalServiceFormValue,
  ServiceStatus,
  Specialization,
  SpecializationFormValue,
} from '../models/service.models';

@Injectable({ providedIn: 'root' })
export class SpecializationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/specializations`;

  getAll(): Observable<Specialization[]> {
    return this.http.get<Specialization[]>(this.apiUrl);
  }

  getById(id: string): Observable<Specialization> {
    return this.http.get<Specialization>(`${this.apiUrl}/${id}`);
  }

  create(value: SpecializationFormValue): Observable<Specialization> {
    return this.http.post<Specialization>(this.apiUrl, { ...value, services: [] });
  }

  update(id: string, value: SpecializationFormValue): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${id}`, value);
  }

  changeStatus(id: string, status: ServiceStatus): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/${id}/status`, { status });
  }

  getService(specializationId: string, serviceId: string): Observable<MedicalService> {
    return this.http.get<MedicalService>(`${this.apiUrl}/${specializationId}/services/${serviceId}`);
  }

  createService(specializationId: string, value: MedicalServiceFormValue): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/${specializationId}/services`, value);
  }

  updateService(
    specializationId: string,
    serviceId: string,
    value: MedicalServiceFormValue,
  ): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${specializationId}/services/${serviceId}`, value);
  }
}
