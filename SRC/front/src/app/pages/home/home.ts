import { Component, OnInit } from '@angular/core';
import { GenericService } from '../../services/generic';
import { IExpediente, IExpedientePage, IResumenSemaforos } from '../../interfaces/iexpediente';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { ChangeDetectorRef } from '@angular/core';

const HEX_A_COLOR: Record<string, string> = {
  '#ef4444': 'red',
  '#f59e0b': 'yellow',
  '#eab308': 'yellow',
  '#22c55e': 'green',
  '#4caf7d': 'green',
};

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TooltipModule],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  constructor(private genericService: GenericService<IExpedientePage>,private cd: ChangeDetectorRef) {
    this.cargarPagina(1,20)
   }

  expedientes: IExpediente[] = [];
  totalRegistros: number = 0;
  resumen: IResumenSemaforos = { rojos: 0, amarillos: 0, verdes: 0 };
  pageSize: number = 20;
  loading: boolean = false;
  filtroGlobal: string = '';
  fechaActual: Date = new Date();

  
  get totalExpedientes(): number { return this.totalRegistros; }
  get expedientesCriticos(): number { return this.resumen.rojos; }
  get expedientesAdvertencia(): number { return this.resumen.amarillos; }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageNumber = Math.floor((event.first ?? 0) / (event.rows ?? this.pageSize)) + 1;
    const pageSize = event.rows ?? this.pageSize;
    this.cargarPagina(pageNumber, pageSize);
  }

  // ── Llamada al servicio ───────────────────────────────────────────────────
  private cargarPagina(pageNumber: number, pageSize: number): void {
    this.loading = true;

    const params = `Alertas/caducidad?pageNumber=${pageNumber}&pageSize=${pageSize}`;

    this.genericService.getAll(params).subscribe({
      next: (resp: IExpedientePage) => {
        this.expedientes = this.normalizarExpedientes(resp.datos);
        this.totalRegistros = resp.totalRegistros;
        this.resumen = resp.resumenSemaforos ?? { rojos: 0, amarillos: 0, verdes: 0 };
        this.loading = false;
        this.cd.detectChanges(); 
      },
      error: (err) => {
        console.error('Error cargando expedientes:', err);
        this.loading = false;
      },
    });
  }


  private normalizarExpedientes(items: IExpediente[]): IExpediente[] {
    return items.map((exp) => ({
      ...exp,
      estadoSemaforo: exp.estadoSemaforo?.trim() ?? '',
      colorSemaforo:
        HEX_A_COLOR[exp.colorSemaforo?.toLowerCase()] ?? exp.colorSemaforo?.toLowerCase() ?? '',
    }));
  }

 
  getRowClass = (exp: IExpediente): string => {
    const color = exp.colorSemaforo?.toLowerCase();
    if (color === 'red' || color === 'rojo') return 'row-red';
    if (color === 'yellow' || color === 'amarillo') return 'row-yellow';
    if (color === 'green' || color === 'verde') return 'row-green';
    return '';
  };


  getInitials(nombre: string): string {
    if (!nombre) return '?';
    return nombre
      .split(' ')
      .slice(0, 2)
      .map((n) => n[0])
      .join('')
      .toUpperCase();
  }
}