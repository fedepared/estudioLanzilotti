import { Directive, ElementRef, Input, OnChanges } from '@angular/core';

@Directive({
  selector: '[appCountUp]',
})
export class CountUp implements OnChanges {
  @Input('appCountUp') target = 0;
  @Input() duration = 1800;

  constructor(private el: ElementRef) {}

  ngOnChanges() {
    if (this.target > 0) this.animate();
  }

  private animate() {
    const start = performance.now();
    const easeOut = (t: number) => 1 - Math.pow(1 - t, 3);

    const step = (now: number) => {
      const progress = easeOut(Math.min((now - start) / this.duration, 1));
      this.el.nativeElement.textContent = Math.round(this.target * progress).toLocaleString();
      if (progress < 1) requestAnimationFrame(step);
    };

    requestAnimationFrame(step);
  }
}
