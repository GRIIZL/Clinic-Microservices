import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Office, OfficeFormValue, OfficeStatus } from '../models/office.models';

@Injectable({ providedIn: 'root' })
export class OfficeService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/offices`;

  getAll(): Observable<Office[]> {
    return this.http.get<Office[]>(this.apiUrl);
  }

  getById(id: string): Observable<Office> {
    return this.http.get<Office>(`${this.apiUrl}/${id}`);
  }

  create(value: OfficeFormValue): Observable<Office> {
    return this.http.post<Office>(this.apiUrl, value);
  }

  update(id: string, value: OfficeFormValue): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${id}`, value);
  }

  changeStatus(id: string, status: OfficeStatus): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/${id}/status`, { status });
  }
}
