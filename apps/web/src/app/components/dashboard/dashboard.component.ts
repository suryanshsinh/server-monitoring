import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, interval, takeUntil, switchMap, startWith } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { Server } from '../../models/server.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  servers: Server[] = [];
  loading = true;
  error: string | null = null;
  
  private destroy$ = new Subject<void>();
  private readonly POLL_INTERVAL = 10000;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    interval(this.POLL_INTERVAL)
      .pipe(
        startWith(0),
        takeUntil(this.destroy$),
        switchMap(() => this.api.getServers())
      )
      .subscribe({
        next: (servers) => {
          this.servers = servers;
          this.loading = false;
          this.error = null;
        },
        error: (err) => {
          this.error = 'Failed to load servers. Is the API running?';
          this.loading = false;
          console.error('Error loading servers:', err);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getStatusClass(status: string | undefined): string {
    switch (status) {
      case 'healthy': return 'status-healthy';
      case 'warning': return 'status-warning';
      case 'critical': return 'status-critical';
      default: return 'status-offline';
    }
  }

  getStatusLabel(status: string | undefined): string {
    switch (status) {
      case 'healthy': return 'Healthy';
      case 'warning': return 'Warning';
      case 'critical': return 'Critical';
      default: return 'Offline';
    }
  }

  getCpuColor(percent: number): string {
    if (percent > 90) return 'text-red-600';
    if (percent > 70) return 'text-yellow-600';
    return 'text-green-600';
  }

  getMemoryColor(percent: number): string {
    if (percent > 90) return 'text-red-600';
    if (percent > 70) return 'text-yellow-600';
    return 'text-green-600';
  }

  getProgressBarColor(percent: number): string {
    if (percent > 90) return 'bg-red-500';
    if (percent > 70) return 'bg-yellow-500';
    return 'bg-green-500';
  }

  formatMemory(mb: number): string {
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(1)} GB`;
    }
    return `${mb} MB`;
  }

  getTimeAgo(timestamp: string | undefined): string {
    if (!timestamp) return 'Never';
    
    const now = new Date();
    const then = new Date(timestamp);
    const diffMs = now.getTime() - then.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    
    if (diffSec < 60) return `${diffSec}s ago`;
    if (diffSec < 3600) return `${Math.floor(diffSec / 60)}m ago`;
    if (diffSec < 86400) return `${Math.floor(diffSec / 3600)}h ago`;
    return `${Math.floor(diffSec / 86400)}d ago`;
  }
}
