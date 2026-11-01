import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AuthUser {
  email: string;
  name: string;
  role: string;
}

interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  user: AuthUser;
}

interface Session {
  token: string;
  expiresAt: number;
  user: AuthUser;
}

const STORAGE_KEY = 'store.session';

/**
 * Sessão do usuário em signals. O token fica no sessionStorage (some ao fechar a aba).
 * Em produção, alternativa mais segura: cookie HttpOnly emitido pelo back (BFF).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<Session | null>(this.restore());

  readonly user = computed(() => this.session()?.user ?? null);
  readonly token = computed(() => this.session()?.token ?? null);

  isAuthenticated(): boolean {
    const session = this.session();
    return !!session && session.expiresAt > Date.now();
  }

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, { email, password }).pipe(
      tap((response) => {
        const session: Session = {
          token: response.accessToken,
          expiresAt: Date.parse(response.expiresAtUtc),
          user: response.user,
        };
        this.session.set(session);
        this.save(session);
      }),
    );
  }

  logout(): void {
    this.session.set(null);
    this.save(null);
  }

  private restore(): Session | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      const session = raw ? (JSON.parse(raw) as Session) : null;
      return session && session.expiresAt > Date.now() ? session : null;
    } catch {
      return null;
    }
  }

  private save(session: Session | null): void {
    try {
      if (session) {
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
      } else {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // Armazenamento indisponível (modo privado): a sessão vive só em memória.
    }
  }
}
