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
