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
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { CountUp } from '../../directives/count-up';

const HEX_A_COLOR: Record<string, string> = {
  '#ef4444': 'rojo',
  '#facc15': 'amarillo',
  '#22c55e': 'verde',
};

@Component({
  selector: 'app-home',
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TooltipModule,CountUp],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  constructor(private genericService: GenericService<IExpedientePage>, private cd: ChangeDetectorRef) {

  }

  expedientes: IExpediente[] = [];
  totalRegistros: number = 0;
  resumen: IResumenSemaforos = { rojos: 0, amarillos: 0, verdes: 0 };
  pageSize: number = 20;
  loading: boolean = false;
  fechaActual: Date = new Date ();

  // Filtros
  filtroTexto: string = '';
  filtroSemaforo: string = '';

  private currentPage: number = 1;
  private currentPageSize: number = 20;

  // Debounce del texto
  private textoSubject = new Subject<string>();

  get totalExpedientes(): number { return (this.resumen.rojos + this.resumen.amarillos + this.resumen.verdes); }
  get expedientesCriticos(): number { return this.resumen.rojos; }
  get expedientesAdvertencia(): number { return this.resumen.amarillos; }
  get expedientesAlDia(): number { return this.resumen.verdes; }

  ngOnInit(): void {
    // Debounce: espera 400ms después de que el usuario deja de tipear
    this.textoSubject.pipe(
      debounceTime(1000),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.cargarPagina(1, this.currentPageSize);
    });
  }
  onTextoChange(valor: string): void {
    if(valor.length> 3)
    {
      this.filtroTexto = valor;
      this.textoSubject.next(valor);
    }
  }

  /** Llamado por los botones de semáforo */
  onSemaforoChange(color: string): void {
    // Toggle: si ya está seleccionado, lo deselecciona
    this.filtroSemaforo = this.filtroSemaforo === color ? '' : color;
    this.currentPage = 1;
    this.cargarPagina(1, this.currentPageSize);
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const pageSize = event.rows ?? this.currentPageSize;
    const pageNumber = Math.floor((event.first ?? 0) / pageSize) + 1;
    this.currentPageSize = pageSize;
    this.currentPage = pageNumber;
    this.cargarPagina(pageNumber, pageSize);
  }



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
          this.resumen = resp.resumenSemaforos ?? { rojos: 0, amarillos: 0, verdes: 0 };
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
    if (color === 'rojo') return 'row-red';
    if (color === 'amarillo') return 'row-yellow';
    if (color === 'verde') return 'row-green';
    return '';
  };



}