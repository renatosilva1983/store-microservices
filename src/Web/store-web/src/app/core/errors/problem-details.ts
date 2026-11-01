import { HttpErrorResponse } from '@angular/common/http';

/**
 * Formato de erro devolvido pelas APIs (RFC 7807 — ProblemDetails).
 * `errors` só vem em erro de validação (400), com mensagens por campo.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

export function toProblemDetails(error: HttpErrorResponse): ProblemDetails | null {
  return error.error && typeof error.error === 'object' ? (error.error as ProblemDetails) : null;
}

/** Mensagem curta para exibir ao usuário. */
export function describeError(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Não foi possível conectar ao servidor. Verifique se o Gateway está no ar.';
  }

  const problem = toProblemDetails(error);
  if (problem?.errors) {
    const first = Object.values(problem.errors).flat()[0];
    return first ? `Dados inválidos: ${first}` : 'Dados inválidos.';
  }

  return problem?.detail ?? problem?.title ?? `Erro inesperado (${error.status}).`;
}
