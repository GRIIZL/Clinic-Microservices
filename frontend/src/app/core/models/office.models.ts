export type OfficeStatus = 'Active' | 'Inactive';

export interface Office {
  id: string;
  photoUrl: string;
  city: string;
  street: string;
  houseNumber: string;
  officeNumber: string;
  status: OfficeStatus;
  registryPhoneNumber: string;
  createdAt: string;
  updatedAt: string;
}

export interface OfficeFormValue {
  city: string;
  street: string;
  houseNumber: string;
  officeNumber: string;
  registryPhoneNumber: string;
  status: OfficeStatus;
  photoUrl: string;
}
