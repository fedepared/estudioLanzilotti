import { Component, OnInit, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
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
  // FIX 1 ─ OnPush evita el doble-check de la estrategia Default y es la forma
  // canónica de resolver NG0100 cuando el estado cambia fuera del ciclo de Angular.
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Home implements OnInit {

  constructor(
    private genericService: GenericService<IExpedientePage>,
    // FIX 2 ─ Inyectar cdr (estaba importado pero nunca inyectado).
    private cdr: ChangeDetectorRef,
  ) {}

  // ── Calendario ────────────────────────────────────────────────────────────
  rangofechas: Date[] | undefined;

  // ── Estado ───────────────────────────────────────────────────────────────
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
    return this.resumen.rojos + this.resumen.naranjas + this.resumen.amarillos + this.resumen.verdes;
  }
  get expedientesCriticos(): number  { return this.resumen.rojos;     }
  get expedientesEnRiesgo(): number  { return this.resumen.naranjas;  }
  get expedientesAdvertencia(): number { return this.resumen.amarillos; }
  get expedientesAlDia(): number     { return this.resumen.verdes;    }

  // ── Lifecycle ─────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.textoSubject.pipe(debounceTime(1000), distinctUntilChanged()).subscribe(() => {
      this.currentPage = 1;
      this.cargarPagina(1, this.currentPageSize);
    });
  }

  // ── Eventos filtro ────────────────────────────────────────────────────────
  onTextoChange(valor: string): void {
    this.filtroTexto = valor;
    this.textoSubject.next(valor);
  }

  onSemaforoChange(color: string): void {
    this.filtroSemaforo = this.filtroSemaforo === color ? '' : color;
  }

  aplicarFiltros(): void {
    this.currentPage = 1;
    this.cargarPagina(1, this.currentPageSize);
  }

  limpiarFiltros(): void {
    this.filtroTexto = '';
    this.filtroSemaforo = '';
    this.rangofechas = undefined;
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
   
    this.loading = true;
    this.cdr.markForCheck();

    let params = `Alertas/caducidad?pageNumber=${pageNumber}&pageSize=${pageSize}`;
    if (this.filtroTexto?.trim()) {
      params += `&texto=${encodeURIComponent(this.filtroTexto.trim())}`;
    }
    if (this.filtroSemaforo) {
      params += `&semaforo=${encodeURIComponent(this.filtroSemaforo)}`;
    }
    if (this.rangofechas?.length) {
      const [desde, hasta] = this.rangofechas;
      if (desde) {
        params += `&fechaUltimoMovimientoDesde=${this.primerDiaMes(desde)}`;
        params += `&fechaUltimoMovimientoHasta=${this.ultimoDiaMes(hasta ?? desde)}`;
      }
    }

    this.genericService.getAll(params).subscribe({
      next: (resp: IExpedientePage) => {
        this.expedientes    = this.normalizarExpedientes(resp.datos);
        this.totalRegistros = resp.totalRegistros;
        this.resumen        = resp.resumenSemaforos ?? { rojos: 0, naranjas: 0, amarillos: 0, verdes: 0 };
        this.loading        = false;
        // FIX 4 ─ Con OnPush, las respuestas asíncronas (Observable/HTTP) no
        // disparan CD automáticamente. markForCheck() programa una revisión
        // en el próximo ciclo sin necesidad de detectChanges() manual.
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Error cargando expedientes:', err);
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  // ── Helpers fecha ─────────────────────────────────────────────────────────
  private primerDiaMes(d: Date): string {
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    return `${yyyy}-${mm}-01`;
  }

  private ultimoDiaMes(d: Date): string {
    const ultimo = new Date(d.getFullYear(), d.getMonth() + 1, 0);
    const yyyy   = ultimo.getFullYear();
    const mm     = String(ultimo.getMonth() + 1).padStart(2, '0');
    const dd     = String(ultimo.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  // ── Normalización ─────────────────────────────────────────────────────────
  private normalizarExpedientes(items: IExpediente[]): IExpediente[] {
    return items.map((exp) => {
      const raw   = exp.colorSemaforo?.trim().toLowerCase() ?? '';
      const color = HEX_A_COLOR[raw] ?? raw;
      return { ...exp, estadoSemaforo: exp.estadoSemaforo?.trim() ?? '', colorSemaforo: color };
    });
  }

  // ── Helpers vista ─────────────────────────────────────────────────────────
  getRowClass = (exp: IExpediente): string => {
    switch (exp.colorSemaforo) {
      case 'rojo':     return 'row-red';
      case 'naranja':  return 'row-orange';
      case 'amarillo': return 'row-yellow';
      case 'verde':    return 'row-green';
      default:         return '';
    }
  };

  getEstadoLabel(color: string): string {
    switch (color) {
      case 'rojo':     return 'Crítico — más de 5 meses';
      case 'naranja':  return 'En riesgo — más de 85 días';
      case 'amarillo': return 'Advertencia — más de 72 días';
      case 'verde':    return 'Al día';
      default:         return '';
    }
  }
}