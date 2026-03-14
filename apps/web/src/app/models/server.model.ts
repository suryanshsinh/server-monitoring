export interface Server {
  id: number;
  name: string;
  host: string;
  port: number;
  username: string;
  isActive: boolean;
  monitoredServices: string[];
  createdAt: string;
  currentHealth: ServerHealth | null;
  publicKey: string | null;
}

export interface ServerHealth {
  cpuPercent: number;
  memoryUsedMb: number;
  memoryTotalMb: number;
  memoryPercent: number;
  lastUpdated: string;
  status: 'healthy' | 'warning' | 'critical' | 'offline';
}

export interface CreateServerRequest {
  name: string;
  host: string;
  port: number;
  username: string;
  monitoredServices?: string[];
}

export interface UpdateServerRequest {
  name?: string;
  host?: string;
  port?: number;
  username?: string;
  isActive?: boolean;
  monitoredServices?: string[];
}

export interface MetricSnapshot {
  id: number;
  serverId: number;
  timestamp: string;
  cpuPercent: number;
  memoryUsedMb: number;
  memoryTotalMb: number;
  memoryPercent: number;
}

export interface ServiceStatus {
  id: number;
  serverId: number;
  timestamp: string;
  serviceName: string;
  isRunning: boolean;
  status: string;
}

export interface TestConnectionResult {
  success: boolean;
  message: string;
  systemInfo: ServerSystemInfo | null;
}

export interface ServerSystemInfo {
  hostname: string;
  osVersion: string;
  cpuCores: number;
  totalMemoryMb: number;
}

export interface SetupSshKeyRequest {
  password: string;
}

export interface SetupSshKeyResult {
  success: boolean;
  message: string;
}
