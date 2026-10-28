export type ApiProblem = {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  correlationId?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ApiProblem | null
  readonly retryAfter: string | null

  constructor(
    status: number,
    problem: ApiProblem | null = null,
    retryAfter: string | null = null,
  ) {
    super(problem?.title ?? `Request failed (${status})`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.retryAfter = retryAfter
  }

  static async fromResponse(
    response: Response,
  ): Promise<ApiError> {
    const contentType =
      response.headers.get('Content-Type') ?? ''

    let problem: ApiProblem | null = null

    if (contentType.includes('json')) {
      try {
        problem =
          (await response.clone().json()) as ApiProblem
      } catch {
        problem = null
      }
    }

    return new ApiError(
      response.status,
      problem,
      response.headers.get('Retry-After'),
    )
  }
}
