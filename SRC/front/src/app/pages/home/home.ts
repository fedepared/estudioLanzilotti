import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { GenericService } from '../../services/generic';
import { IExpediente, IExpedientePage, IResumenSemaforos } from '../../interfaces/iexpediente';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CountUp } from '../../directives/count-up';
import { DatePickerModule } from 'primeng/datepicker';

// ── Mapa hex → clave lógica ──────────────────────────────────────────────────
// Ajustar los valores hex si el back los cambia.
// Umbrales acordados:
//   Rojo     → más de 5 meses (~150 días) → "Crítico"
//   Naranja  → más de 85 días             → "En riesgo"
//   Amarillo → más de 72 días             → "Advertencia"
//   Verde    → al día                     → "Al día"
const HEX_A_COLOR: Record<string, string> = {
  '#ef4444': 'rojo',
  '#f97316': 'naranja',
  '#facc15': 'amarillo',
  '#22c55e': 'verde',
};

@Component({
  selector: 'app-home',
  imports: [
    CommonModule,
    FormsModule,
    DatePickerModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TooltipModule,
    CountUp,
  ],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home implements OnInit {
  constructor(
    private genericService: GenericService<IExpedientePage>,
    private cd: ChangeDetectorRef,
  ) {}
  // ── Calendario ────────────────────────────────────────────────────────────────
  rangofechas: Date[] | undefined;
  // ── Estado ────────────────────────────────────────────────────────────────
  expedientes: IExpediente[] = [];
  totalRegistros: number = 0;
  resumen: IResumenSemaforos = { rojos: 0, naranjas: 0, amarillos: 0, verdes: 0 };
  loading: boolean = false;
  fechaActual: Date = new Date();

  filtroTexto: string = '';
  filtroSemaforo: string = '';

  private currentPage: number = 1;
  private currentPageSize: number = 50;
  private textoSubject = new Subject<string>();

  // ── Getters header ────────────────────────────────────────────────────────
  get totalExpedientes(): number {
    return (
      this.resumen.rojos + this.resumen.naranjas + this.resumen.amarillos + this.resumen.verdes
    );
  }
  get expedientesCriticos(): number {
    return this.resumen.rojos;
  }
  get expedientesEnRiesgo(): number {
    return this.resumen.naranjas;
  }
  get expedientesAdvertencia(): number {
    return this.resumen.amarillos;
  }
  get expedientesAlDia(): number {
    return this.resumen.verdes;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.textoSubject.pipe(debounceTime(1000), distinctUntilChanged()).subscribe(() => {
      this.currentPage = 1;
      this.cargarPagina(1, this.currentPageSize);
    });
  }

  // ── Eventos filtro ────────────────────────────────────────────────────────
  onTextoChange(valor: string): void {
    if (valor.length > 3 || valor.length === 0) {
      this.filtroTexto = valor;
      this.textoSubject.next(valor);
    }
  }

  onSemaforoChange(color: string): void {
    this.filtroSemaforo = this.filtroSemaforo === color ? '' : color;
    this.currentPage = 1;
    this.cargarPagina(1, this.currentPageSize);
  }

  // ── Lazy load ─────────────────────────────────────────────────────────────
  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.currentPageSize;
    const pageNumber = Math.floor((event.first ?? 0) / pageSize) + 1;
    this.currentPageSize = pageSize;
    this.currentPage = pageNumber;
    this.cargarPagina(pageNumber, pageSize);
  }

  // ── Llamada al servicio ───────────────────────────────────────────────────
  private cargarPagina(pageNumber: number, pageSize: number): void {
    Promise.resolve().then(() => {
      this.loading = true;
      this.cd.detectChanges();

      let params = `Alertas/caducidad?pageNumber=${pageNumber}&pageSize=${pageSize}`;
      if (this.filtroTexto?.trim()) {
        params += `&texto=${encodeURIComponent(this.filtroTexto.trim())}`;
      }
      if (this.filtroSemaforo) {
        params += `&semaforo=${encodeURIComponent(this.filtroSemaforo)}`;
      }

      this.genericService.getAll(params).subscribe({
        next: (resp: IExpedientePage) => {
          this.expedientes = this.normalizarExpedientes(resp.datos);
          this.totalRegistros = resp.totalRegistros;
          this.resumen = resp.resumenSemaforos ?? {
            rojos: 0,
            naranjas: 0,
            amarillos: 0,
            verdes: 0,
          };
          this.loading = false;
          this.cd.detectChanges();
        },
        error: (err) => {
          console.error('Error cargando expedientes:', err);
          this.loading = false;
          this.cd.detectChanges();
        },
      });
    });
  }

  // ── Normalización ─────────────────────────────────────────────────────────
  private normalizarExpedientes(items: IExpediente[]): IExpediente[] {
    return items.map((exp) => {
      const raw = exp.colorSemaforo?.trim().toLowerCase() ?? '';
      const color = HEX_A_COLOR[raw] ?? raw; // si el back ya manda 'rojo', lo respeta
      return {
        ...exp,
        estadoSemaforo: exp.estadoSemaforo?.trim() ?? '',
        colorSemaforo: color,
      };
    });
  }

  // ── Helpers vista ─────────────────────────────────────────────────────────
  getRowClass = (exp: IExpediente): string => {
    switch (exp.colorSemaforo) {
      case 'rojo':
        return 'row-red';
      case 'naranja':
        return 'row-orange';
      case 'amarillo':
        return 'row-yellow';
      case 'verde':
        return 'row-green';
      default:
        return '';
    }
  };

  getEstadoLabel(color: string): string {
    switch (color) {
      case 'rojo':
        return 'Crítico — más de 5 meses';
      case 'naranja':
        return 'En riesgo — más de 85 días';
      case 'amarillo':
        return 'Advertencia — más de 72 días';
      case 'verde':
        return 'Al día';
      default:
        return '';
    }
  }
}
