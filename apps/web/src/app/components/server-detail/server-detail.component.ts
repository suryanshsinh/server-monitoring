import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, interval, takeUntil, switchMap, startWith, forkJoin } from 'rxjs';
import { Chart, registerables } from 'chart.js';
import { ApiService } from '../../services/api.service';
import { Server, MetricSnapshot, ServiceStatus } from '../../models/server.model';

Chart.register(...registerables);

@Component({
  selector: 'app-server-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './server-detail.component.html',
  styleUrl: './server-detail.component.scss'
})
export class ServerDetailComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('cpuChart') cpuChartRef!: ElementRef<HTMLCanvasElement>;
  @ViewChild('memoryChart') memoryChartRef!: ElementRef<HTMLCanvasElement>;

  serverId!: number;
  server: Server | null = null;
  metrics: MetricSnapshot[] = [];
  services: ServiceStatus[] = [];
  loading = true;
  error: string | null = null;
  selectedRange: '1h' | '24h' | '7d' = '24h';

  private cpuChart: Chart | null = null;
  private memoryChart: Chart | null = null;
  private destroy$ = new Subject<void>();
  private readonly POLL_INTERVAL = 5000;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private api: ApiService
  ) {}

  ngOnInit(): void {
    this.serverId = Number(this.route.snapshot.paramMap.get('id'));
    if (isNaN(this.serverId)) {
      this.router.navigate(['/']);
      return;
    }
  }

  ngAfterViewInit(): void {
    this.loadData();

    interval(this.POLL_INTERVAL)
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => this.fetchData())
      )
      .subscribe({
        next: ([server, metrics, services]) => {
          this.server = server;
          this.metrics = metrics;
          this.services = services;
          this.updateCharts();
        },
        error: (err) => console.error('Polling error:', err)
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.cpuChart?.destroy();
    this.memoryChart?.destroy();
  }

  loadData(): void {
    this.loading = true;
    this.fetchData().subscribe({
      next: ([server, metrics, services]) => {
        this.server = server;
        this.metrics = metrics;
        this.services = services;
        this.loading = false;
        this.error = null;
        setTimeout(() => this.initCharts(), 0);
      },
      error: (err) => {
        this.error = 'Failed to load server details';
        this.loading = false;
        console.error('Error loading server:', err);
      }
    });
  }

  private fetchData() {
    const from = this.getFromDate();
    return forkJoin([
      this.api.getServer(this.serverId),
      this.api.getServerMetrics(this.serverId, from, undefined, 500),
      this.api.getServerServices(this.serverId)
    ]);
  }

  private getFromDate(): Date {
    const now = new Date();
    switch (this.selectedRange) {
      case '1h': return new Date(now.getTime() - 60 * 60 * 1000);
      case '24h': return new Date(now.getTime() - 24 * 60 * 60 * 1000);
      case '7d': return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
    }
  }

  selectRange(range: '1h' | '24h' | '7d'): void {
    this.selectedRange = range;
    this.loadData();
  }

  private initCharts(): void {
    if (!this.cpuChartRef || !this.memoryChartRef) return;

    const sortedMetrics = [...this.metrics].sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    );

    const labels = sortedMetrics.map(m => this.formatTime(m.timestamp));
    const cpuData = sortedMetrics.map(m => m.cpuPercent);
    const memoryData = sortedMetrics.map(m => m.memoryPercent);

    const chartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false }
      },
      scales: {
        y: {
          min: 0,
          max: 100,
          ticks: { callback: (value: any) => value + '%' }
        },
        x: {
          ticks: { maxTicksLimit: 10 }
        }
      },
      elements: {
        point: { radius: 0 },
        line: { tension: 0.3 }
      }
    };

    this.cpuChart = new Chart(this.cpuChartRef.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'CPU Usage',
          data: cpuData,
          borderColor: '#3b82f6',
          backgroundColor: 'rgba(59, 130, 246, 0.1)',
          fill: true
        }]
      },
      options: chartOptions
    });

    this.memoryChart = new Chart(this.memoryChartRef.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [{
          label: 'Memory Usage',
          data: memoryData,
          borderColor: '#8b5cf6',
          backgroundColor: 'rgba(139, 92, 246, 0.1)',
          fill: true
        }]
      },
      options: chartOptions
    });
  }

  private updateCharts(): void {
    if (!this.cpuChart || !this.memoryChart) return;

    const sortedMetrics = [...this.metrics].sort(
      (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime()
    );

    const labels = sortedMetrics.map(m => this.formatTime(m.timestamp));
    const cpuData = sortedMetrics.map(m => m.cpuPercent);
    const memoryData = sortedMetrics.map(m => m.memoryPercent);

    this.cpuChart.data.labels = labels;
    this.cpuChart.data.datasets[0].data = cpuData;
    this.cpuChart.update('none');

    this.memoryChart.data.labels = labels;
    this.memoryChart.data.datasets[0].data = memoryData;
    this.memoryChart.update('none');
  }

  private formatTime(timestamp: string): string {
    const date = new Date(timestamp);
    if (this.selectedRange === '7d') {
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
    }
    return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });
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

  formatMemory(mb: number): string {
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(1)} GB`;
    }
    return `${mb} MB`;
  }
}
