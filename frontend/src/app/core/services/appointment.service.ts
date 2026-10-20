import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Appointment,
  AppointmentResult,
  AppointmentScheduleItem,
  AvailableSlotsResponse,
  CreateAppointmentRequest,
  CreateAppointmentResultRequest,
  RescheduleAppointmentRequest,
  UpdateAppointmentResultRequest,
} from '../models/appointment.models';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/appointments`;

  create(request: CreateAppointmentRequest): Observable<{ message: string; appointmentId: string }> {
    return this.http.post<{ message: string; appointmentId: string }>(this.apiUrl, request);
  }

  getPatientHistory(patientId: string): Observable<Appointment[]> {
    return this.http.get<Appointment[]>(`${this.apiUrl}/patient/${patientId}`);
  }

  getDoctorSchedule(doctorId: string, date: string): Observable<AppointmentScheduleItem[]> {
    return this.http.get<AppointmentScheduleItem[]>(`${this.apiUrl}/doctor/${doctorId}`, {
      params: { date },
    });
  }

  cancel(id: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  reschedule(id: string, request: RescheduleAppointmentRequest): Observable<unknown> {
    return this.http.patch(`${this.apiUrl}/${id}/reschedule`, request);
  }

  getAvailableSlots(
    doctorId: string,
    date: string,
    categoryName: string,
  ): Observable<AvailableSlotsResponse> {
    return this.http.get<AvailableSlotsResponse>(`${this.apiUrl}/available-slots`, {
      params: { doctorId, date, categoryName },
    });
  }

  getAvailableDates(
    doctorId: string,
    from: string,
    to: string,
    categoryName: string,
  ): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/available-dates`, {
      params: { doctorId, from, to, categoryName },
    });
  }

  getResult(appointmentId: string): Observable<AppointmentResult> {
    return this.http.get<AppointmentResult>(`${this.apiUrl}/${appointmentId}/result`);
  }

  createResult(request: CreateAppointmentResultRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/results`, request);
  }

  updateResult(appointmentId: string, request: UpdateAppointmentResultRequest): Observable<unknown> {
    return this.http.put(`${this.apiUrl}/${appointmentId}/result`, request);
  }

  downloadResultPdf(appointmentId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${appointmentId}/result/download`, {
      responseType: 'blob',
    });
  }

  /** Скачивает PDF и отдаёт его браузеру как файл. */
  saveResultPdf(appointmentId: string, blob: Blob): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `MedicalReport_${appointmentId}.pdf`;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  }
}
