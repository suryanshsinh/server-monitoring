import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly STORAGE_KEY = 'monitor-theme';
  
  isDark = signal(false);

  constructor() {
    this.initTheme();
  }

  private initTheme(): void {
    const stored = localStorage.getItem(this.STORAGE_KEY);
    
    if (stored) {
      this.setDark(stored === 'dark');
    } else {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.setDark(prefersDark);
    }

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      if (!localStorage.getItem(this.STORAGE_KEY)) {
        this.setDark(e.matches);
      }
    });
  }

  toggle(): void {
    this.setDark(!this.isDark());
  }

  private setDark(dark: boolean): void {
    this.isDark.set(dark);
    
    if (dark) {
      document.documentElement.classList.add('dark');
    } else {
      document.documentElement.classList.remove('dark');
    }
    
    localStorage.setItem(this.STORAGE_KEY, dark ? 'dark' : 'light');
  }
}
