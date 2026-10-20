export type ServiceStatus = 'Active' | 'Inactive';
export type ServiceCategory = 'Consultations' | 'Diagnostics' | 'Analyses';

export const SERVICE_CATEGORIES: ServiceCategory[] = ['Consultations', 'Diagnostics', 'Analyses'];

export interface MedicalService {
  id: string;
  name: string;
  price: number;
  status: ServiceStatus;
  categoryName: ServiceCategory;
  createdAt: string;
  updatedAt: string;
}

export interface Specialization {
  id: string;
  name: string;
  status: ServiceStatus;
  services: MedicalService[];
  createdAt: string;
  updatedAt: string;
}

export interface SpecializationFormValue {
  name: string;
  status: ServiceStatus;
}

export interface MedicalServiceFormValue {
  name: string;
  price: number;
  categoryName: ServiceCategory;
  status: ServiceStatus;
}
