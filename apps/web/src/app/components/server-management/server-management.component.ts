import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiService } from '../../services/api.service';
import { Server, CreateServerRequest, UpdateServerRequest, TestConnectionResult, SetupSshKeyResult } from '../../models/server.model';

@Component({
  selector: 'app-server-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './server-management.component.html',
  styleUrl: './server-management.component.scss'
})
export class ServerManagementComponent implements OnInit {
  servers: Server[] = [];
  loading = true;
  saving = false;
  testing = false;
  settingUp = false;
  showForm = false;
  editingServer: Server | null = null;
  error: string | null = null;
  successMessage: string | null = null;
  testResult: TestConnectionResult | null = null;
  setupResult: SetupSshKeyResult | null = null;
  newServerPublicKey: string | null = null;
  sshPassword = '';

  form: CreateServerRequest & { id?: number } = {
    name: '',
    host: '',
    port: 22,
    username: '',
    monitoredServices: []
  };

  servicesInput = '';

  constructor(
    private api: ApiService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadServers();

    this.route.queryParams.subscribe(params => {
      if (params['edit']) {
        const serverId = Number(params['edit']);
        this.api.getServer(serverId).subscribe({
          next: (server) => this.editServer(server),
          error: () => this.router.navigate(['/manage'])
        });
      }
    });
  }

  loadServers(): void {
    this.loading = true;
    this.api.getServers().subscribe({
      next: (servers) => {
        this.servers = servers;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load servers';
        this.loading = false;
        console.error('Error loading servers:', err);
      }
    });
  }

  openAddForm(): void {
    this.resetForm();
    this.showForm = true;
    this.editingServer = null;
    this.testResult = null;
    this.newServerPublicKey = null;
  }

  editServer(server: Server): void {
    this.editingServer = server;
    this.form = {
      id: server.id,
      name: server.name,
      host: server.host,
      port: server.port,
      username: server.username,
      monitoredServices: [...server.monitoredServices]
    };
    this.servicesInput = server.monitoredServices.join(', ');
    this.showForm = true;
    this.testResult = null;
    this.newServerPublicKey = null;
  }

  closeForm(): void {
    this.showForm = false;
    this.editingServer = null;
    this.testResult = null;
    this.setupResult = null;
    this.newServerPublicKey = null;
    this.sshPassword = '';
    this.resetForm();
    this.router.navigate(['/manage']);
  }

  resetForm(): void {
    this.form = {
      name: '',
      host: '',
      port: 22,
      username: '',
      monitoredServices: []
    };
    this.servicesInput = '';
    this.sshPassword = '';
    this.error = null;
    this.setupResult = null;
  }

  parseServices(): void {
    this.form.monitoredServices = this.servicesInput
      .split(',')
      .map(s => s.trim())
      .filter(s => s.length > 0);
  }

  saveServer(): void {
    this.parseServices();
    
    if (!this.form.name || !this.form.host || !this.form.username) {
      this.error = 'Please fill in all required fields';
      return;
    }

    this.saving = true;
    this.error = null;

    const request: CreateServerRequest = {
      name: this.form.name,
      host: this.form.host,
      port: this.form.port,
      username: this.form.username,
      monitoredServices: this.form.monitoredServices
    };

    if (this.editingServer) {
      const updateRequest: UpdateServerRequest = {
        ...request,
        isActive: this.editingServer.isActive
      };
      this.api.updateServer(this.editingServer.id, updateRequest).subscribe({
        next: () => {
          this.successMessage = 'Server updated successfully';
          this.saving = false;
          this.closeForm();
          this.loadServers();
          setTimeout(() => this.successMessage = null, 3000);
        },
        error: (err) => {
          this.error = 'Failed to update server';
          this.saving = false;
          console.error('Error updating server:', err);
        }
      });
    } else {
      this.api.createServer(request).subscribe({
        next: (server) => {
          this.saving = false;
          this.newServerPublicKey = server.publicKey;
          this.editingServer = server;
          this.loadServers();
        },
        error: (err) => {
          this.error = 'Failed to add server';
          this.saving = false;
          console.error('Error adding server:', err);
        }
      });
    }
  }

  copyPublicKey(): void {
    if (this.newServerPublicKey || this.editingServer?.publicKey) {
      const key = this.newServerPublicKey || this.editingServer?.publicKey || '';
      navigator.clipboard.writeText(key);
      this.successMessage = 'Public key copied to clipboard';
      setTimeout(() => this.successMessage = null, 3000);
    }
  }

  copySetupCommand(): void {
    if (this.newServerPublicKey) {
      const command = `echo "${this.newServerPublicKey}" >> ~/.ssh/authorized_keys`;
      navigator.clipboard.writeText(command);
      this.successMessage = 'Command copied to clipboard';
      setTimeout(() => this.successMessage = null, 3000);
    }
  }

  setupSshKey(): void {
    if (!this.editingServer || !this.sshPassword) {
      this.error = 'Password is required';
      return;
    }

    this.settingUp = true;
    this.setupResult = null;
    this.error = null;

    this.api.setupSshKey(this.editingServer.id, this.sshPassword).subscribe({
      next: (result) => {
        this.setupResult = result;
        this.settingUp = false;
        this.sshPassword = '';
        
        if (result.success) {
          this.successMessage = result.message;
          setTimeout(() => this.successMessage = null, 3000);
        }
      },
      error: (err) => {
        this.setupResult = {
          success: false,
          message: 'Failed to setup SSH key'
        };
        this.settingUp = false;
        console.error('Error setting up SSH key:', err);
      }
    });
  }

  testConnection(activate: boolean = false): void {
    if (!this.editingServer) return;

    this.testing = true;
    this.testResult = null;

    this.api.testConnection(this.editingServer.id, activate).subscribe({
      next: (result) => {
        this.testResult = result;
        this.testing = false;
        if (result.success && activate) {
          this.successMessage = 'Server activated and monitoring started!';
          this.loadServers();
          setTimeout(() => {
            this.successMessage = null;
            this.closeForm();
          }, 2000);
        }
      },
      error: (err) => {
        this.testResult = {
          success: false,
          message: 'Failed to test connection',
          systemInfo: null
        };
        this.testing = false;
        console.error('Error testing connection:', err);
      }
    });
  }

  toggleServerActive(server: Server): void {
    this.api.updateServer(server.id, { isActive: !server.isActive }).subscribe({
      next: () => this.loadServers(),
      error: (err) => console.error('Error toggling server:', err)
    });
  }

  deleteServer(server: Server): void {
    if (!confirm(`Are you sure you want to delete "${server.name}"? This will also delete all historical data.`)) {
      return;
    }

    this.api.deleteServer(server.id).subscribe({
      next: () => {
        this.successMessage = 'Server deleted successfully';
        this.loadServers();
        setTimeout(() => this.successMessage = null, 3000);
      },
      error: (err) => {
        this.error = 'Failed to delete server';
        console.error('Error deleting server:', err);
      }
    });
  }

  showPublicKey(server: Server): void {
    this.editingServer = server;
    this.newServerPublicKey = server.publicKey;
    this.showForm = true;
  }

  formatMemory(mb: number): string {
    if (mb >= 1024) {
      return `${(mb / 1024).toFixed(1)} GB`;
    }
    return `${mb} MB`;
  }
}
