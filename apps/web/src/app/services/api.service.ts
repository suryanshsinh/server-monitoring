import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Server,
  CreateServerRequest,
  UpdateServerRequest,
  MetricSnapshot,
  ServiceStatus,
  TestConnectionResult,
  SetupSshKeyResult
} from '../models/server.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getServers(): Observable<Server[]> {
    return this.http.get<Server[]>(`${this.baseUrl}/servers`);
  }

  getServer(id: number): Observable<Server> {
    return this.http.get<Server>(`${this.baseUrl}/servers/${id}`);
  }

  createServer(request: CreateServerRequest): Observable<Server> {
    return this.http.post<Server>(`${this.baseUrl}/servers`, request);
  }

  updateServer(id: number, request: UpdateServerRequest): Observable<Server> {
    return this.http.put<Server>(`${this.baseUrl}/servers/${id}`, request);
  }

  deleteServer(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/servers/${id}`);
  }

  getServerMetrics(
    id: number,
    from?: Date,
    to?: Date,
    limit?: number
  ): Observable<MetricSnapshot[]> {
    let params = new HttpParams();
    if (from) params = params.set('from', from.toISOString());
    if (to) params = params.set('to', to.toISOString());
    if (limit) params = params.set('limit', limit.toString());

    return this.http.get<MetricSnapshot[]>(`${this.baseUrl}/servers/${id}/metrics`, { params });
  }

  getServerServices(id: number): Observable<ServiceStatus[]> {
    return this.http.get<ServiceStatus[]>(`${this.baseUrl}/servers/${id}/services`);
  }

  testConnection(id: number, activate: boolean = false): Observable<TestConnectionResult> {
    const params = activate ? '?activate=true' : '';
    return this.http.post<TestConnectionResult>(`${this.baseUrl}/servers/${id}/test-connection${params}`, {});
  }

  setupSshKey(id: number, password: string): Observable<SetupSshKeyResult> {
    return this.http.post<SetupSshKeyResult>(`${this.baseUrl}/servers/${id}/setup-ssh-key`, { password });
  }

  checkHealth(): Observable<{ status: string; timestamp: string }> {
    return this.http.get<{ status: string; timestamp: string }>(`${this.baseUrl}/health`);
  }
}
