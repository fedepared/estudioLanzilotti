import { Component, OnInit } from '@angular/core';
import { GenericService } from '../../services/generic';
import { IExpediente } from '../../interfaces/iexpediente';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TooltipModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  constructor(private genericService: GenericService<IExpediente>) {}

  expedientes: IExpediente[] = [];
  filtroGlobal: string = '';
  fechaActual: Date = new Date();

  // Métricas del header
  get totalExpedientes(): number {
    return this.expedientes.length;
  }

  get expedientesCriticos(): number {
    return this.expedientes.filter((e) => e.colorSemaforo === 'red' || e.colorSemaforo === 'rojo')
      .length;
  }

  get expedientesAdvertencia(): number {
    return this.expedientes.filter(
      (e) => e.colorSemaforo === 'yellow' || e.colorSemaforo === 'amarillo',
    ).length;
  }

  ngOnInit(): void {
    // TODO: Descomentar cuando el back esté disponible
    // this.getExpedientes();

    // Datos de prueba temporales — remover al conectar el back
    this.expedientes = this.getMockExpedientes();
  }

  getRowClass = (exp: IExpediente): string => {
    const color = exp.colorSemaforo?.toLowerCase();
    if (color === 'red' || color === 'rojo') return 'row-red';
    if (color === 'yellow' || color === 'amarillo') return 'row-yellow';
    if (color === 'green' || color === 'verde') return 'row-green';
    return '';
  };

  /**
   * Extrae las iniciales del nombre del demandado (máx. 2).
   */
  getInitials(nombre: string): string {
    if (!nombre) return '?';
    return nombre
      .split(' ')
      .slice(0, 2)
      .map((n) => n[0])
      .join('')
      .toUpperCase();
  }

  // ─────────────────────────────────────────────
  // MOCK — eliminar cuando el back esté disponible
  // ─────────────────────────────────────────────
  private getMockExpedientes(): IExpediente[] {
    return [
      {
        idExpediente: '2024-CIV-00341',
        acto: 'Demanda',
        dema: 'Constructora Álvarez S.A.',
        descripcionUltimoEscrito:
          'Contestación de demanda por daños y perjuicios presentada por la parte demandada.',
        fechaUltimoMovimiento: new Date('2024-01-10'),
        diasInactivo: 92,
        mesesInactivo: 3,
        estadoSemaforo: 'Crítico',
        colorSemaforo: 'red',
        prioridadSemaforo: 1,
      },
      {
        idExpediente: '2024-LAB-00118',
        acto: 'Apelación',
        dema: 'Ramírez, Jorge Luis',
        descripcionUltimoEscrito: 'Recurso de apelación contra sentencia de primera instancia.',
        fechaUltimoMovimiento: new Date('2024-02-15'),
        diasInactivo: 56,
        mesesInactivo: 1,
        estadoSemaforo: 'Advertencia',
        colorSemaforo: 'yellow',
        prioridadSemaforo: 2,
      },
      {
        idExpediente: '2023-MER-00892',
        acto: 'Cautelar',
        dema: 'González & Asociados S.R.L.',
        descripcionUltimoEscrito:
          'Medida cautelar de no innovar solicitada sobre bienes registrables.',
        fechaUltimoMovimiento: new Date('2024-01-28'),
        diasInactivo: 74,
        mesesInactivo: 2,
        estadoSemaforo: 'Crítico',
        colorSemaforo: 'red',
        prioridadSemaforo: 1,
      },
      {
        idExpediente: '2024-CIV-00445',
        acto: 'Ejecutivo',
        dema: 'Herrera, María Inés',
        descripcionUltimoEscrito: 'Presentación de liquidación de intereses y capital adeudado.',
        fechaUltimoMovimiento: new Date('2024-03-01'),
        diasInactivo: 18,
        mesesInactivo: 0,
        estadoSemaforo: 'Al día',
        colorSemaforo: 'green',
        prioridadSemaforo: 3,
      },
      {
        idExpediente: '2023-FAM-00067',
        acto: 'Divorcio',
        dema: 'Moreno, Carlos Alberto',
        descripcionUltimoEscrito:
          'Convenio de alimentos y régimen de visitas presentado ante el juzgado.',
        fechaUltimoMovimiento: new Date('2024-02-08'),
        diasInactivo: 43,
        mesesInactivo: 1,
        estadoSemaforo: 'Advertencia',
        colorSemaforo: 'yellow',
        prioridadSemaforo: 2,
      },
      {
        idExpediente: '2024-PEN-00231',
        acto: 'Denuncia',
        dema: 'López, Sebastián Omar',
        descripcionUltimoEscrito:
          'Ampliación de denuncia penal con nuevas evidencias documentales.',
        fechaUltimoMovimiento: new Date('2024-03-05'),
        diasInactivo: 14,
        mesesInactivo: 0,
        estadoSemaforo: 'Al día',
        colorSemaforo: 'green',
        prioridadSemaforo: 3,
      },
      {
        idExpediente: '2022-CIV-01105',
        acto: 'Sucesión',
        dema: 'Fernández, Rosa Elena',
        descripcionUltimoEscrito:
          'Inventario y avalúo de bienes sucesorios en proceso de homologación.',
        fechaUltimoMovimiento: new Date('2023-11-20'),
        diasInactivo: 120,
        mesesInactivo: 4,
        estadoSemaforo: 'Crítico',
        colorSemaforo: 'red',
        prioridadSemaforo: 1,
      },
    ];
  }
  getExpedientes() {
    this.genericService.getAll('').subscribe();
  }
}
