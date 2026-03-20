import { Injectable } from '@angular/core';
import { Observable, catchError } from 'rxjs';
import { ApiHandler } from './api-handler';

@Injectable({
  providedIn: 'root',
})
export class GenericService<T> extends ApiHandler {

  getAll(path: string): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${path}`).pipe(catchError(this.handleError));
  }

  getById(path: string, id: number | string): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${path}/${id}`).pipe(catchError(this.handleError));
  }

  create(path: string, item: T): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${path}`, item).pipe(catchError(this.handleError));
  }

  update(path: string, id: number | string, item: T): Observable<T> {
    return this.http
      .put<T>(`${this.baseUrl}/${path}/${id}`, item)
      .pipe(catchError(this.handleError));
  }

  delete(path: string, id: number | string): Observable<any> {
    return this.http.delete(`${this.baseUrl}/${path}/${id}`).pipe(catchError(this.handleError));
  }
}
