import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { throwError } from 'rxjs';
import { environment } from '../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ApiHandler {
  protected baseUrl: string = environment.baseUrl;

  constructor(protected http: HttpClient) {}

  protected handleError(error: HttpErrorResponse) {
    console.error('Ocurrió un error en la petición:', error);
    return throwError(() => new Error(error.message || 'Error en el servidor'));
  }
}
