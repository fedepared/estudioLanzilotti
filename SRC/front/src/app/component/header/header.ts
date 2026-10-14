import { Component, Input, ChangeDetectionStrategy, inject } from '@angular/core';
import { CountUp } from '../../directives/count-up';
import { IResumenSemaforos } from '../../interfaces/iexpediente';
import { SidebarService } from '../../services/sidebar.service';

@Component({
  selector: 'app-header',
  imports: [CountUp],
  templateUrl: './header.html',
  styleUrl: './header.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Header {
  readonly sidebarService = inject(SidebarService);

  @Input() subtitle = 'Control de escritos vencidos';
  @Input() total = 0;
  @Input() criticos = 0;
  @Input() enRiesgo = 0;
  @Input() advertencia = 0;
  @Input() alDia = 0;

  @Input() set resumen(val: IResumenSemaforos | undefined) {
    if (val) {
      this.criticos = val.rojos ?? 0;
      this.enRiesgo = val.naranjas ?? 0;
      this.advertencia = val.amarillos ?? 0;
      this.alDia = val.verdes ?? 0;
      this.total = this.criticos + this.enRiesgo + this.advertencia + this.alDia;
    }
  }
}
