export type AppointmentStatus = 'Pending' | 'Approved' | 'Canceled' | 'Completed';

export interface Appointment {
  id: string;
  patientId: string;
  patientEmail: string;
  specializationId: string;
  doctorId: string;
  serviceId: string;
  officeId: string;
  date: string;
  timeslot: string;
  status: AppointmentStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAppointmentRequest {
  patientId: string;
  patientEmail?: string | null;
  specializationId: string;
  doctorId: string;
  serviceId: string;
  officeId: string;
  date: string;
  timeslot: string;
}

export interface RescheduleAppointmentRequest {
  doctorId: string;
  date: string;
  timeslot: string;
}

export interface AppointmentScheduleItem {
  id: string;
  patientId: string;
  patientFullName: string;
  serviceName: string;
  timeslot: string;
  status: AppointmentStatus;
  date: string;
  hasResult: boolean;
}

export interface AvailableSlotsResponse {
  date: string;
  requiredSlotsCount: number;
  availableStartTimes: string[];
}

export interface AppointmentResult {
  id: string;
  appointmentId: string;
  complaints: string;
  conclusion: string;
  recommendations: string;
  createdAt: string;
}

export interface CreateAppointmentResultRequest {
  appointmentId: string;
  complaints: string;
  conclusion: string;
  recommendations: string;
}

export interface UpdateAppointmentResultRequest {
  complaints: string;
  conclusion: string;
  recommendations: string;
}
