import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Footer } from "./component/footer/footer";
import { Sidebar } from "./component/sidebar/sidebar";

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Footer, Sidebar],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('front');
}
