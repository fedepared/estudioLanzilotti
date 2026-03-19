export interface IExpedientePage {
  datos: IExpediente[];
  totalRegistros: number;
  resumenSemaforos: IResumenSemaforos;
}
export interface IExpediente {
  idExpediente: string;
  acto: string;
  dema: string;
  descripcionUltimoEscrito: string;
  fechaUltimoMovimiento: Date;
  diasInactivo?: number;
  mesesInactivo?: number;
  estadoSemaforo: string;
  colorSemaforo: string;
  prioridadSemaforo: number;
}
export interface IResumenSemaforos {
  rojos: number;
  amarillos: number;
  verdes: number;
}
