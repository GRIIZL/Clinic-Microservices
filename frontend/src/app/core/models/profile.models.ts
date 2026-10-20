export type DoctorStatus = 'At work' | 'On vacation' | 'Sick leave';

export interface PatientProfile {
  id: string;
  firstName: string;
  lastName: string;
  middleName: string;
  phoneNumber: string;
  dateOfBirth: string;
  photoUrl: string;
  appointmentResults: string[];
}

export interface DoctorProfile {
  id: string;
  accountId: string;
  firstName: string;
  lastName: string;
  middleName: string;
  fullName: string;
  email: string;
  dateOfBirth: string;
  photoUrl: string;
  specialization: string;
  officeId: string;
  status: DoctorStatus;
  careerStartYear: number;
  experience: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePatientRequest {
  accountId?: string | null;
  photoUrl?: string | null;
  firstName: string;
  lastName: string;
  middleName?: string | null;
  phoneNumber: string;
  dateOfBirth: string;
}

export interface ExistingProfileData {
  firstName: string;
  lastName: string;
  phoneNumber: string;
}

export interface CreatePatientResponse {
  matchFound: boolean;
  message?: string;
  existingProfileId?: string;
  existingProfileData?: ExistingProfileData;
  profileId?: string;
}

export interface DoctorQuery {
  name?: string;
  specialization?: string;
  officeId?: string;
}
